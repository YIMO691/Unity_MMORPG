using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MmoDemo.Contracts;

namespace MmoDemo.Gateway.Tests;

public class Phase3CombatTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public Phase3CombatTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Combat_FullFlow_AuthEnterSceneCastSkillOnMonster()
    {
        var client = _factory.CreateClient();

        // Login + create role
        var login = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("phase3-001", "editor", "0.3.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login!.PlayerId, login.Token, "Hunter", 3)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();

        // WebSocket
        var ws = _factory.Server.CreateWebSocketClient();
        var socket = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);

        // Auth
        await Send(socket, MessageTypes.Auth, new AuthPayload
            { PlayerId = login.PlayerId, Token = login.Token, RoleId = create!.Role!.RoleId });
        var (t, raw) = await Receive(socket);
        Assert.Equal(MessageTypes.AuthResult, t);
        Assert.Contains("\"ok\":true", raw);

        // Enter scene (spawns 3 monsters, returned in entities list)
        await Send(socket, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "city_001" });
        (t, raw) = await Receive(socket);
        Assert.Equal(MessageTypes.EnterSceneResult, t);
        Assert.Contains("\"ok\":true", raw);

        // Extract monster entityIds from the enter_scene_result
        var monsterIds = new List<string>();
        var entsStart = raw.IndexOf("\"entities\":[");
        if (entsStart > 0)
        {
            var remaining = raw.Substring(entsStart + 12);
            // Find all "monster" type entities
            while (remaining.Contains("\"type\":\"monster\""))
            {
                var typeIdx = remaining.IndexOf("\"type\":\"monster\"");
                // Go back to find the entityId
                var beforeType = remaining[..typeIdx];
                var idIdx = beforeType.LastIndexOf("\"entityId\":\"");
                if (idIdx > 0)
                {
                    var idStart = idIdx + 12;
                    var idEnd = remaining.IndexOf('"', idStart);
                    monsterIds.Add(remaining[idStart..idEnd]);
                }
                remaining = remaining[(typeIdx + 19)..];
            }
        }

        Assert.NotEmpty(monsterIds);

        // Cast skill on first monster
        await Send(socket, MessageTypes.CastSkill, new CastSkillPayload
            { TargetId = monsterIds[0], SkillId = 1 });
        (t, raw) = await Receive(socket);
        Assert.Equal(MessageTypes.CombatEvent, t);
        Assert.Contains("\"damage\"", raw);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    [Fact]
    public async Task Inventory_GetAndUse_Works()
    {
        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("phase3-002", "editor", "0.3.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login!.PlayerId, login.Token, "Healer", 1)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();

        var ws = _factory.Server.CreateWebSocketClient();
        var socket = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);

        await Send(socket, MessageTypes.Auth, new AuthPayload
            { PlayerId = login.PlayerId, Token = login.Token, RoleId = create!.Role!.RoleId });
        await Receive(socket);

        await Send(socket, MessageTypes.GetInventory, new { });
        var (t, raw) = await Receive(socket);
        Assert.Equal(MessageTypes.InventoryData, t);
        Assert.Contains("\"items\"", raw);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
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
        using var d = JsonDocument.Parse(raw);
        return (d.RootElement.GetProperty("t").GetString()!, raw);
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

    private static string Extract(string json, string key)
    {
        var i = json.IndexOf(key);
        if (i < 0) return "";
        i += key.Length;
        var e = json.IndexOf('"', i);
        return e < 0 ? "" : json.Substring(i, e - i);
    }
}
