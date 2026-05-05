namespace MmoDemo.Domain;

/// <summary>
/// Base entity in the game world. Players, monsters, and NPCs all extend this.
/// </summary>
public class Entity
{
    public string EntityId { get; init; } = string.Empty;
    public EntityType Type { get; init; } = EntityType.Unknown;
    public string SceneId { get; set; } = string.Empty;
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotY { get; set; }
    public int Hp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public float MoveSpeed { get; set; } = 5f;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public enum EntityType
{
    Unknown = 0,
    Player = 1,
    Monster = 2,
    Npc = 3
}

/// <summary>
/// A player entity backed by a WebSocket connection.
/// </summary>
public class PlayerEntity : Entity
{
    public string PlayerId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public int Level { get; set; } = 1;
    public long Gold { get; set; } = 100;

    public PlayerEntity()
    {
        Type = EntityType.Player;
    }
}
