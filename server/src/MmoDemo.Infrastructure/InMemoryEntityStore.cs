using System.Collections.Concurrent;
using MmoDemo.Domain;

namespace MmoDemo.Infrastructure;

public class InMemoryEntityStore
{
    private readonly ConcurrentDictionary<string, Entity> _entities = new();
    private readonly ConcurrentDictionary<string, string> _connectionToEntity = new(); // connectionId → entityId

    public Entity Add(Entity entity)
    {
        _entities[entity.EntityId] = entity;
        return entity;
    }

    public Entity? Get(string entityId) =>
        _entities.TryGetValue(entityId, out var e) ? e : null;

    public Entity? GetByConnection(string connectionId) =>
        _connectionToEntity.TryGetValue(connectionId, out var entityId)
            ? Get(entityId)
            : null;

    public void BindConnection(string connectionId, string entityId) =>
        _connectionToEntity[connectionId] = entityId;

    public void Remove(string entityId)
    {
        _entities.TryRemove(entityId, out _);
        var conn = _connectionToEntity.FirstOrDefault(kv => kv.Value == entityId);
        if (conn.Key != null)
            _connectionToEntity.TryRemove(conn.Key, out _);
    }

    public void RemoveByConnection(string connectionId)
    {
        if (_connectionToEntity.TryRemove(connectionId, out var entityId))
            _entities.TryRemove(entityId, out _);
    }

    public List<Entity> GetByScene(string sceneId) =>
        _entities.Values.Where(e => e.SceneId == sceneId).ToList();

    public int Count => _entities.Count;
}
