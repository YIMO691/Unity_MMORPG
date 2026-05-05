# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity + C# + Lua + .NET 9.0 MMORPG Demo (resume portfolio vertical slice). Currently at Phase 3: combat, monsters, drops, and inventory over WebSocket.

## Commands

### Server

```powershell
dotnet build server/MmoDemo.sln
dotnet run --project server/src/MmoDemo.Gateway/MmoDemo.Gateway.csproj   # http://localhost:5000
dotnet test server/MmoDemo.sln                                            # xUnit + WebApplicationFactory<T>
```

## Architecture

### Server Layer Dependency (top-down)

```
Gateway (Program.cs, minimal API) → Application (services + message routing)
Gateway → Contracts (API models + WebSocket payloads)
Application → Contracts
Infrastructure (in-memory stores) → Application + Domain
```

- **Gateway** (`MmoDemo.Gateway`): ASP.NET Core minimal API. Maps HTTP endpoints (`/health`, `/api/auth/guest-login`, `/api/roles/*`, `/api/scene/enter-city`) and a single WebSocket endpoint (`/ws`). All DI registration and initial scene setup lives in `Program.cs`.
- **Application** (`MmoDemo.Application`): Business logic services (`AuthService`, `RoleService`, `SceneService`, `MovementService`, `CombatService`, `InventoryService`, `DropService`, `MonsterService`), the `MessageRouter` that dispatches WebSocket messages by type string, and `SceneManager` that tracks entities, connections, and scene membership.
- **Contracts** (`MmoDemo.Contracts`): HTTP request/response records (`ApiModels.cs`) and WebSocket message type constants plus strongly-typed payload classes (`GameMessages.cs`). The WebSocket envelope is `{"t": "<type>", "ts": <unix_ms>, "p": <payload>}`.
- **Domain** (`MmoDemo.Domain`): Entity model — `Entity` base class, `PlayerEntity`, `Monster`, `Scene`, `Item`, `Role`. `EntityType` enum: Player, Monster, Npc.
- **Infrastructure** (`MmoDemo.Infrastructure`): Thread-safe in-memory stores (`InMemoryPlayerStore`, `InMemoryRoleStore`, `InMemoryEntityStore`, `InMemorySceneStore`).

### Client Layer

```
GameLauncher → NetworkManager (HTTP) + WebSocketClient (real-time)
             → UIManager → LoginView / RoleSelectView / CityView / GameManager
             → LuaManager (xLua bridge placeholder)
```

- **Phase 1 flow**: `GameLauncher` → `NetworkManager` (REST calls) → `LoginView` → `RoleSelectView` → `CityView`
- **Phase 2/3 flow**: After role select, `GameManager.Connect()` opens a `WebSocketClient`, sends `c2s.auth`, then `c2s.enter_scene`. All real-time messages route through `OnWsMessage` switch.
- `WebSocketClient` runs a receive loop on a background thread, enqueues callbacks to the Unity main thread via `Update()`.
- `GameManager` uses manual string-scanning JSON helpers (no `JsonUtility` or `Newtonsoft.Json` dependency) for performance in the hot path.

### WebSocket Message Flow

Client sends `{"t": "c2s.xxx", "ts": ..., "p": {...}}` → Server `WebSocketHandler` deserializes to `Envelope` → `MessageRouter.HandleMessageAsync` dispatches by type string → service logic → response enqueued and sent back. Server broadcasts entity snapshots, joins, leaves, combat events, and drop spawns to all connections in a scene.

### Client-Server Authority

1. Client is responsible for rendering, UI, input, local presentation, resource loading, and sending network messages.
2. Server is authoritative for movement validation, combat results, inventory, loot, quest progress, and rewards.
3. Client must only send **intent**, never final result.
4. C# handles low-level, high-frequency, performance-sensitive logic.
5. Lua handles UI flow, quests, activities, tutorial, and lightweight hotfix.

### Message Naming Convention

- Client requests: `c2s.<action>` (e.g., `c2s.auth`, `c2s.move`, `c2s.cast_skill`)
- Server responses: `s2c.<action>_result` or `s2c.<event>` (e.g., `s2c.auth_result`, `s2c.combat_event`, `s2c.entity_snapshot`)

## Code Conventions

- C# private fields: `_camelCase`, types/methods: PascalCase.
- Lua files: `lowercase_snake_case`.
- Server namespace: `MmoDemo.*`; client namespace: `MmoDemo.Client.*`.
- Proto files use `package mmorpg` with `option csharp_namespace = "AIMMO.Proto";`.
- Resource prefab naming: `UI_<Feature>_<Name>`, `Char_<Name>`, `Monster_<Name>`, `Map_<SceneName>`.

## Constraints

- Do not introduce new frameworks without explicit approval. Do not change the tech stack.
- Never hardcode secrets. Never store API keys in code.
- `.env` is gitignored (use `.env.example` as template). `*.local.json` is gitignored.
- Keep module names consistent with `docs/`, `proto/`, `client/`, and `server/`.
- Update `docs/11_Changelog.md` for each completed phase/task.
- Do not commit generated folders or build outputs: .NET `bin/obj`, Unity `Library/`, `Temp/`, `Obj/`, `Logs/`, `Build/`, `Builds/`, or exported packages.
- For server code changes, run `dotnet test server/MmoDemo.sln` when feasible.
