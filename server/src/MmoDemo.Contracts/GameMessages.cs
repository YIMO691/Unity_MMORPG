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

    // Phase 3: Combat
    public const string CastSkill = "c2s.cast_skill";
    public const string CombatEvent = "s2c.combat_event";

    // Phase 3: Monster
    public const string MonsterSpawned = "s2c.monster_spawned";
    public const string MonsterDeath = "s2c.monster_death";
    public const string MonsterSnapshot = "s2c.monster_snapshot";

    // Phase 3: Drop & Inventory
    public const string DropSpawned = "s2c.drop_spawned";
    public const string PickupItem = "c2s.pickup_item";
    public const string DropPickedUp = "s2c.drop_picked_up";
    public const string GetInventory = "c2s.get_inventory";
    public const string InventoryData = "s2c.inventory_data";
    public const string UseItem = "c2s.use_item";
    public const string EquipItem = "c2s.equip_item";

    // Phase 4: Quest
    public const string AcceptQuest = "c2s.accept_quest";
    public const string QuestUpdated = "s2c.quest_updated";
    public const string QuestCompleted = "s2c.quest_completed";

    // Phase 4: Chat
    public const string Chat = "c2s.chat";
    public const string ChatBroadcast = "s2c.chat_broadcast";
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

    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = "";

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

// ═══════════ Phase 3 Payloads ═══════════

public class CastSkillPayload
{
    [JsonPropertyName("targetId")]
    public string TargetId { get; set; } = "";

    [JsonPropertyName("skillId")]
    public int SkillId { get; set; }
}

public class CombatEventPayload
{
    [JsonPropertyName("casterId")]
    public string CasterId { get; set; } = "";

    [JsonPropertyName("targetId")]
    public string TargetId { get; set; } = "";

    [JsonPropertyName("damage")]
    public int Damage { get; set; }

    [JsonPropertyName("crit")]
    public bool Crit { get; set; }

    [JsonPropertyName("targetDied")]
    public bool TargetDied { get; set; }

    [JsonPropertyName("casterHp")]
    public int CasterHp { get; set; }
}

public class MonsterDeathPayload
{
    [JsonPropertyName("entityId")]
    public string EntityId { get; set; } = "";

    [JsonPropertyName("killerId")]
    public string KillerId { get; set; } = "";

    [JsonPropertyName("expReward")]
    public int ExpReward { get; set; }

    [JsonPropertyName("goldReward")]
    public int GoldReward { get; set; }
}

public class DropSpawnedPayload
{
    [JsonPropertyName("dropId")]
    public string DropId { get; set; } = "";

    [JsonPropertyName("itemTemplateId")]
    public int ItemTemplateId { get; set; }

    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = "";

    [JsonPropertyName("posX")]
    public float PosX { get; set; }

    [JsonPropertyName("posZ")]
    public float PosZ { get; set; }
}

public class PickupItemPayload
{
    [JsonPropertyName("dropId")]
    public string DropId { get; set; } = "";
}

public class DropPickedUpPayload
{
    [JsonPropertyName("dropId")]
    public string DropId { get; set; } = "";

    [JsonPropertyName("pickedBy")]
    public string PickedBy { get; set; } = "";
}

public class InventoryItemData
{
    [JsonPropertyName("templateId")]
    public int TemplateId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("slotIndex")]
    public int SlotIndex { get; set; }

    [JsonPropertyName("equipped")]
    public bool Equipped { get; set; }
}

public class InventoryDataPayload
{
    [JsonPropertyName("items")]
    public List<InventoryItemData> Items { get; set; } = [];

    [JsonPropertyName("playerHp")]
    public int PlayerHp { get; set; }
}

public class UseItemPayload
{
    [JsonPropertyName("templateId")]
    public int TemplateId { get; set; }
}

public class EquipItemPayload
{
    [JsonPropertyName("templateId")]
    public int TemplateId { get; set; }
}

// ═══════════ Phase 4 Payloads: Quest ═══════════

public class AcceptQuestPayload
{
    [JsonPropertyName("questId")]
    public int QuestId { get; set; }
}

public class QuestUpdatedPayload
{
    [JsonPropertyName("questId")]
    public int QuestId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("targetCount")]
    public int TargetCount { get; set; }

    [JsonPropertyName("ok")]
    public bool Ok { get; set; }
}

public class QuestCompletedPayload
{
    [JsonPropertyName("questId")]
    public int QuestId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("expReward")]
    public int ExpReward { get; set; }

    [JsonPropertyName("goldReward")]
    public int GoldReward { get; set; }
}

// ═══════════ Phase 4 Payloads: Chat ═══════════

public class ChatPayload
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public class ChatBroadcastPayload
{
    [JsonPropertyName("senderName")]
    public string SenderName { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
