using MmoDemo.Domain;

namespace MmoDemo.Application;

public class CombatService : ICombatService
{
    private static readonly Random _rng = new();

    // Simple skill templates
    private static readonly Dictionary<int, (string name, float mult, float range)> _skills = new()
    {
        [1] = ("Slash", 1.0f, 2f),
        [2] = ("Power Strike", 1.5f, 2f),
        [3] = ("Fireball", 2.0f, 6f),
    };

    public CombatResult CastSkill(PlayerEntity caster, Entity target, int skillId)
    {
        if (!_skills.TryGetValue(skillId, out var skill))
            return new CombatResult { Hit = false };

        if (target.Hp <= 0)
            return new CombatResult { Hit = false };

        // Distance check
        var dist = MathF.Sqrt(
            (caster.PosX - target.PosX) * (caster.PosX - target.PosX) +
            (caster.PosZ - target.PosZ) * (caster.PosZ - target.PosZ));
        if (dist > skill.range + 0.5f)
            return new CombatResult { Hit = false };

        // Damage formula: atk * mult - def * 0.5, min 1
        var baseDmg = caster.Level * 5f * skill.mult; // simplified: level-based attack
        var reducedDef = (target as Monster)?.Defense * 0.5f ?? 0;
        var rawDmg = Math.Max(1, baseDmg - reducedDef);
        var variance = (float)(_rng.NextDouble() * 0.2 + 0.9); // 0.9~1.1
        var dmg = (int)(rawDmg * variance);

        // Crit check: 15% chance
        var crit = _rng.NextDouble() < 0.15;
        if (crit) dmg = (int)(dmg * 1.5);

        target.Hp -= dmg;

        var died = target.Hp <= 0;
        var exp = 0;
        var gold = 0;
        var drops = new List<int>();

        if (died && target is Monster monster)
        {
            exp = monster.ExpReward;
            gold = monster.GoldReward;
            drops = monster.DropTableIds;
        }

        return new CombatResult
        {
            Hit = true,
            Damage = dmg,
            Crit = crit,
            TargetDied = died,
            ExpReward = exp,
            GoldReward = gold,
            DroppedItemIds = drops
        };
    }

    public static (string name, float mult, float range)? GetSkillInfo(int skillId) =>
        _skills.TryGetValue(skillId, out var s) ? s : null;
}
