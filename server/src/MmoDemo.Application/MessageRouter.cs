using System.Text.Json;
using MmoDemo.Contracts;
using MmoDemo.Domain;

namespace MmoDemo.Application;

public class MessageRouter : IMessageRouter
{
    private readonly IAuthService _auth;
    private readonly IRoleRepository _roles;
    private readonly ISceneManager _scenes;
    private readonly IMovementService _movement;
    private readonly ICombatService _combat;
    private readonly MonsterService _monsters;
    private readonly DropService _drops;
    private readonly InventoryService _inventory;

    public MessageRouter(IAuthService auth, IRoleRepository roles, ISceneManager scenes,
        IMovementService movement, ICombatService combat, MonsterService monsters,
        DropService drops, InventoryService inventory)
    {
        _auth = auth; _roles = roles; _scenes = scenes;
        _movement = movement; _combat = combat;
        _monsters = monsters; _drops = drops; _inventory = inventory;
    }

    public Task<string> HandleMessageAsync(string connectionId, string type, string payloadJson, CancellationToken ct)
    {
        var result = type switch
        {
            MessageTypes.Auth => HandleAuth(connectionId, payloadJson),
            MessageTypes.EnterScene => HandleEnterScene(connectionId, payloadJson),
            MessageTypes.Move => HandleMove(connectionId, payloadJson),
            MessageTypes.Ping => MakeResponse(MessageTypes.Pong, new {}),

            // Phase 3
            MessageTypes.CastSkill => HandleCastSkill(connectionId, payloadJson),
            MessageTypes.PickupItem => HandlePickup(connectionId, payloadJson),
            MessageTypes.GetInventory => HandleGetInventory(connectionId, payloadJson),
            MessageTypes.UseItem => HandleUseItem(connectionId, payloadJson),
            MessageTypes.EquipItem => HandleEquipItem(connectionId, payloadJson),

            _ => MakeResponse(MessageTypes.AuthResult, new AuthResultPayload { Ok = false, Message = $"Unknown: {type}" })
        };
        return Task.FromResult(result);
    }

    // ── Phase 1/2 handlers (unchanged) ──

    private string HandleAuth(string cid, string json)
    {
        var p = Deserialize<AuthPayload>(json);
        if (p == null || !_auth.ValidateToken(p.PlayerId, p.Token))
            return MakeResponse(MessageTypes.AuthResult, new AuthResultPayload { Ok = false, Message = "Auth failed" });

        var role = _roles.Get(p.RoleId);
        if (role == null || role.PlayerId != p.PlayerId)
            return MakeResponse(MessageTypes.AuthResult, new AuthResultPayload { Ok = false, Message = "Role not found" });

        var player = new PlayerEntity
        {
            EntityId = $"entity_{p.RoleId}", PlayerId = p.PlayerId,
            RoleId = p.RoleId, RoleName = role.Name,
            Level = role.Level, Gold = role.Gold, MoveSpeed = 5f
        };
        _scenes.RegisterConnection(cid, player);
        return MakeResponse(MessageTypes.AuthResult, new AuthResultPayload { Ok = true, Message = "OK" });
    }

    private string HandleEnterScene(string cid, string json)
    {
        var p = Deserialize<EnterScenePayload>(json);
        var player = _scenes.GetPlayerByConnection(cid);
        if (player == null || p == null)
            return MakeResponse(MessageTypes.EnterSceneResult, new EnterSceneResultPayload { Ok = false });

        var entered = _scenes.EnterScene(p.SceneId, player);
        if (entered == null)
            return MakeResponse(MessageTypes.EnterSceneResult, new EnterSceneResultPayload { Ok = false });

        // Spawn initial monsters
        _monsters.SpawnMonster(p.SceneId, "slime", 5, 3);
        _monsters.SpawnMonster(p.SceneId, "goblin", -4, -2);
        _monsters.SpawnMonster(p.SceneId, "wolf", 3, -4);

        var entities = _scenes.GetEntities(p.SceneId).Select(ToSnapshot).ToList();
        var joinMsg = MakeResponse(MessageTypes.EntityJoined,
            new EntityJoinedPayload { Entity = ToSnapshot(player) });
        _scenes.Broadcast(p.SceneId, cid, joinMsg);

        return MakeResponse(MessageTypes.EnterSceneResult, new EnterSceneResultPayload
        {
            Ok = true, SceneId = p.SceneId,
            SpawnX = player.PosX, SpawnY = player.PosY, SpawnZ = player.PosZ,
            Entities = entities
        });
    }

    private string HandleMove(string cid, string json)
    {
        var p = Deserialize<MovePayload>(json);
        var player = _scenes.GetPlayerByConnection(cid);
        if (player == null || p == null) return "{}";

        _movement.ValidateAndApply(player, p.DirX, p.DirZ, p.PosX, p.PosZ);

        var snapshot = ToSnapshot(player);
        var msg = MakeResponse(MessageTypes.EntitySnapshot, new { entities = new[] { snapshot } });
        _scenes.Broadcast(player.SceneId, cid, msg);
        return "{}";
    }

    // ── Phase 3: Combat ──

