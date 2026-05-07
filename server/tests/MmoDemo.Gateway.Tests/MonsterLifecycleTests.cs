using System.Net.WebSockets;
using MmoDemo.Application;
using MmoDemo.Contracts;
using MmoDemo.Domain;

namespace MmoDemo.Gateway.Tests;

public class MonsterLifecycleTests
{
    [Fact]
    public void Respawn_AddsMonsterAndBroadcastsEntityJoined()
    {
        var scenes = new CapturingSceneManager();
        var service = new MonsterService(scenes, "missing-monsters.lua");
        var monster = service.SpawnMonster("city_001", "slime", 5, 3);
        monster.RespawnSeconds = -1;

        scenes.RemoveEntityFromScene("city_001", monster.EntityId);
        service.MarkDead(monster);
        service.TickRespawn(scenes);

        Assert.Contains(scenes.GetEntities("city_001"), e => e.EntityId == monster.EntityId);
        Assert.Contains(scenes.Messages, m => m.Contains(MessageTypes.EntityJoined) && m.Contains(monster.EntityId));
    }

    [Fact]
    public void EnsureSceneMonsters_SeedsOnlyOnce()
    {
        var scenes = new CapturingSceneManager();
        var service = new MonsterService(scenes, "missing-monsters.lua");

        service.EnsureSceneMonsters("city_001", [("slime", 5, 3), ("goblin", -4, -2)]);
        service.EnsureSceneMonsters("city_001", [("slime", 5, 3), ("goblin", -4, -2)]);

        Assert.Equal(2, scenes.GetEntities("city_001").OfType<Monster>().Count());
    }

    private sealed class CapturingSceneManager : ISceneManager
    {
        private readonly Dictionary<string, List<Entity>> _entities = new()
        {
            ["city_001"] = []
        };

        public List<string> Messages { get; } = [];

        public PlayerEntity? EnterScene(string sceneId, PlayerEntity player)
        {
            AddEntity(sceneId, player);
            return player;
        }

        public void LeaveScene(string sceneId, string entityId) => RemoveEntityFromScene(sceneId, entityId);

        public List<Entity> GetEntities(string sceneId) =>
            _entities.TryGetValue(sceneId, out var entities) ? entities.ToList() : [];

        public Scene? GetScene(string sceneId) => new() { SceneId = sceneId };

        public void AddScene(Scene scene) => _entities.TryAdd(scene.SceneId, []);

        public void Broadcast(string sceneId, string excludeConnectionId, string messageJson) =>
            Messages.Add(messageJson);

        public void SendToConnection(string connectionId, string messageJson) =>
            Messages.Add(messageJson);

        public string? GetConnectionId(string entityId) => null;

        public PlayerEntity? GetPlayerByConnection(string connectionId) => null;

        public void RegisterConnection(string connectionId, PlayerEntity player) { }

        public void UnregisterConnection(string connectionId) { }

        public void TrackConnection(string connectionId, WebSocket socket) { }

        public void AddEntity(string sceneId, Entity entity)
        {
            if (!_entities.TryGetValue(sceneId, out var entities))
            {
                entities = [];
                _entities[sceneId] = entities;
            }

            entities.RemoveAll(e => e.EntityId == entity.EntityId);
            entity.SceneId = sceneId;
            entities.Add(entity);
        }

        public void RemoveEntityFromScene(string sceneId, string entityId)
        {
            if (_entities.TryGetValue(sceneId, out var entities))
                entities.RemoveAll(e => e.EntityId == entityId);
        }
    }
}
