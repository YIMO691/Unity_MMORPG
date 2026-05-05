using MmoDemo.Domain;

namespace MmoDemo.Application;

public class DropService
{
    // Drop tables: template id -> [droppable item template ids with rates]
    public static readonly Dictionary<int, List<(int itemTemplateId, float chance)>> DropTables = new()
    {
        [1] = [(1, 0.6f)],                          // slime drop: potion 60%
        [2] = [(2, 0.4f), (4, 0.3f)],              // goblin drop: sword 40%, armor 30%
        [3] = [(3, 0.5f), (5, 0.3f), (6, 0.2f)],   // wolf drop: bow 50%, shield 30%, ring 20%
    };

    // Item templates
    public static readonly Dictionary<int, ItemTemplate> ItemTemplates = new()
    {
        [1] = new ItemTemplate { TemplateId = 1, Name = "Health Potion", Type = ItemType.Consumable, Value = 20, MaxStack = 10 },
        [2] = new ItemTemplate { TemplateId = 2, Name = "Iron Sword", Type = ItemType.Equipment, Value = 50, Attack = 8 },
        [3] = new ItemTemplate { TemplateId = 3, Name = "Wooden Bow", Type = ItemType.Equipment, Value = 40, Attack = 6 },
        [4] = new ItemTemplate { TemplateId = 4, Name = "Leather Armor", Type = ItemType.Equipment, Value = 30, Defense = 5 },
        [5] = new ItemTemplate { TemplateId = 5, Name = "Iron Shield", Type = ItemType.Equipment, Value = 35, Defense = 8 },
        [6] = new ItemTemplate { TemplateId = 6, Name = "Lucky Ring", Type = ItemType.Equipment, Value = 100, HpBonus = 20 },
        [7] = new ItemTemplate { TemplateId = 7, Name = "Monster Fang", Type = ItemType.Material, Value = 10 },
        [8] = new ItemTemplate { TemplateId = 8, Name = "Wolf Pelt", Type = ItemType.Material, Value = 15 },
    };

    private static readonly Random _rng = new();

    public List<int> GenerateDrops(int dropTableId)
    {
        if (!DropTables.TryGetValue(dropTableId, out var entries))
            return [];

        var dropped = new List<int>();
        foreach (var (itemTemplateId, chance) in entries)
        {
            if (_rng.NextDouble() < chance)
                dropped.Add(itemTemplateId);
        }
        return dropped;
    }

    public ItemTemplate? GetTemplate(int templateId) =>
        ItemTemplates.TryGetValue(templateId, out var t) ? t : null;
}
