using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MmoDemo.Contracts;

namespace MmoDemo.Gateway.Tests;

public class Phase4QuestChatTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public Phase4QuestChatTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Chat_SendAndReceiveBroadcast()
    {
        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("p4-chat-1", "editor", "0.4.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login!.PlayerId, login.Token, "Chatter", 1)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();

        // First client
        var ws = _factory.Server.CreateWebSocketClient();
        var s1 = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
        await Send(s1, MessageTypes.Auth, new AuthPayload
            { PlayerId = login.PlayerId, Token = login.Token, RoleId = create!.Role!.RoleId });
        await Receive(s1); // auth_result
        await Send(s1, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "city_001" });
        await Receive(s1); // enter_scene_result

        // Second client (separate login)
        var login2 = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("p4-chat-2", "editor", "0.4.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create2 = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login2!.PlayerId, login2.Token, "Listener", 2)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();
        var ws2 = _factory.Server.CreateWebSocketClient();
        var s2 = await ws2.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
        await Send(s2, MessageTypes.Auth, new AuthPayload
            { PlayerId = login2.PlayerId, Token = login2.Token, RoleId = create2!.Role!.RoleId });
        await Receive(s2); // auth_result
        await Send(s2, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "city_001" });
        await Receive(s2); // enter_scene_result

        // Client 1 sends chat
        await Send(s1, MessageTypes.Chat, new ChatPayload { Text = "Hello world!" });

        // Sender also receives its own broadcast so the client can confirm the local echo
        var (selfType, selfRaw) = await ReceiveUntil(s1, MessageTypes.ChatBroadcast);
        Assert.Equal(MessageTypes.ChatBroadcast, selfType);
        Assert.Contains("Hello world!", selfRaw);
        Assert.Contains("Chatter", selfRaw);

        // Client 2 receives broadcast
        var (t, raw) = await ReceiveUntil(s2, MessageTypes.ChatBroadcast);
        Assert.Equal(MessageTypes.ChatBroadcast, t);
        Assert.Contains("Hello world!", raw);
        Assert.Contains("Chatter", raw);

        await s1.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        await s2.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    [Fact]
    public async Task Quest_AcceptAndKillMonster_ProgressAndComplete()
    {
        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("p4-quest-1", "editor", "0.4.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login!.PlayerId, login.Token, "Slayer", 3)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();

        var ws = _factory.Server.CreateWebSocketClient();
        var s = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
        await Send(s, MessageTypes.Auth, new AuthPayload
            { PlayerId = login.PlayerId, Token = login.Token, RoleId = create!.Role!.RoleId });
        await Receive(s); // auth_result
        await Send(s, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "city_001" });
        var (_, enterRaw) = await Receive(s); // enter_scene_result

        // Accept quest 1 (Kill 3 Slimes)
        await Send(s, MessageTypes.AcceptQuest, new AcceptQuestPayload { QuestId = 1 });
        var (tQuest, rawQuest) = await Receive(s);
        Assert.Equal(MessageTypes.QuestUpdated, tQuest);
        Assert.Contains("\"ok\":true", rawQuest);
        Assert.Contains("Slime Extermination", rawQuest);

        // Find a slime entityId
        var slimeId = ExtractEntityIdOfType(enterRaw, "monster");

        // Kill the slime (30 HP). Use skill 3 (Fireball, 2x mult) for ~8-10 dmg/hit → ~3-4 hits.
        var foundQuestUpdate = false;
        for (var i = 0; i < 10 && !string.IsNullOrEmpty(slimeId); i++)
        {
            await Send(s, MessageTypes.CastSkill, new CastSkillPayload { TargetId = slimeId, SkillId = 3 });
            // CastSkill returns "{}" before async combat_event arrives — drain until we get valid msg
            var (t, raw) = await ReceiveNonEmpty(s);
            Assert.Equal(MessageTypes.CombatEvent, t);

            // Drain any follow-up messages (monster_death, quest_updated, drop_spawned)
            if (raw.Contains("\"targetDied\":true"))
            {
                for (var j = 0; j < 10; j++)
                {
                    try
                    {
                        var (ft, fraw) = await ReceiveWithTimeout(s, 500);
                        if (ft == MessageTypes.QuestUpdated) foundQuestUpdate = true;
                        if (ft == MessageTypes.QuestCompleted) { foundQuestUpdate = true; break; }
                        if (ft == MessageTypes.MonsterDeath || ft == MessageTypes.DropSpawned) continue;
                    }
                    catch { break; }
                }
                break;
            }
        }

        Assert.True(foundQuestUpdate, "Expected quest progress update after monster kill");

        await s.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    [Fact]
    public async Task Quest_AcceptInvalidQuest_Fails()
    {
        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("p4-quest-2", "editor", "0.4.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login!.PlayerId, login.Token, "Noob", 1)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();

        var ws = _factory.Server.CreateWebSocketClient();
        var s = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
        await Send(s, MessageTypes.Auth, new AuthPayload
            { PlayerId = login.PlayerId, Token = login.Token, RoleId = create!.Role!.RoleId });
        await Receive(s);
        await Send(s, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "city_001" });
        await Receive(s);

        // Accept nonexistent quest
        await Send(s, MessageTypes.AcceptQuest, new AcceptQuestPayload { QuestId = 999 });
        var (t, raw) = await Receive(s);
        Assert.Equal(MessageTypes.QuestUpdated, t);
        Assert.Contains("\"ok\":false", raw);

        await s.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    // ── Helpers ──

    private static async Task Send(WebSocket s, string type, object p)
    {
        var json = JsonSerializer.Serialize(new { t = type, ts = 0, p });
        await s.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<(string type, string raw)> Receive(WebSocket s)
    {
        var buf = new byte[4096];
        var r = await s.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
        var raw = Encoding.UTF8.GetString(buf, 0, r.Count);
        try
        {
            using var d = JsonDocument.Parse(raw);
            var t = d.RootElement.TryGetProperty("t", out var prop) ? prop.GetString()! : "";
            return (t, raw);
        }
        catch { return ("", raw); }
    }

    private static async Task<(string type, string raw)> ReceiveNonEmpty(WebSocket s)
    {
        while (true)
        {
            var (t, raw) = await Receive(s);
            if (!string.IsNullOrEmpty(t)) return (t, raw);
        }
    }

    private static async Task<(string type, string raw)> ReceiveWithTimeout(WebSocket s, int ms)
    {
        using var cts = new CancellationTokenSource(ms);
        var buf = new byte[4096];
        var r = await s.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
        var raw = Encoding.UTF8.GetString(buf, 0, r.Count);
        using var d = JsonDocument.Parse(raw);
        return (d.RootElement.GetProperty("t").GetString()!, raw);
    }

    private static async Task<(string type, string raw)> ReceiveUntil(WebSocket s, string expectedType)
    {
        for (var i = 0; i < 10; i++)
        {
            var (type, raw) = await ReceiveWithTimeout(s, 1000);
            if (type == expectedType) return (type, raw);
        }

        throw new TimeoutException($"Expected websocket message type {expectedType}");
    }

    private static string ExtractEntityIdOfType(string json, string entityType)
    {
        var search = $"\"type\":\"{entityType}\"";
        var typeIdx = json.IndexOf(search);
        if (typeIdx < 0) return "";
        var before = json[..typeIdx];
        var idIdx = before.LastIndexOf("\"entityId\":\"");
        if (idIdx < 0) return "";
        idIdx += 12;
        var end = json.IndexOf('"', idIdx);
        return end < 0 ? "" : json[idIdx..end];
    }
}
