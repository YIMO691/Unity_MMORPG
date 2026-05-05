namespace MmoDemo.Application;

public interface IMessageRouter
{
    Task<string> HandleMessageAsync(string connectionId, string type, string payloadJson, CancellationToken ct);
}
