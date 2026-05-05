using System.Collections.Concurrent;
using MmoDemo.Domain;

namespace MmoDemo.Infrastructure;

public class InMemorySceneStore
{
    private readonly ConcurrentDictionary<string, Scene> _scenes = new();

    public Scene Add(Scene scene)
    {
        _scenes[scene.SceneId] = scene;
        return scene;
    }

    public Scene? Get(string sceneId) =>
        _scenes.TryGetValue(sceneId, out var s) ? s : null;

    public List<Scene> GetAll() => _scenes.Values.ToList();

    public void AddEntity(string sceneId, string entityId)
    {
        if (_scenes.TryGetValue(sceneId, out var scene))
            lock (scene.EntityIds)
                scene.EntityIds.Add(entityId);
    }

    public void RemoveEntity(string sceneId, string entityId)
    {
        if (_scenes.TryGetValue(sceneId, out var scene))
            lock (scene.EntityIds)
                scene.EntityIds.Remove(entityId);
    }
}
