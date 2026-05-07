# Roadmap

## Phase 0: Foundation — Completed

- AI calling environment available.
- Git repository available.
- Stable root directories.
- Protocol drafts created.
- Config templates created.
- Unity project ready.
- .NET server ready.

## Phase 1: Login / Role / City — Completed

- Unity client opens login UI.
- Server provides guest login (playerId + token).
- Role list, create role (max 4, name 1-12 chars).
- Role selection and city entry.
- HTTP REST API (5 endpoints).

## Phase 2: WebSocket + Movement — Completed

- WebSocket endpoint `/ws` with JSON envelope.
- Entity system: PlayerEntity, EntitySnapshot, EntityType.
- Scene system: enter/leave, entity tracking, broadcasts.
- Movement sync: client sends intent, server validates.
- MessageRouter dispatches by type string.

## Phase 3: Combat + Monsters + Drops + Inventory — Completed

- 3 skills (Slash, Power Strike, Fireball) with range/damage/crit.
- Monster AI: patrol, chase, attack, return states.
- Drop tables with chance-based loot generation.
- Inventory: list, use consumables, equip/unequip items.
- Auto-pickup within 2 units, monster respawn (15s).

## Phase 4: Quest + Chat — Completed

- 3 kill quests (Slime x3, Goblin x2, Wolf x1).
- Quest progress tracked per player, auto-complete with rewards.
- Scene chat broadcast with sender name + timestamp.
- ChatPanel + QuestTracker UI overlays.

## Phase 5: Lua Hotfix — Completed

- MoonSharp 2.0 integrated (pure C# Lua interpreter).
- Quest/monster configs loaded from `configs/*.lua` files.
- `POST /api/admin/reload-config` runtime reload.
- LuaManager with DoString/DoFile/Call/Reload.

## Phase 6: Remote Resource Update — Completed

- `GET /api/resources/manifest` version manifest (SHA256 hashes).
- `GET /api/resources/{path}` file download.
- Client ResourceManager: check updates, download, cache locally.

## Phase 7: World Map — Completed

- Two scenes: `city_001` (Main City), `field_001` (Wilderness).
- Portal travel: walk to markers to switch scenes.
- Per-scene monster spawns, clean entity despawn on transition.
- CameraFollow persists across scene switches.

## Remaining Phases

- Phase 8: Backend deployment (Docker Compose, PostgreSQL, Redis).
- Phase 9: Android build.
