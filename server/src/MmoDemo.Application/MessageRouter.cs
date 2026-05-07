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
    private readonly IQuestService _quests;
    private readonly IChatService _chat;

    public MessageRouter(IAuthService auth, IRoleRepository roles, ISceneManager scenes,
        IMovementService movement, ICombatService combat, MonsterService monsters,
        DropService drops, InventoryService inventory, IQuestService quests, IChatService chat)
    {
        _auth = auth; _roles = roles; _scenes = scenes;
        _movement = movement; _combat = combat;
        _monsters = monsters; _drops = drops; _inventory = inventory;
        _quests = quests; _chat = chat;
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

            // Phase 4
            MessageTypes.AcceptQuest => HandleAcceptQuest(connectionId, payloadJson),
            MessageTypes.Chat => HandleChat(connectionId, payloadJson),

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

        // If already in a different scene, leave old scene first
        if (!string.IsNullOrEmpty(player.SceneId) && player.SceneId != p.SceneId)
        {
            var leaveMsg = MakeResponse(MessageTypes.EntityLeft,
                new EntityLeftPayload { EntityId = player.EntityId });
            _scenes.Broadcast(player.SceneId, cid, leaveMsg);
            _scenes.LeaveScene(player.SceneId, player.EntityId);
        }

        var entered = _scenes.EnterScene(p.SceneId, player);
        if (entered == null)
            return MakeResponse(MessageTypes.EnterSceneResult, new EnterSceneResultPayload { Ok = false });

        // Phase 7: Spawn different monsters per scene
        if (p.SceneId == "field_001")
        {
            _monsters.SpawnMonster(p.SceneId, "wolf", -25, -35);
            _monsters.SpawnMonster(p.SceneId, "wolf", -35, -25);
            _monsters.SpawnMonster(p.SceneId, "goblin", -28, -28);
            _monsters.SpawnMonster(p.SceneId, "goblin", -32, -32);
        }
        else
        {
            _monsters.SpawnMonster(p.SceneId, "slime", 5, 3);
            _monsters.SpawnMonster(p.SceneId, "goblin", -4, -2);
            _monsters.SpawnMonster(p.SceneId, "wolf", 3, -4);
        }

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

            // Remove monster from scene and schedule respawn
            _monsters.MarkDead(monster);
            _scenes.RemoveEntityFromScene(player.SceneId, target.EntityId);

            // Phase 4: Quest progress
            var questUpdate = _quests.OnMonsterKilled(player.PlayerId, monster.TemplateId);
            if (questUpdate != null)
            {
                var def = _quests.GetDefinition(questUpdate.QuestId);
                _scenes.SendToConnection(cid, MakeResponse(MessageTypes.QuestUpdated,
                    new QuestUpdatedPayload
                    {
                        QuestId = questUpdate.QuestId,
                        Name = def?.Name ?? "", Description = def?.Description ?? "",
                        Progress = questUpdate.Progress, TargetCount = def?.TargetCount ?? 0, Ok = true
                    }));

                // Check completion
                var completed = _quests.CheckComplete(player.PlayerId);
                if (completed != null)
                {
                    player.Level += 1;
                    player.Gold += completed.GoldReward;
                    _scenes.SendToConnection(cid, MakeResponse(MessageTypes.QuestCompleted,
                        new QuestCompletedPayload
                        {
                            QuestId = completed.QuestId, Name = completed.Name,
                            ExpReward = completed.ExpReward, GoldReward = completed.GoldReward
                        }));
                }
            }

            // Generate drops and broadcast
            var dropped = _drops.GenerateDrops(monster.DropTableIds.FirstOrDefault());
            foreach (var itemTid in dropped)
            {
                var tpl = _drops.GetTemplate(itemTid);
                if (tpl == null) continue;
                var dropId = $"drop_{Guid.NewGuid():N}";
                _drops.TrackDrop(dropId, itemTid);
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

        // Look up templateId from tracked drops
        var templateId = _drops.GetDropTemplateId(p.DropId);
        if (templateId != null)
        {
            var tpl = _drops.GetTemplate(templateId.Value);
            if (tpl != null)
            {
                _inventory.AddDrop(player.PlayerId, [templateId.Value]);
                player.Gold += Math.Max(1, templateId.Value);
            }
            _drops.RemoveDrop(p.DropId);
        }

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

    // ── Phase 4: Quest ──

    private string HandleAcceptQuest(string cid, string json)
    {
        var p = Deserialize<AcceptQuestPayload>(json);
        var player = _scenes.GetPlayerByConnection(cid);
        if (player == null || p == null) return "{}";

        var state = _quests.AcceptQuest(player.PlayerId, p.QuestId);
        if (state == null)
            return MakeResponse(MessageTypes.QuestUpdated, new QuestUpdatedPayload { Ok = false });

        var def = _quests.GetDefinition(p.QuestId);
        return MakeResponse(MessageTypes.QuestUpdated, new QuestUpdatedPayload
        {
            QuestId = p.QuestId, Name = def?.Name ?? "",
            Description = def?.Description ?? "",
            Progress = 0, TargetCount = def?.TargetCount ?? 0, Ok = true
        });
    }

    // ── Phase 4: Chat ──

    private string HandleChat(string cid, string json)
    {
        var p = Deserialize<ChatPayload>(json);
        var player = _scenes.GetPlayerByConnection(cid);
        if (player == null || p == null || string.IsNullOrWhiteSpace(p.Text)) return "{}";

        var msg = _chat.BuildBroadcast(player.RoleName, p.Text.Trim());
        _scenes.Broadcast(player.SceneId, "", msg);
        return "{}";
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
