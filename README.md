# AI MMORPG Demo

Unity + C# + Lua + .NET 9.0 MMORPG vertical-slice demo for a resume portfolio.

## Features

| Phase | Feature | Status |
|-------|---------|--------|
| 1 | Guest login, role create/select, city entry | Done |
| 2 | WebSocket real-time connection, movement sync, entity snapshots | Done |
| 3 | Combat (3 skills), monster AI, drops, inventory (use/equip) | Done |
| 4 | Kill quests (3 quests), scene chat broadcast | Done |

All game state is in-memory (no database dependency). Server-authoritative for combat, loot, inventory, quests. Client sends intent only.

## Quick Start

### Server

```powershell
dotnet run --project server/src/MmoDemo.Gateway/MmoDemo.Gateway.csproj
# http://localhost:5000  |  ws://localhost:5000/ws
```

### Client

Open `client/MmoDemoClient/` in Unity Hub (2022.3 LTS recommended). Open the Bootstrap scene and press Play.

### Tests

```powershell
dotnet test server/MmoDemo.sln
# 18 tests: HTTP API + WebSocket + combat + quest + chat
```

## Architecture

```
Client                          Server
──────                          ──────
Unity C# (UI, rendering)  ←→   ASP.NET Core minimal API
  GameLauncher                    ├─ /health, /api/auth/*, /api/roles/* (HTTP)
  UIManager                       └─ /ws (WebSocket)
  GameManager                       ├─ MessageRouter (dispatch by type)
  WebSocketClient                   ├─ Services (Auth, Combat, Quest, Chat…)
  ChatPanel / QuestTracker          ├─ SceneManager (entities, connections)
                                    └─ In-memory stores (no DB)
```

- Client sends **intent** (`c2s.*`), server is **authoritative**
- WebSocket envelope: `{"t": "<type>", "ts": <unix_ms>, "p": <payload>}`
- C# handles high-frequency systems, Lua is reserved for UI flow / hotfix

## Project Layout

```
client/MmoDemoClient/    Unity project
server/                  .NET 9.0 solution
  src/
    MmoDemo.Gateway/     Minimal API entry point
    MmoDemo.Application/ Business logic (Interfaces/ + Services/ + routing)
    MmoDemo.Contracts/   HTTP models + WebSocket message types/payloads
    MmoDemo.Domain/      Entity, PlayerEntity, Monster, Item, Quest, Role, Scene
    MmoDemo.Infrastructure/  In-memory stores
  tests/
proto/                   Protocol design drafts
docs/                    Architecture, phases, design notes, changelog
configs/                 CSV config templates
```

## Documentation

- [Roadmap](docs/10_Roadmap.md)
- [Changelog](docs/11_Changelog.md)
- [Architecture](docs/architecture/)
- [Phase plans](docs/phases/)
- [Design docs](docs/design/)

## Constraints

- No frameworks without approval. No new tech stack.
- Never commit secrets. `.env`, `*.local.json` are gitignored.
- Do not commit build outputs: `bin/`, `obj/`, `Library/`, `Temp/`, `Build/`, `Builds/`, `Logs/`
- Update `docs/11_Changelog.md` for completed work.
