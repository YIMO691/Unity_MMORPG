using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MmoDemo.Contracts;

namespace MmoDemo.Gateway.Tests;

public class Phase2WebSocketTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Phase2WebSocketTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WebSocket_AuthAndEnterScene_Success()
    {
        var client = _factory.CreateClient();

        // Phase 1: HTTP login + create role
        var login = await (await client.PostAsJsonAsync("/api/auth/guest-login",
            new GuestLoginRequest("ws-001", "editor", "0.1.0")))
            .Content.ReadFromJsonAsync<GuestLoginResponse>();
        var create = await (await client.PostAsJsonAsync("/api/roles/create",
            new CreateRoleRequest(login!.PlayerId, login.Token, "Mage", 2)))
            .Content.ReadFromJsonAsync<CreateRoleResponse>();

        // Phase 2: WebSocket
        var ws = _factory.Server.CreateWebSocketClient();
        var socket = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);

        // 1. Auth
        await Send(socket, MessageTypes.Auth, new AuthPayload
            { PlayerId = login.PlayerId, Token = login.Token, RoleId = create!.Role!.RoleId });
        var msg = await Receive(socket);
        Assert.Equal(MessageTypes.AuthResult, msg.Type);
        Assert.Contains("\"ok\":true", msg.Raw);

        // 2. Enter scene
        await Send(socket, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "city_001" });
        msg = await Receive(socket);
        Assert.Equal(MessageTypes.EnterSceneResult, msg.Type);
        Assert.Contains("\"ok\":true", msg.Raw);
        Assert.Contains("city_001", msg.Raw);
        Assert.Contains("\"entities\"", msg.Raw);

        // 3. Ping/Pong
        await Send(socket, MessageTypes.Ping, new { });
        msg = await Receive(socket);
        Assert.Equal(MessageTypes.Pong, msg.Type);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    [Fact]
    public async Task WebSocket_BadAuth_Rejected()
    {
        var ws = _factory.Server.CreateWebSocketClient();
        var socket = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);

        await Send(socket, MessageTypes.Auth, new AuthPayload
            { PlayerId = "bad", Token = "bad", RoleId = "bad" });
        var msg = await Receive(socket);
        Assert.Equal(MessageTypes.AuthResult, msg.Type);
        Assert.Contains("\"ok\":false", msg.Raw);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    [Fact]
    public async Task WebSocket_UnauthenticatedEnterScene_Rejected()
    {
        var ws = _factory.Server.CreateWebSocketClient();
        var socket = await ws.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);

        await Send(socket, MessageTypes.EnterScene, new EnterScenePayload { SceneId = "city_001" });
        var msg = await Receive(socket);
        Assert.Equal(MessageTypes.EnterSceneResult, msg.Type);
        Assert.Contains("\"ok\":false", msg.Raw);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    // ── Helpers ──

    private static async Task Send(WebSocket socket, string type, object payload)
    {
        var json = JsonSerializer.Serialize(new { t = type, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), p = payload });
        await socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<(string Type, string Raw)> Receive(WebSocket socket)
    {
        var buffer = new byte[4096];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
        using var doc = JsonDocument.Parse(raw);
        var type = doc.RootElement.GetProperty("t").GetString()!;
        return (type, raw);
    }
}
