using MmoDemo.Application;
using MmoDemo.Domain;

namespace MmoDemo.Infrastructure;

public class PostgresRoleStore : IRoleRepository
{
    private readonly AppDbContext _db;
    public PostgresRoleStore(AppDbContext db) => _db = db;

    public Role Create(string roleId, string playerId, string name, int classId)
    {
        var row = new RoleRow
        {
            RoleId = roleId,
            PlayerId = playerId,
            Name = name,
            ClassId = classId,
            Level = 1,
            Gold = 100,
        };
        _db.Roles.Add(row);
        _db.SaveChanges();

        return new Role { RoleId = roleId, PlayerId = playerId, Name = name, ClassId = classId, Level = 1, SceneId = 1001, Gold = 100 };
    }

    public Role? Get(string roleId)
    {
        var row = _db.Roles.FirstOrDefault(r => r.RoleId == roleId);
        return row == null ? null : ToDomain(row);
    }

    public List<Role> GetByPlayer(string playerId)
    {
        return _db.Roles.Where(r => r.PlayerId == playerId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => ToDomain(r))
            .ToList();
    }

    public int CountByPlayer(string playerId)
    {
        return _db.Roles.Count(r => r.PlayerId == playerId);
    }

    private static Role ToDomain(RoleRow r) => new()
    {
        RoleId = r.RoleId,
        PlayerId = r.PlayerId,
        Name = r.Name,
        Level = r.Level,
        ClassId = r.ClassId,
        Gold = r.Gold,
    };
}
