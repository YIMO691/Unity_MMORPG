using System.Text.Json.Serialization;

namespace MmoDemo.Contracts;

/// <summary>
/// WebSocket message envelope. All messages are wrapped in this.
/// </summary>
public class Envelope
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }

    [JsonPropertyName("p")]
    public System.Text.Json.JsonElement? Payload { get; set; }
}

/// <summary>
/// Message type constants for Phase 2 real-time gameplay.
/// </summary>
public static class MessageTypes
{
    public const string Auth = "c2s.auth";
    public const string AuthResult = "s2c.auth_result";

    public const string EnterScene = "c2s.enter_scene";
    public const string EnterSceneResult = "s2c.enter_scene_result";

    public const string Move = "c2s.move";
    public const string EntitySnapshot = "s2c.entity_snapshot";

    public const string EntityJoined = "s2c.entity_joined";
    public const string EntityLeft = "s2c.entity_left";

    public const string Ping = "c2s.ping";
    public const string Pong = "s2c.pong";
}

// ── Payloads ──

public class AuthPayload
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = "";

    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("roleId")]
    public string RoleId { get; set; } = "";
}

public class AuthResultPayload
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

public class EnterScenePayload
{
    [JsonPropertyName("sceneId")]
    public string SceneId { get; set; } = "";
}

public class EnterSceneResultPayload
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("sceneId")]
    public string SceneId { get; set; } = "";

    [JsonPropertyName("spawnX")]
    public float SpawnX { get; set; }

    [JsonPropertyName("spawnY")]
    public float SpawnY { get; set; }

    [JsonPropertyName("spawnZ")]
    public float SpawnZ { get; set; }

    [JsonPropertyName("entities")]
    public List<EntitySnapshotData> Entities { get; set; } = [];
}

public class MovePayload
{
    [JsonPropertyName("dirX")]
    public float DirX { get; set; }

    [JsonPropertyName("dirY")]
    public float DirY { get; set; }

    [JsonPropertyName("dirZ")]
    public float DirZ { get; set; }

    [JsonPropertyName("posX")]
    public float PosX { get; set; }

    [JsonPropertyName("posY")]
    public float PosY { get; set; }

    [JsonPropertyName("posZ")]
    public float PosZ { get; set; }
}

public class EntitySnapshotData
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "player";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("posX")]
    public float PosX { get; set; }

    [JsonPropertyName("posY")]
    public float PosY { get; set; }

    [JsonPropertyName("posZ")]
    public float PosZ { get; set; }

    [JsonPropertyName("rotY")]
    public float RotY { get; set; }

    [JsonPropertyName("hp")]
    public int Hp { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }
}

public class EntityJoinedPayload
{
    [JsonPropertyName("entity")]
    public EntitySnapshotData Entity { get; set; } = new();
}

public class EntityLeftPayload
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = "";
}