    private string HandleCastSkill(string cid, string json)
    {
        var p = Deserialize<CastSkillPayload>(json);
        var player = _scenes.GetPlayerByConnection(cid);
        if (player == null || p == null) return "{}";

        var sceneEntities = _scenes.GetEntities(player.SceneId);
        var target = sceneEntities.FirstOrDefault(e => e.EntityId == p.TargetId);
        if (target == null)
            return MakeResponse(MessageTypes.CombatEvent, new CombatEventPayload { Damage = 0, TargetDied = false });

        var result = _combat.CastSkill(player, target, p.SkillId);

        // Broadcast combat event to scene
        var evt = MakeResponse(MessageTypes.CombatEvent, new CombatEventPayload
        {
            CasterId = player.EntityId, TargetId = target.EntityId,
            Damage = result.Damage, Crit = result.Crit,
            TargetDied = result.TargetDied, CasterHp = player.Hp
        });
        _scenes.Broadcast(player.SceneId, cid, evt);
        _scenes.SendToConnection(cid, evt);

        // If target (monster) died
        if (result.TargetDied && target is Monster monster)
        {
            // Send death event
            var deathMsg = MakeResponse(MessageTypes.MonsterDeath, new MonsterDeathPayload
            {
                EntityId = target.EntityId, KillerId = player.EntityId,
                ExpReward = result.ExpReward, GoldReward = result.GoldReward
            });
            _scenes.Broadcast(player.SceneId, "", deathMsg);

            // Remove monster from scene
            _scenes.RemoveEntityFromScene(player.SceneId, target.EntityId);

            // Generate drops and broadcast
            var dropped = _drops.GenerateDrops(monster.DropTableIds.FirstOrDefault());
            foreach (var itemTid in dropped)
            {
                var tpl = _drops.GetTemplate(itemTid);
                if (tpl == null) continue;
                var dropId = $"drop_{Guid.NewGuid():N}";
                var dropMsg = MakeResponse(MessageTypes.DropSpawned, new DropSpawnedPayload
                {
                    DropId = dropId, ItemTemplateId = itemTid, ItemName = tpl.Name,
                    PosX = target.PosX, PosZ = target.PosZ
                });
                _scenes.Broadcast(player.SceneId, "", dropMsg);
                _scenes.SendToConnection(cid, dropMsg);
            }
        }

        return "{}";
    }

    // ── Phase 3: Drop & Inventory ──

    private string HandlePickup(string cid, string json)
    {
        var p = Deserialize<PickupItemPayload>(json);
        var player = _scenes.GetPlayerByConnection(cid);
        if (player == null || p == null) return "{}";

        // In a full implementation we'd validate the drop exists and is in range
        // For now, just add to inventory
        // dropId format: "drop_<guid>" — we don't track individual drops yet,
        // so let's use the templateId from the dropId (would come from client)

        var dropMsg = MakeResponse(MessageTypes.DropPickedUp, new DropPickedUpPayload
        {
            DropId = p.DropId, PickedBy = player.EntityId
        });
        _scenes.Broadcast(player.SceneId, cid, dropMsg);
        _scenes.SendToConnection(cid, dropMsg);

        return "{}";
    }

    private string HandleGetInventory(string cid, string json)
    {
        var player = _scenes.GetPlayerByConnection(cid);
        if (player == null) return "{}";

        var items = _inventory.List(player.PlayerId)
            .Select(i => new InventoryItemData
            {
                TemplateId = i.TemplateId, Name = i.Name,
                Type = i.Type.ToString().ToLower(), Quantity = i.Quantity,
                SlotIndex = i.SlotIndex, Equipped = i.IsEquipped
            }).ToList();

        return MakeResponse(MessageTypes.InventoryData, new InventoryDataPayload
        {
            Items = items, PlayerHp = player.Hp
        });
    }

    private string HandleUseItem(string cid, string json)
    {
        var p = Deserialize<UseItemPayload>(json);
        var player = _scenes.GetPlayerByConnection(cid);
        if (player == null || p == null) return "{}";

        var ok = _inventory.Use(player.PlayerId, p.TemplateId);
        // If health potion (id=1), heal
        if (ok && p.TemplateId == 1)
        {
            player.Hp = Math.Min(player.Hp + 20, player.MaxHp);
        }

        return HandleGetInventory(cid, json); // return updated inventory
    }

    private string HandleEquipItem(string cid, string json)
    {
        var p = Deserialize<EquipItemPayload>(json);
        var player = _scenes.GetPlayerByConnection(cid);
        if (player == null || p == null) return "{}";

        _inventory.Equip(player.PlayerId, p.TemplateId);
        return HandleGetInventory(cid, json);
    }

    // ── Helpers ──

    private static EntitySnapshotData ToSnapshot(Entity e) => new()
    {
        EntityId = e.EntityId, Type = e.Type.ToString().ToLower(),
        Name = (e as PlayerEntity)?.RoleName ?? (e as Monster)?.DisplayName ?? "",
        PosX = e.PosX, PosY = e.PosY, PosZ = e.PosZ,
        RotY = e.RotY, Hp = e.Hp, Level = (e as PlayerEntity)?.Level ?? 0
    };

    private static string MakeResponse(string type, object payload) =>
        JsonSerializer.Serialize(new { t = type, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), p = payload });

    private static T? Deserialize<T>(string json) { try { return JsonSerializer.Deserialize<T>(json); } catch { return default; } }
}
