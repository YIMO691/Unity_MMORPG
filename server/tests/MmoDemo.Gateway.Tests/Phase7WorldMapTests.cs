using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MmoDemo.Contracts;

namespace MmoDemo.Gateway.Tests;

public class Phase7WorldMapTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public Phase7WorldMapTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task WorldMap_EnterField_ReturnsCorrectScene()
    {
        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("p7-1", "editor", "0.7.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login!.PlayerId, login.Token, "Explorer", 1)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();

        var ws = _factory.Server.CreateWebSocketClient();
        var s = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
        await Send(s, MessageTypes.Auth, new AuthPayload
            { PlayerId = login.PlayerId, Token = login.Token, RoleId = create!.Role!.RoleId });
        await Receive(s); // auth_result

        // Enter field directly
        await Send(s, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "field_001" });
        var (t, raw) = await Receive(s);
        Assert.Equal(MessageTypes.EnterSceneResult, t);
        Assert.Contains("\"ok\":true", raw);
        Assert.Contains("field_001", raw);
        Assert.Contains("\"entities\":[", raw);

        await s.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    [Fact]
    public async Task WorldMap_SwitchScene_LeavesOldEntersNew()
    {
        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("p7-2", "editor", "0.7.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login!.PlayerId, login.Token, "Traveler", 2)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();

        var ws = _factory.Server.CreateWebSocketClient();
        var s = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
        await Send(s, MessageTypes.Auth, new AuthPayload
            { PlayerId = login.PlayerId, Token = login.Token, RoleId = create!.Role!.RoleId });
        await Receive(s);

        // Enter city
        await Send(s, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "city_001" });
        var (_, cityRaw) = await Receive(s); // enter_scene_result
        var cityMonsterCount = CountMonsters(cityRaw);

        // Switch to field
        await Send(s, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "field_001" });
        var (t, raw) = await Receive(s);
        Assert.Equal(MessageTypes.EnterSceneResult, t);
        Assert.Contains("field_001", raw);

        // Switch back to city
        await Send(s, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "city_001" });
        (t, raw) = await Receive(s);
        Assert.Equal(MessageTypes.EnterSceneResult, t);
        Assert.Contains("city_001", raw);
        Assert.Equal(cityMonsterCount, CountMonsters(raw));

        await s.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    [Fact]
    public async Task WorldMap_InvalidScene_ReturnsError()
    {
        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("p7-3", "editor", "0.7.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login!.PlayerId, login.Token, "Lost", 3)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();

        var ws = _factory.Server.CreateWebSocketClient();
        var s = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);
        await Send(s, MessageTypes.Auth, new AuthPayload
            { PlayerId = login.PlayerId, Token = login.Token, RoleId = create!.Role!.RoleId });
        await Receive(s);

        await Send(s, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "nonexistent" });
        var (t, raw) = await Receive(s);
        Assert.Equal(MessageTypes.EnterSceneResult, t);
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
        using var d = JsonDocument.Parse(raw);
        return (d.RootElement.GetProperty("t").GetString()!, raw);
    }

    private static int CountMonsters(string raw)
    {
        var count = 0;
        var index = 0;
        const string marker = "\"type\":\"monster\"";
        while ((index = raw.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += marker.Length;
        }
        return count;
    }
}
