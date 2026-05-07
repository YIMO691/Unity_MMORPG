using System.Collections.Concurrent;
using MoonSharp.Interpreter;
using MmoDemo.Domain;

namespace MmoDemo.Application;

public class MonsterService
{
    public record MonsterTemplate(string Name, int Hp, int Atk, int Def, int Exp, int Gold, List<int> Drops);

    private readonly ISceneManager _sceneManager;
    private static readonly Random _rng = new();
    private int _monsterCounter;
    private Dictionary<string, MonsterTemplate> _templates = new();
    private readonly string _configPath;
    private readonly ConcurrentDictionary<string, (Monster monster, DateTime respawnAt)> _deadMonsters = new();

    public static Dictionary<string, MonsterTemplate> Templates { get; private set; } = new();

    public MonsterService(ISceneManager sceneManager, string configPath = "configs/monsters.lua")
    {
        _sceneManager = sceneManager;
        _configPath = configPath;
        LoadFromLua();
    }

    public void LoadFromLua()
    {
        try
        {
            var script = new Script();
            var result = script.DoFile(_configPath);
            var root = result.Table;
            var temps = new Dictionary<string, MonsterTemplate>();

            foreach (var pair in root.Pairs)
            {
                var key = pair.Key.String;
                var v = pair.Value.Table;

                var drops = new List<int>();
                var dropsTable = v.Get("drops").Table;
                foreach (var dv in dropsTable.Values)
                    drops.Add((int)dv.CastToNumber());

                temps[key] = new MonsterTemplate(
                    v.Get("name").CastToString(),
                    (int)v.Get("hp").CastToNumber(),
                    (int)v.Get("atk").CastToNumber(),
                    (int)v.Get("def").CastToNumber(),
                    (int)v.Get("exp").CastToNumber(),
                    (int)v.Get("gold").CastToNumber(),
                    drops);
            }
            _templates = temps;
            Templates = temps;
        }
        catch
        {
            _templates = new()
            {
                ["slime"] = new("Slime", 30, 10, 3, 10, 5, [1]),
                ["goblin"] = new("Goblin", 50, 15, 5, 20, 10, [1, 2]),
                ["wolf"] = new("Wolf", 40, 12, 4, 15, 8, [2, 3]),
            };
            Templates = _templates;
        }
    }

    public void Reload() => LoadFromLua();

    public void StartRespawnTimer(ISceneManager scenes)
    {
        var timer = new System.Timers.Timer(3000);
        timer.Elapsed += (_, _) => TickRespawn(scenes);
        timer.AutoReset = true;
        timer.Start();
    }

    public void MarkDead(Monster monster)
    {
        _deadMonsters[monster.EntityId] = (monster, DateTime.UtcNow.AddSeconds(monster.RespawnSeconds));
    }

    public void TickRespawn(ISceneManager scenes)
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _deadMonsters)
        {
            if (now >= kv.Value.respawnAt)
            {
                var m = kv.Value.monster;
                m.Hp = m.MaxHp;
                m.AiState = MonsterAiState.Patrol;
                m.PosX = m.PatrolCenterX;
                m.PosZ = m.PatrolCenterZ;
                scenes.AddEntity(m.SceneId, m);
                _deadMonsters.TryRemove(kv.Key, out _);
            }
        }
    }

    public Monster SpawnMonster(string sceneId, string templateId, float x, float z)
    {
        if (!_templates.TryGetValue(templateId, out var tpl)) return null!;

        var monster = new Monster
        {
            EntityId = $"monster_{Interlocked.Increment(ref _monsterCounter)}",
            TemplateId = templateId,
            DisplayName = tpl.Name,
            SceneId = sceneId,
            PosX = x, PosY = 0, PosZ = z,
            Hp = tpl.Hp, MaxHp = tpl.Hp,
            Attack = tpl.Atk, Defense = tpl.Def,
            ExpReward = tpl.Exp, GoldReward = tpl.Gold,
            DropTableIds = tpl.Drops,
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
                    monster.PosX += (float)(_rng.NextDouble() - 0.5) * monster.PatrolSpeed * dt;
                    monster.PosZ += (float)(_rng.NextDouble() - 0.5) * monster.PatrolSpeed * dt;
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
