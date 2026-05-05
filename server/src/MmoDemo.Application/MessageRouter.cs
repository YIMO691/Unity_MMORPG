using System.Text.Json;
using MmoDemo.Contracts;
using MmoDemo.Domain;

namespace MmoDemo.Application;

public class MessageRouter : IMessageRouter
{
    private readonly IAuthService _auth;
    private readonly IRoleRepository _roles;
    private readonly ISceneManager _sceneManager;
    private readonly IMovementService _movement;

    public MessageRouter(IAuthService auth, IRoleRepository roles, ISceneManager sceneManager, IMovementService movement)
    {
        _auth = auth;
        _roles = roles;
        _sceneManager = sceneManager;
        _movement = movement;
    }

    public Task<string> HandleMessageAsync(string connectionId, string type, string payloadJson, CancellationToken ct)
    {
        var result = type switch
        {
            MessageTypes.Auth => HandleAuth(connectionId, payloadJson),
            MessageTypes.EnterScene => HandleEnterScene(connectionId, payloadJson),
            MessageTypes.Move => HandleMove(connectionId, payloadJson),
            MessageTypes.Ping => MakeResponse(MessageTypes.Pong, new {}),
            _ => MakeResponse(MessageTypes.AuthResult, new AuthResultPayload { Ok = false, Message = $"Unknown: {type}" })
        };
        return Task.FromResult(result);
    }

    private string HandleAuth(string connectionId, string payloadJson)
    {
        var p = Deserialize<AuthPayload>(payloadJson);
        if (p == null || !_auth.ValidateToken(p.PlayerId, p.Token))
            return MakeResponse(MessageTypes.AuthResult, new AuthResultPayload { Ok = false, Message = "Auth failed" });

        var role = _roles.Get(p.RoleId);
        if (role == null || role.PlayerId != p.PlayerId)
            return MakeResponse(MessageTypes.AuthResult, new AuthResultPayload { Ok = false, Message = "Role not found" });

        var player = new PlayerEntity
        {
            EntityId = $"entity_{p.RoleId}",
            PlayerId = p.PlayerId,
            RoleId = p.RoleId,
            RoleName = role.Name,
            Level = role.Level,
            Gold = role.Gold,
            MoveSpeed = 5f
        };

        _sceneManager.RegisterConnection(connectionId, player);
        return MakeResponse(MessageTypes.AuthResult, new AuthResultPayload { Ok = true, Message = "OK" });
    }

    private string HandleEnterScene(string connectionId, string payloadJson)
    {
        var p = Deserialize<EnterScenePayload>(payloadJson);
        var player = _sceneManager.GetPlayerByConnection(connectionId);
        if (player == null || p == null)
            return MakeResponse(MessageTypes.EnterSceneResult, new EnterSceneResultPayload { Ok = false });

        var entered = _sceneManager.EnterScene(p.SceneId, player);
        if (entered == null)
            return MakeResponse(MessageTypes.EnterSceneResult, new EnterSceneResultPayload { Ok = false, SceneId = p.SceneId });

        var entities = _sceneManager.GetEntities(p.SceneId).Select(ToSnapshot).ToList();

        var joinMsg = MakeResponse(MessageTypes.EntityJoined,
            new EntityJoinedPayload { Entity = ToSnapshot(player) });
        _sceneManager.Broadcast(p.SceneId, connectionId, joinMsg);

        return MakeResponse(MessageTypes.EnterSceneResult, new EnterSceneResultPayload
        {
            Ok = true, SceneId = p.SceneId,
            SpawnX = player.PosX, SpawnY = player.PosY, SpawnZ = player.PosZ,
            Entities = entities
        });
    }

    private string HandleMove(string connectionId, string payloadJson)
    {
        var p = Deserialize<MovePayload>(payloadJson);
        var player = _sceneManager.GetPlayerByConnection(connectionId);
        if (player == null || p == null) return "{}";

        _movement.ValidateAndApply(player, p.DirX, p.DirZ, p.PosX, p.PosZ);

        var snapshot = ToSnapshot(player);
        var msg = MakeResponse(MessageTypes.EntitySnapshot, new { entities = new[] { snapshot } });
        _sceneManager.Broadcast(player.SceneId, connectionId, msg);

        return "{}";
    }

    private static EntitySnapshotData ToSnapshot(Entity e)
    {
        var pe = e as PlayerEntity;
        return new EntitySnapshotData
        {
            EntityId = e.EntityId,
            Type = e.Type.ToString().ToLower(),
            Name = pe?.RoleName ?? "",
            PosX = e.PosX, PosY = e.PosY, PosZ = e.PosZ,
            RotY = e.RotY, Hp = e.Hp, Level = pe?.Level ?? 0
        };
    }

    private static string MakeResponse(string type, object payload) =>
        JsonSerializer.Serialize(new { t = type, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), p = payload });

    private static T? Deserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json); }
        catch { return default; }
    }
}
