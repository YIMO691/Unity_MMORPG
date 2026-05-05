namespace MmoDemo.Domain;

/// <summary>
/// A game scene/zone. Tracks which entities are currently inside it.
/// </summary>
public class Scene
{
    public string SceneId { get; init; } = string.Empty;
    public string SceneName { get; init; } = "City";
    public float SpawnX { get; set; }
    public float SpawnY { get; set; }
    public float SpawnZ { get; set; }
    public float BoundsMinX { get; set; } = -50f;
    public float BoundsMaxX { get; set; } = 50f;
    public float BoundsMinZ { get; set; } = -50f;
    public float BoundsMaxZ { get; set; } = 50f;
    public HashSet<string> EntityIds { get; init; } = [];
}
