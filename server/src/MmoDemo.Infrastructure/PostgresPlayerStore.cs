using MmoDemo.Application;
using MmoDemo.Domain;

namespace MmoDemo.Infrastructure;

public class PostgresPlayerStore : IPlayerRepository
{
    private readonly AppDbContext _db;
    public PostgresPlayerStore(AppDbContext db) => _db = db;

    public Player GetOrCreate(string playerId)
    {
        var row = _db.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (row == null)
        {
            row = new PlayerRow { PlayerId = playerId, DeviceId = "", Platform = "" };
            _db.Players.Add(row);
            _db.SaveChanges();
        }
        return new Player { PlayerId = row.PlayerId };
    }

    public Player? Get(string playerId)
    {
        var row = _db.Players.FirstOrDefault(p => p.PlayerId == playerId);
        return row == null ? null : new Player { PlayerId = row.PlayerId };
    }

    public void AddSession(string playerId, PlayerSession session)
    {
        _db.Sessions.Add(new PlayerSessionRow { Token = session.Token, PlayerId = playerId });
        _db.SaveChanges();
    }

    public bool ValidateToken(string playerId, string token)
    {
        return _db.Sessions.Any(s => s.Token == token && s.PlayerId == playerId);
    }
}
