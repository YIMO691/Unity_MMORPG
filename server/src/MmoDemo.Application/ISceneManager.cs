using System.Net.WebSockets;
using MmoDemo.Domain;

namespace MmoDemo.Application;

public interface ISceneManager
{
    PlayerEntity? EnterScene(string sceneId, PlayerEntity player);
    void LeaveScene(string sceneId, string entityId);
    List<Entity> GetEntities(string sceneId);
    Scene? GetScene(string sceneId);
    void AddScene(Scene scene);
    void Broadcast(string sceneId, string excludeConnectionId, string messageJson);
    void SendToConnection(string connectionId, string messageJson);
    string? GetConnectionId(string entityId);
    PlayerEntity? GetPlayerByConnection(string connectionId);
    void RegisterConnection(string connectionId, PlayerEntity player);
    void UnregisterConnection(string connectionId);
    void TrackConnection(string connectionId, WebSocket socket);
    void AddEntity(string sceneId, Entity entity);
    void RemoveEntityFromScene(string sceneId, string entityId);
}
