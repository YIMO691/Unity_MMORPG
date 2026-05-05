# Changelog

## 2026-05-05 — Phase 2: Scene + Entity + Movement Sync

### Server — WebSocket Real-time Infrastructure
- Added WebSocket endpoint at `/ws` with full message framing (Envelope pattern)
- Entity system: `Entity`, `PlayerEntity`, `EntityType` domain models
- Scene system: `Scene` domain model with entity tracking
- WebSocketHandler: connection lifecycle, message read loop, cleanup on disconnect
- MessageRouter: dispatches Auth / EnterScene / Move / Ping messages
- SceneManager: singleton managing connections, entities, scenes, and broadcasts
- MovementService: server-authoritative position validation with anti-cheat bounds
- All in-memory, no database dependency

### Server — Message Protocol (JSON over WebSocket)
| Type | Dir | Purpose |
|------|-----|---------|
| c2s.auth / s2c.auth_result | ⇄ | Authenticate player+token+role over WebSocket |
| c2s.enter_scene / s2c.enter_scene_result | ⇄ | Enter scene, receive entity list + spawn position |
| c2s.move | → | Move intent (direction + client position) |
| s2c.entity_snapshot | ← | Broadcast: entity position updates |
| s2c.entity_joined / s2c.entity_left | ← | Broadcast: entity enter/leave scene |

### Tests — 13/13 passing
- 10 Phase 1 tests (HTTP API)
- 3 Phase 2 WebSocket tests (auth + enter scene + ping, bad auth rejected, unauthenticated rejected)

### Unity Client — Phase 2 Scripts
- `WebSocketClient.cs` — ClientWebSocket wrapper with main-thread message dispatch
- `GameManager.cs` — Phase 2 game state: connects WS, spawns entities, handles WASD movement
- `CityView.cs` — Updated to trigger GameManager after Phase 1 city entry
- `SceneSetup.cs` — Updated to create GameManager + player prefabs (blue/red capsules)

## 2026-05-04 — Phase 1 Implementation Complete

### Server Implementation
- Built full layered architecture: Domain → Contracts → Application → Infrastructure → Gateway
- Implemented 5 REST API endpoints:
  - `POST /api/auth/guest-login` — guest login returns playerId + token
  - `POST /api/roles/list` — returns role list for authenticated player
  - `POST /api/roles/create` — creates role (validates name 1-12 chars, classId 1-3, max 4 roles)
  - `POST /api/roles/select` — selects role for city entry
  - `POST /api/scene/enter-city` — enters city, returns role display data
- 10 integration tests (xUnit + WebApplicationFactory) — all passing
- In-memory data stores (PostgreSQL/Redis deferred to later phases)
- End-to-end flow verified: Login → Create Role → Select Role → Enter City

### Unity Client Project Created
- Created Unity 2022.3.62f3c1 project at `client/MmoDemoClient/`
- C# client scripts: GameLauncher, NetworkManager (UnityWebRequest-based), UIManager, LuaManager, LoginView, RoleSelectView, CityView
- Editor script: SceneSetup — auto-generates Bootstrap scene + UI prefabs
- Lua scripts: login_flow.lua, role_select_flow.lua, city_flow.lua (in Resources/Lua)
- UI prefabs: LoginView, RoleSelectView, CityView
- Bootstrap scene: GameLauncher + EventSystem, wired to all prefabs

### Documentation
- Created comprehensive `CLAUDE.md` — AI agent reference for the project
- Created `docs/27_Project_Engineering_Plan.md` — project planning document

## 2026-04-30

### Project Rules

- Refined `AGENTS.md` task boundaries, validation guidance, and generated-output constraints.
- Fixed mojibake in `AGENTS.md` and verified `docs/08_AI_Task_Rules.md` is readable UTF-8 Chinese text.
- Expanded `.gitignore` for .NET build outputs, Unity generated folders, and local package/build artifacts.

## 2026-04-28

### Phase 1

- Phase 1 Task 5: completed empty city screen design, supplemented scene.proto, city interface documentation and city entry sequence notes, without implementing city business.
- Added a Qwen context check script and verified Task 5 scope understanding before city design generation.
- Added Qwen project context and task rules to reduce scope drift before Phase 1 Task 5.
- Phase 1 Task 4: fixed role selection design by aligning `role.proto` with the shared proto namespace, using `POST /api/roles/list`, removing out-of-scope city/WebSocket steps, and repairing Markdown/changelog text.
- Phase 1 Task 4: completed role selection flow design, including `role.proto`, role interface documentation, and role selection sequence notes, without implementing role business logic.
- Phase 1 Task 3: completed login protocol and interface design, including `auth.proto`, login interface documentation, and login sequence notes, without implementing login business logic.
- Phase 1 Task 2: prepared the Unity client skeleton documentation, including client directory planning, module boundaries, and Bootstrap/Login/RoleSelect/City responsibilities.
- Phase 1 Task 1: created the server skeleton with .NET solution, Gateway/API, layered projects, test project, and `/health` endpoint.
- Prepared Phase 1 Login / Role / City execution plan and environment checklist.
- Added copy-ready Codex prompts for Phase 1 server skeleton, Unity skeleton planning, login design, role design, and city design.
- Updated roadmap to mark Phase 0 completed and Phase 1 preparing.

### Phase 0

- Re-verified Phase 0 foundation against AGENTS.md, MMORPG_AI_Agent_Workflow.md, AI task rules, model routing rules, and setup guide.
- Confirmed required directories, documentation, protocol drafts, and config templates are present for Phase 0.
- Initialized Phase 0 foundation documentation.
- Added root directory placeholders for client, server, tools, deploy, and source assets.
- Added minimal protocol drafts for auth, role, scene, movement, combat, inventory, quest, and chat.
- Added config table templates for roles, scenes, monsters, skills, items, drops, and quests.
- Documented server-authoritative architecture, client intent rules, Lua hotfix boundaries, and first-version exclusions.
