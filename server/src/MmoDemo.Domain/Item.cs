namespace MmoDemo.Domain;

/// <summary>
/// Item template definition (from config table).
/// </summary>
public class ItemTemplate
{
    public int TemplateId { get; init; }
    public string Name { get; init; } = "";
    public ItemType Type { get; init; }
    public int Value { get; init; }
    public int MaxStack { get; init; } = 99;
    public int Attack { get; init; }
    public int Defense { get; init; }
    public int HpBonus { get; init; }
}

public enum ItemType
{
    Consumable = 0,  // potions, scrolls
    Equipment = 1,   // weapon, armor
    Material = 2     // crafting mats
}

/// <summary>
/// An item instance in a player's inventory.
/// </summary>
public class InventoryItem
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = "";
    public ItemType Type { get; set; }
    public int Quantity { get; set; } = 1;
    public int SlotIndex { get; set; }
    public bool IsEquipped { get; set; }
}

/// <summary>
/// Player inventory with fixed slot count.
/// </summary>
public class Inventory
{
    public string PlayerId { get; init; } = "";
    public List<InventoryItem> Items { get; init; } = [];
    public const int MaxSlots = 20;

    public bool AddItem(int templateId, string name, ItemType type, int quantity)
    {
        // Try stack existing
        foreach (var item in Items)
        {
            if (item.TemplateId == templateId && item.Quantity < 99)
            {
                item.Quantity += quantity;
                return true;
            }
        }

        if (Items.Count >= MaxSlots) return false;

        Items.Add(new InventoryItem
        {
            TemplateId = templateId,
            Name = name,
            Type = type,
            Quantity = quantity,
            SlotIndex = Items.Count
        });
        return true;
    }

    public bool RemoveItem(int templateId, int quantity)
    {
        var item = Items.FirstOrDefault(i => i.TemplateId == templateId);
        if (item == null) return false;
        item.Quantity -= quantity;
        if (item.Quantity <= 0) Items.Remove(item);
        return true;
    }

    public List<InventoryItem> GetAll() => [.. Items.OrderBy(i => i.SlotIndex)];
}
