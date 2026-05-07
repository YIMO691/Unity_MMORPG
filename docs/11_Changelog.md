# Changelog

## 2026-05-07 — Scene Lifecycle Fixes

- Fixed monster respawn by broadcasting respawned monsters to clients as `s2c.entity_joined`.
- Prevented repeated scene entry from spawning duplicate baseline monsters.
- Fixed duplicate drop spawn delivery to the killing client, which left unpickable yellow ghost drops.
- Updated Unity client pickup handling to wait for server acknowledgement before removing drops locally.
- Added system chat notices for scene entry, scene transition, monster respawn, and item pickup.
- Cleared chat input focus after sending so movement resumes cleanly.
- Reduced Unity Console output to connection/errors only for routine gameplay messages.
- Replaced boundary-based scene switching with visible fixed portal markers in each scene.
- Throttled monster respawn system notices to avoid repeated chat spam.
- Added immediate local chat echo and server-broadcast confirmation coverage for sent chat messages.
- Fixed chat local echo confirmation for repeated sends and JSON-escaped Unicode text.
- Registered Lua CLR bridge types before exposing `network` and `ui`, and removed routine Lua lifecycle logs.

## 2026-05-07 — Phase 7: World Map / Multiple Scenes

### Server
- Added `field_001` (Wilderness) scene with different spawn point and monsters
- Scene switching: leave old scene (broadcast entity_left), enter new scene
- Different monster spawns per scene (slime/goblin/wolf in city, wolf/goblin in field)

### Client
- Despawn all entities on scene switch, re-spawn from new scene data
- Portal trigger: walk to city boundary → auto-teleport to field; field boundary → back to city
- 3-second cooldown to prevent rapid switching
- Chat and quest UI persist across scene transitions

### Tests — 30/30 passing (3 new)
- Enter field scene directly
- Switch city → field → city
- Invalid scene returns error

## 2026-05-07 — Phase 6: Remote Resource Update

### Server
- `GET /api/resources/manifest` — version manifest with file names, SHA256 hashes, sizes
- `GET /api/resources/{*path}` — serve files from `resources/` directory
- Directory traversal protection
- `ResourceService` scans `resources/` and computes hashes

### Client
- `ResourceManager.cs` — checks manifest, downloads changed files, caches locally
- `GameLauncher` checks for resource updates after health check, before login
- Files cached to `Application.persistentDataPath/resources/`
- SHA256 hash comparison to skip unchanged files

### Tests — 27/27 passing (4 new)
- Manifest returns file list with name + hash
- Download returns file content
- Version file accessible
- Directory traversal blocked (404)

## 2026-05-07 — Phase 5: Lua Hotfix

### Server — MoonSharp Integration
- Installed MoonSharp 2.0 (pure C# Lua 5.2 interpreter) via NuGet
- Quest definitions moved from hardcoded C# to `configs/quests.lua`
- Monster templates moved from hardcoded C# to `configs/monsters.lua`
- `POST /api/admin/reload-config` — reload Lua configs at runtime
- QuestService + MonsterService load from Lua with C# fallback

### Client — LuaManager Rewrite
- Replaced placeholder LuaManager with full MoonSharp VM
- `DoString`, `DoFile`, `Call` methods for Lua execution
- `Reload()` method for hotfix demonstration
- Lua scripts simplified to flow-control modules (login_flow, role_select_flow, city_flow)
- MoonSharp DLL copied to `Assets/Plugins/`

### Tests — 23/23 passing (5 new)
- Lua config loads quests with correct values
- Lua config loads monsters with correct values
- Lua string execution works
- Admin reload endpoint returns OK
- Quest reward matches Lua config

### Hotfix Demo Flow
1. Modify `configs/quests.lua` (change exp/gold rewards)
2. `POST /api/admin/reload-config`
3. New values take effect immediately — no recompilation

## 2026-05-07 — GitHub Repository Standards

- Copied the `github-repo-standards` skill and reusable GitHub templates from `F:\Unity6_AI`.
- Added repository community files: `README.md`, `SECURITY.md`, `CONTRIBUTING.md`, `.editorconfig`, `.gitattributes`, CODEOWNERS, PR template, issue template, and docs index.
- Updated root `.gitignore` to ignore accidental Unity-generated root folders.
- Documented GitHub remote and branch workflow in `docs/workflows/github-setup.md`.
- Rewrote the copied `github-repo-standards` skill for the AI_MMORPG Unity + .NET repository.
- Cleaned accidental Unity-generated root folders after confirming they were ignored and untracked.
- Skipped CI workflow creation because project rules require explicit approval before changing CI/CD shape.

## 2026-05-07 — Unity UI Overlay Fix

- Fixed CityView chat and quest overlays to reuse an existing panel instead of creating duplicate runtime copies.
- Raised CityView canvas sorting order so chat and quest UI stay visible after entering the 3D scene.
- Anchored chat controls to the bottom-left and quest controls to the top-right of the CityView canvas.
- Guarded chat and quest button wiring against duplicate listener registration.

## 2026-05-05 — Phase 4: Quest + Chat

### Server — Quest System
- QuestService: 3 kill quests (Slime x3, Goblin x2, Wolf x1) with exp + gold rewards
- Quest flow: accept → kill target monsters → progress tracking → auto-complete → reward
- Quest progress hooks into monster death in HandleCastSkill
- One active quest per player, quest definitions in static dict (like skill templates)

### Server — Chat System
- ChatService: broadcast messages to all players in a scene
- Message validation (trim whitespace, reject empty)
- Sender name from player entity RoleName

### Server — New Message Types
| Type | Dir | Purpose |
|------|-----|---------|
| c2s.accept_quest / s2c.quest_updated | ⇄ | Accept quest, receive progress |
| s2c.quest_completed | ← | Quest completion with rewards |
| c2s.chat / s2c.chat_broadcast | ⇄ | Send chat, receive broadcast |

### Tests — 18/18 passing (3 new)
- Chat: two clients in same scene, send chat, verify broadcast received by other
- Quest: accept quest, kill monster, verify progress update
- Quest: accept invalid quest ID fails

### Unity Client — Phase 4 Scripts
- `ChatPanel.cs` — chat log overlay (last 20 messages), input field, send button, Enter to send
- `QuestTracker.cs` — quest progress display, accept buttons for 3 quests
- `GameManager.cs` — added chat/quest send methods, message handlers, public events
- `SceneSetup.cs` — wired ChatPanel + QuestTracker into CityView prefab

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
