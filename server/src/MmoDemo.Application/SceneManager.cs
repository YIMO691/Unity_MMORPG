using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using MmoDemo.Domain;

namespace MmoDemo.Application;

public class SceneManager : ISceneManager
{
    private readonly ConcurrentDictionary<string, Scene> _scenes = new();
    private readonly ConcurrentDictionary<string, Entity> _entities = new();
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ConcurrentDictionary<string, string> _entityToConnection = new();
    private readonly ConcurrentDictionary<string, string> _connectionToEntity = new();

    public PlayerEntity? EnterScene(string sceneId, PlayerEntity player)
    {
        if (!_scenes.TryGetValue(sceneId, out var scene)) return null;

        player.SceneId = sceneId;
        player.PosX = scene.SpawnX;
        player.PosY = scene.SpawnY;
        player.PosZ = scene.SpawnZ;

        _entities[player.EntityId] = player;
        lock (scene.EntityIds)
            scene.EntityIds.Add(player.EntityId);

        return player;
    }

    public void LeaveScene(string? sceneId, string entityId)
    {
        if (sceneId != null && _scenes.TryGetValue(sceneId, out var scene))
        {
            lock (scene.EntityIds)
                scene.EntityIds.Remove(entityId);
        }
        _entities.TryRemove(entityId, out _);
    }

    public List<Entity> GetEntities(string sceneId)
    {
        if (!_scenes.TryGetValue(sceneId, out var scene)) return [];
        lock (scene.EntityIds)
            return scene.EntityIds
                .Select(id => _entities.TryGetValue(id, out var e) ? e : null)
                .Where(e => e != null)
                .ToList()!;
    }

    public Scene? GetScene(string sceneId) =>
        _scenes.TryGetValue(sceneId, out var s) ? s : null;

    public void AddScene(Scene scene) => _scenes[scene.SceneId] = scene;

    public void AddEntity(string sceneId, Entity entity)
    {
        entity.SceneId = sceneId;
        _entities[entity.EntityId] = entity;
        if (_scenes.TryGetValue(sceneId, out var scene))
            lock (scene.EntityIds)
                scene.EntityIds.Add(entity.EntityId);
    }

    public void RemoveEntityFromScene(string sceneId, string entityId)
    {
        if (_scenes.TryGetValue(sceneId, out var scene))
            lock (scene.EntityIds)
                scene.EntityIds.Remove(entityId);
        _entities.TryRemove(entityId, out _);
    }

    public void RegisterConnection(string connectionId, PlayerEntity player)
    {
        _entities[player.EntityId] = player;
        _entityToConnection[player.EntityId] = connectionId;
        _connectionToEntity[connectionId] = player.EntityId;
    }

    public void UnregisterConnection(string connectionId)
    {
        if (_connectionToEntity.TryRemove(connectionId, out var entityId))
        {
            _entityToConnection.TryRemove(entityId, out _);
            if (_entities.TryRemove(entityId, out var entity))
            {
                if (_scenes.TryGetValue(entity.SceneId, out var scene))
                    lock (scene.EntityIds)
                        scene.EntityIds.Remove(entityId);
            }
        }
        _connections.TryRemove(connectionId, out _);
    }

    public string? GetConnectionId(string entityId) =>
        _entityToConnection.TryGetValue(entityId, out var cid) ? cid : null;

    public PlayerEntity? GetPlayerByConnection(string connectionId)
    {
        if (!_connectionToEntity.TryGetValue(connectionId, out var entityId)) return null;
        return _entities.TryGetValue(entityId, out var e) ? e as PlayerEntity : null;
    }

    public void TrackConnection(string connectionId, WebSocket socket) =>
        _connections[connectionId] = socket;

    public async void Broadcast(string sceneId, string excludeConnectionId, string messageJson)
    {
        var bytes = Encoding.UTF8.GetBytes(messageJson);
        if (!_scenes.TryGetValue(sceneId, out var scene)) return;

        string[] entityIds;
        lock (scene.EntityIds) entityIds = scene.EntityIds.ToArray();

        foreach (var eid in entityIds)
        {
            if (!_entityToConnection.TryGetValue(eid, out var cid)) continue;
            if (cid == excludeConnectionId) continue;
            if (!_connections.TryGetValue(cid, out var ws) || ws.State != WebSocketState.Open) continue;

            try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
            catch { }
        }
    }

    public async void SendToConnection(string connectionId, string messageJson)
    {
        if (!_connections.TryGetValue(connectionId, out var ws) || ws.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(messageJson);
        try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); }
        catch { }
    }
}
