# AGENTS.md

## Project

Unity + C# + Lua + .NET 9.0 MMORPG Demo. Phase 1 (Login / Role / City) in progress.

## Current Status

- **Server**: .NET 9.0 ASP.NET Core Minimal API, only `GET /health` works.
- **Client**: Empty. `client/` contains docs only—no Unity project created yet.
- **Proto**: Design drafts in `proto/`—no codegen.
- **No**: Database, Redis, WebSocket, CI/CD, Docker.

## Commands

### Server
```
dotnet build server/MmoDemo.sln
dotnet run --project server/src/MmoDemo.Gateway/MmoDemo.Gateway.csproj   # http://localhost:5000
dotnet test server/MmoDemo.sln                                            # xUnit + WebApplicationFactory<T>
```

### AI Scripts (requires `.venv\`, `.env` with API keys)
```
.\.venv\Scripts\python.exe ai\scripts\check_env.py
.\.venv\Scripts\python.exe ai\scripts\smoke_test.py
.\.venv\Scripts\python.exe ai\scripts\ai_chat.py --provider qwen --prompt "..."
```

## Architecture

### Server Layer Dependency (top-down)
```
Gateway -> Application -> Domain
Gateway -> Contracts
Application -> Contracts
Infrastructure -> Application + Domain
```

### Client-Server Authority
1. Client is responsible for rendering, UI, input, local presentation, resource loading, and sending network messages.
2. Server is authoritative for movement validation, combat results, inventory, loot, quest progress, and rewards.
3. Client must only send **intent**, never final result.
4. C# handles low-level, high-frequency, performance-sensitive logic.
5. Lua handles UI flow, quests, activities, tutorial, and lightweight hotfix.

### Proto Naming
- Client requests: `C2S_*Req`
- Server responses: `S2C_*Res`
- Server pushes: `S2C_*Ntf`

## Code Conventions

- C# private fields: `_camelCase`, types/methods: PascalCase
- Lua files: `lowercase_snake_case` (e.g., `login_flow.lua`)
- Server namespace: `MmoDemo.*`, client namespace (planned): `MmoDemo.Client.*`
- Proto files use `package mmorpg` with `option csharp_namespace = "AIMMO.Proto";`
- Resource prefab naming: `UI_<Feature>_<Name>`, `Char_<Name>`, `Monster_<Name>`, `Map_<SceneName>`

## Constraints

- Do not introduce new frameworks without explicit approval. Do not change the tech stack.
- Never hardcode secrets. Never store API keys in code.
- `.env` is gitignored (use `.env.example` as template). `*.local.json` is gitignored.
- Do not rewrite unrelated modules. Keep module names consistent with `docs/` and `proto/`.
- Update `docs/11_Changelog.md` for each completed phase/task.
- Do not commit Unity generated folders: `Library/`, `Temp/`, `Obj/`, `Logs/`, build outputs.
