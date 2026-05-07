# AI MMORPG Demo

Unity + C# + Lua + .NET 9.0 MMORPG vertical-slice demo for a resume portfolio.

## Features

| Phase | Feature | Status |
|-------|---------|--------|
| 1 | Guest login, role create/select, city entry | Done |
| 2 | WebSocket real-time connection, movement sync, entity snapshots | Done |
| 3 | Combat (3 skills), monster AI, drops, inventory (use/equip) | Done |
| 4 | Kill quests (3 quests), scene chat broadcast | Done |
| 5 | Lua hotfix (MoonSharp), config-driven quests & monsters | Done |
| 6 | Remote resource update (manifest, download, cache) | Done |
| 7 | World map (2 scenes: city + wilderness, portal travel) | Done |

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
# 30 tests: HTTP API + WebSocket + combat + quest + chat + Lua + resource + scene
```

## Architecture

```
Client                          Server
──────                          ──────
Unity C# (UI, rendering)  ←→   ASP.NET Core minimal API
  GameLauncher                    ├─ /health, /api/auth/*, /api/roles/* (HTTP)
  UIManager                       ├─ /api/resources/* (resource update)
  GameManager                     ├─ /api/admin/reload-config (Lua hotfix)
  WebSocketClient                 └─ /ws (WebSocket)
  ChatPanel / QuestTracker          ├─ MessageRouter (dispatch by type)
  ResourceManager                   ├─ Services (Auth, Combat, Quest, Chat…)
  LuaManager (MoonSharp)            ├─ SceneManager (entities, connections)
  CameraFollow                      └─ In-memory stores (no DB)
```

- Client sends **intent** (`c2s.*`), server is **authoritative**
- WebSocket envelope: `{"t": "<type>", "ts": <unix_ms>, "p": <payload>}`
- C# handles high-frequency systems, Lua handles hotfix configs & UI flow
- Scenes: `city_001` (Main City) + `field_001` (Wilderness), portal travel

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
configs/                 Lua config tables (quests.lua, monsters.lua)
resources/               Remote-updateable resource files
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
