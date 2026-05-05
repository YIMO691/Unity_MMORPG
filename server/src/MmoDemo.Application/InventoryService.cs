using MmoDemo.Domain;

namespace MmoDemo.Application;

public class InventoryService
{
    private readonly Dictionary<string, Inventory> _inventories = new();
    private readonly DropService _drops;

    public InventoryService(DropService drops)
    {
        _drops = drops;
    }

    public Inventory GetOrCreate(string playerId)
    {
        if (!_inventories.TryGetValue(playerId, out var inv))
        {
            inv = new Inventory { PlayerId = playerId };
            _inventories[playerId] = inv;
        }
        return inv;
    }

    public bool AddDrop(string playerId, List<int> dropIds)
    {
        var inv = GetOrCreate(playerId);
        foreach (var tid in dropIds)
        {
            var tpl = _drops.GetTemplate(tid);
            if (tpl == null) continue;
            inv.AddItem(tid, tpl.Name, tpl.Type, 1);
        }
        return true;
    }

    public List<InventoryItem> List(string playerId) =>
        GetOrCreate(playerId).GetAll();

    public bool Use(string playerId, int templateId)
    {
        var inv = GetOrCreate(playerId);
        var tpl = _drops.GetTemplate(templateId);
        if (tpl == null || tpl.Type != ItemType.Consumable)
            return false;

        return inv.RemoveItem(templateId, 1);
    }

    public bool Equip(string playerId, int templateId)
    {
        var inv = GetOrCreate(playerId);
        var tpl = _drops.GetTemplate(templateId);
        if (tpl == null || tpl.Type != ItemType.Equipment) return false;

        var item = inv.Items.FirstOrDefault(i => i.TemplateId == templateId);
        if (item == null) return false;

        // Unequip same type first
        foreach (var other in inv.Items.Where(i => i.IsEquipped && i.Type == ItemType.Equipment))
            other.IsEquipped = false;

        item.IsEquipped = !item.IsEquipped;
        return true;
    }
}
