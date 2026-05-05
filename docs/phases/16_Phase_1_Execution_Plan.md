# Phase 1 Execution Plan: Login / Role / City

Phase 1 builds the first runnable vertical slice after the Phase 0 foundation. Codex remains the repository execution agent. Qwen-Coder is the primary AI provider for coding and design assistance. OpenAI, Kimi, DeepSeek, and MiMo are not required for this phase and must not be treated as available until configured and verified.

## Total Goal

Run the first minimal flow:

```text
Start client
-> Login
-> Receive playerId/token
-> Enter role selection
-> Create or select role
-> Enter empty city screen
-> Show role name, level, and gold
```

## Out Of Scope

- Combat.
- Inventory.
- Quest.
- Chat.
- Movement sync.
- Monster AI.
- Loot.
- PostgreSQL or Redis integration.
- Full auth, payment, anti-cheat, guild, auction house, seamless world, or massive same-screen concurrency.
- Complete Unity project creation before Unity is installed and confirmed by the user.

## Task Split

### Task 1: Server Skeleton

Goal:
- Create a .NET solution.
- Create a Gateway/API project.
- Add `GET /health`.
- Create basic server directories.
- Do not connect PostgreSQL or Redis.
- Do not implement login business logic.

Allowed directories:
- `server/`
- `docs/11_Changelog.md`

Forbidden directories and files:
- `.env`
- `.venv/`
- `.tools/`
- `ai/providers/`
- `client/`
- `proto/` unless a blocking issue is found and documented first.

Acceptance:
- `dotnet run` can start the server.
- `GET /health` returns `OK`.
- `server/README.md` explains how to run the server.
- `docs/11_Changelog.md` is updated.

Suggested Codex prompt:
- Use `ai/prompts/phase1/task1_server_skeleton.md`.

### Task 2: Unity Skeleton Planning

Goal:
- Prepare Unity project directory planning.
- Document Bootstrap, Login, RoleSelect, and City directory design.
- Do not create real Unity project files unless Unity is installed and the user explicitly confirms.
- Do not import assets.

Allowed directories:
- `client/`
- `docs/`
- `ai/prompts/phase1/`

Forbidden directories and files:
- `.env`
- `.venv/`
- `.tools/`
- `ai/providers/`
- `server/`
- Generated Unity `Library/`, `Temp/`, `Obj/`, or build output directories.

Acceptance:
- `client/README.md` explains how to create the Unity project.
- Docs include client directory design.
- GameLauncher, LoginView, RoleSelectView, and CityView responsibilities are clear.
- `docs/11_Changelog.md` is updated.

Suggested Codex prompt:
- Use `ai/prompts/phase1/task2_unity_skeleton.md`.

### Task 3: Login Protocol And Interface Design

Goal:
- Review `proto/auth.proto`.
- Confirm login request and response structure.
- Design server login interface.
- Design client call flow.
- Keep this task design-only.

Allowed directories:
- `docs/`
- `proto/`

Forbidden directories and files:
- `.env`
- `.venv/`
- `.tools/`
- `ai/providers/`
- `client/` implementation files.
- `server/` implementation files.

Acceptance:
- Docs include login sequence notes.
- `proto/auth.proto` satisfies Phase 1 login needs.
- No database is implemented.
- No complex auth is implemented.
- `docs/11_Changelog.md` is updated.

Suggested Codex prompt:
- Use `ai/prompts/phase1/task3_login_design.md`.

### Task 4: Role Selection Flow Design

Goal:
- Review `proto/role.proto`.
- Design role list, create-role, and select-role flows.
- Design client UI states.
- Design server response data shape.

Allowed directories:
- `docs/`
- `proto/`

Forbidden directories and files:
- `.env`
- `.venv/`
- `.tools/`
- `ai/providers/`
- `client/` implementation files.
- `server/` implementation files.

Acceptance:
- Docs include role selection sequence notes.
- `proto/role.proto` satisfies Phase 1 role selection needs.
- Minimum role fields are clear: `roleId`, `name`, `level`, `classId`, `sceneId`.
- `docs/11_Changelog.md` is updated.

Suggested Codex prompt:
- Use `ai/prompts/phase1/task4_role_design.md`.

### Task 5: Empty City Screen Design

Goal:
- Review `proto/scene.proto`.
- Design enter-city flow.
- Design empty city UI display fields.
- Do not implement movement, combat, NPCs, or quests.

Allowed directories:
- `docs/`
- `proto/`

Forbidden directories and files:
- `.env`
- `.venv/`
- `.tools/`
- `ai/providers/`
- `client/` implementation files.
- `server/` implementation files.

Acceptance:
- Docs include city entry flow notes.
- `proto/scene.proto` satisfies empty city entry needs.
- City UI is limited to role name, level, and gold.
- `docs/11_Changelog.md` is updated.

Suggested Codex prompt:
- Use `ai/prompts/phase1/task5_city_design.md`.

## Phase 1 Completion Standard

- Task 1 through Task 5 are complete and committed.
- Server skeleton starts and `/health` returns `OK`.
- Client Unity project plan exists before real Unity files are created.
- Login, role selection, and city entry protocol and interface designs are documented.
- No combat, inventory, quest, chat, movement sync, database, or Redis logic is implemented early.
- `.env`, local provider configs, and API keys are not committed.
- `docs/11_Changelog.md` records every meaningful change.

