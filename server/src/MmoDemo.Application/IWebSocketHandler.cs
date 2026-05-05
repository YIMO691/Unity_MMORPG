using System.Net.WebSockets;

namespace MmoDemo.Application;

public interface IWebSocketHandler
{
    Task HandleConnectionAsync(WebSocket socket, string connectionId, CancellationToken ct);
}
