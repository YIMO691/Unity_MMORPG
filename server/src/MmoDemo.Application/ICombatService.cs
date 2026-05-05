using MmoDemo.Domain;

namespace MmoDemo.Application;

public interface ICombatService
{
    CombatResult CastSkill(PlayerEntity caster, Entity target, int skillId);
}

public class CombatResult
{
    public bool Hit { get; set; }
    public int Damage { get; set; }
    public bool Crit { get; set; }
    public bool TargetDied { get; set; }
    public int ExpReward { get; set; }
    public int GoldReward { get; set; }
    public List<int> DroppedItemIds { get; set; } = [];
}
