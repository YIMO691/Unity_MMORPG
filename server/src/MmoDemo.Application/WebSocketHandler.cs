using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MmoDemo.Contracts;

namespace MmoDemo.Application;

public class WebSocketHandler : IWebSocketHandler
{
    private readonly IMessageRouter _router;
    private readonly ISceneManager _sceneManager;

    public WebSocketHandler(IMessageRouter router, ISceneManager sceneManager)
    {
        _router = router;
        _sceneManager = sceneManager;
    }

    public async Task HandleConnectionAsync(WebSocket socket, string connectionId, CancellationToken ct)
    {
        var buffer = new byte[4096];

        _sceneManager.TrackConnection(connectionId, socket);

        try
        {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", ct);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var response = await ProcessMessage(connectionId, json, ct);

                    if (!string.IsNullOrEmpty(response))
                    {
                        var bytes = Encoding.UTF8.GetBytes(response);
                        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
                    }
                }
            }
        }
        catch (WebSocketException) { }
        catch (OperationCanceledException) { }
        finally
        {
            // Cleanup: broadcast departure, remove entity
            var player = _sceneManager.GetPlayerByConnection(connectionId);
            if (player != null)
            {
                var leaveMsg = BuildMessage(MessageTypes.EntityLeft, new EntityLeftPayload
                {
                    EntityId = player.EntityId
                });
                _sceneManager.Broadcast(player.SceneId, connectionId, leaveMsg);
                _sceneManager.UnregisterConnection(connectionId);
            }

            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); }
                catch { }
            }
        }
    }

    private async Task<string> ProcessMessage(string connectionId, string json, CancellationToken ct)
    {
        try
        {
            Console.WriteLine($"[WS] From {connectionId}: {json}");
            var env = JsonSerializer.Deserialize<Envelope>(json);
            if (env == null || string.IsNullOrEmpty(env.Type)) return "";

            var payloadJson = env.Payload?.GetRawText() ?? "{}";
            return await _router.HandleMessageAsync(connectionId, env.Type, payloadJson, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WS] Error: {ex.Message}");
            return ""; // don't send wrong message type back
        }
    }

    private static string BuildMessage(string type, object payload) =>
        JsonSerializer.Serialize(new { t = type, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), p = payload });
}
