namespace MmoDemo.Domain;

/// <summary>
/// Monster entity with AI state and combat stats.
/// </summary>
public class Monster : Entity
{
    public string TemplateId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = "Slime";
    public MonsterAiState AiState { get; set; } = MonsterAiState.Idle;
    public int Attack { get; set; } = 10;
    public int Defense { get; set; } = 3;
    public float ChaseRange { get; set; } = 8f;
    public float AttackRange { get; set; } = 2f;
    public float PatrolSpeed { get; set; } = 2f;
    public float ChaseSpeed { get; set; } = 4f;
    public int ExpReward { get; set; } = 10;
    public int GoldReward { get; set; } = 5;
    public List<int> DropTableIds { get; init; } = [];
    public float PatrolCenterX { get; set; }
    public float PatrolCenterZ { get; set; }
    public float PatrolRadius { get; set; } = 3f;
    public float RespawnSeconds { get; set; } = 10f;
    public string? TargetEntityId { get; set; }

    public Monster()
    {
        Type = EntityType.Monster;
    }
}

public enum MonsterAiState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Return,
    Dead
}
