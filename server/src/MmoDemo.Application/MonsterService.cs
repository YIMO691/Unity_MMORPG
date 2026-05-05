using MmoDemo.Domain;

namespace MmoDemo.Application;

public class MonsterService
{
    private readonly ISceneManager _sceneManager;
    private static readonly Random _rng = new();
    private int _monsterCounter;

    // Monster templates
    public static readonly Dictionary<string, (string name, int hp, int atk, int def, int exp, int gold, List<int> drops)> Templates = new()
    {
        ["slime"] = ("Slime", 30, 10, 3, 10, 5, [1]),
        ["goblin"] = ("Goblin", 50, 15, 5, 20, 10, [1, 2]),
        ["wolf"] = ("Wolf", 40, 12, 4, 15, 8, [2, 3]),
    };

    public MonsterService(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager;
    }

    public Monster SpawnMonster(string sceneId, string templateId, float x, float z)
    {
        if (!Templates.TryGetValue(templateId, out var tpl)) return null!;

        var monster = new Monster
        {
            EntityId = $"monster_{Interlocked.Increment(ref _monsterCounter)}",
            TemplateId = templateId,
            DisplayName = tpl.name,
            SceneId = sceneId,
            PosX = x, PosY = 0, PosZ = z,
            Hp = tpl.hp, MaxHp = tpl.hp,
            Attack = tpl.atk, Defense = tpl.def,
            ExpReward = tpl.exp, GoldReward = tpl.gold,
            DropTableIds = tpl.drops,
            AiState = MonsterAiState.Patrol,
            PatrolCenterX = x, PatrolCenterZ = z,
            PatrolRadius = 3f,
            ChaseRange = 8f, AttackRange = 2f,
            RespawnSeconds = 15f
        };

        _sceneManager.AddEntity(sceneId, monster);
        return monster;
    }

    public void TickMonsters(string sceneId, float deltaTime)
    {
        var entities = _sceneManager.GetEntities(sceneId);
        var players = entities.OfType<PlayerEntity>().ToList();
        var monsters = entities.OfType<Monster>().ToList();

        foreach (var monster in monsters)
        {
            if (monster.AiState == MonsterAiState.Dead) continue;
            TickMonster(monster, players, deltaTime);
        }
    }

    private void TickMonster(Monster monster, List<PlayerEntity> players, float dt)
    {
        // Find nearest player
        PlayerEntity? nearest = null;
        var nearestDist = float.MaxValue;
        foreach (var p in players)
        {
            var d = Dist(monster, p);
            if (d < nearestDist) { nearestDist = d; nearest = p; }
        }

        switch (monster.AiState)
        {
            case MonsterAiState.Idle:
                monster.AiState = MonsterAiState.Patrol;
                break;

            case MonsterAiState.Patrol:
                if (nearest != null && nearestDist <= monster.ChaseRange)
                {
                    monster.AiState = MonsterAiState.Chase;
                    monster.TargetEntityId = nearest.EntityId;
                }
                else
                {
                    // Wander around patrol center
                    monster.PosX += (float)(_rng.NextDouble() - 0.5) * monster.PatrolSpeed * dt;
                    monster.PosZ += (float)(_rng.NextDouble() - 0.5) * monster.PatrolSpeed * dt;
                    // Clamp to patrol radius
                    var dx = monster.PosX - monster.PatrolCenterX;
                    var dz = monster.PosZ - monster.PatrolCenterZ;
                    if (dx * dx + dz * dz > monster.PatrolRadius * monster.PatrolRadius)
                    {
                        var angle = MathF.Atan2(-dz, -dx);
                        monster.PosX = monster.PatrolCenterX + MathF.Cos(angle) * monster.PatrolRadius * 0.8f;
                        monster.PosZ = monster.PatrolCenterZ + MathF.Sin(angle) * monster.PatrolRadius * 0.8f;
                    }
                }
                break;

            case MonsterAiState.Chase:
                if (nearest == null || nearestDist > monster.ChaseRange * 1.5f)
                {
                    monster.AiState = MonsterAiState.Return;
                    monster.TargetEntityId = null;
                }
                else if (nearestDist <= monster.AttackRange)
                {
                    monster.AiState = MonsterAiState.Attack;
                }
                else
                {
                    // Move toward target
                    var dx2 = nearest.PosX - monster.PosX;
                    var dz2 = nearest.PosZ - monster.PosZ;
                    var len = MathF.Sqrt(dx2 * dx2 + dz2 * dz2);
                    if (len > 0.01f)
                    {
                        monster.PosX += dx2 / len * monster.ChaseSpeed * dt;
                        monster.PosZ += dz2 / len * monster.ChaseSpeed * dt;
                    }
                }
                break;

            case MonsterAiState.Attack:
                if (nearest != null && nearestDist <= monster.AttackRange)
                {
                    // Monster attacks player (simple contact damage)
                    nearest.Hp -= Math.Max(1, monster.Attack - nearest.Level * 2);
                    if (nearest.Hp <= 0)
                    {
                        nearest.Hp = 0;
                        monster.AiState = MonsterAiState.Patrol;
                    }
                }
                else
                {
                    monster.AiState = MonsterAiState.Chase;
                }
                break;

            case MonsterAiState.Return:
                // Move back to patrol center
                var rdx = monster.PatrolCenterX - monster.PosX;
                var rdz = monster.PatrolCenterZ - monster.PosZ;
                var rlen = MathF.Sqrt(rdx * rdx + rdz * rdz);
                if (rlen < 0.5f)
                {
                    monster.PosX = monster.PatrolCenterX;
                    monster.PosZ = monster.PatrolCenterZ;
                    monster.AiState = MonsterAiState.Patrol;
                }
                else
                {
                    monster.PosX += rdx / rlen * monster.PatrolSpeed * dt;
                    monster.PosZ += rdz / rlen * monster.PatrolSpeed * dt;
                }
                break;
        }
    }

    private static float Dist(Entity a, Entity b) =>
        MathF.Sqrt((a.PosX - b.PosX) * (a.PosX - b.PosX) + (a.PosZ - b.PosZ) * (a.PosZ - b.PosZ));
}
