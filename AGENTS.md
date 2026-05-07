# AGENTS.md

## Project

Unity + C# + Lua + .NET 9.0 MMORPG Demo (resume portfolio vertical slice). Phase 1-7 complete.

## Current Status

- **Server**: .NET 9.0 ASP.NET Core with HTTP + WebSocket, in-memory stores, MessageRouter, 10+ services (Auth, Combat, Quest, Chat, Lua hotfix, Resource update).
- **Client**: Unity project with login → role select → city flow, WebSocket gameplay, chat/quest UI, Lua VM (MoonSharp), resource update, multi-scene with portals.
- **Proto**: Design drafts in `proto/` — no codegen yet.
- **No**: Database, Redis, CI/CD, Docker deployment.

## Commands

### Server

```powershell
dotnet build server/MmoDemo.sln
dotnet run --project server/src/MmoDemo.Gateway/MmoDemo.Gateway.csproj   # http://localhost:5000
dotnet test server/MmoDemo.sln                                            # xUnit + WebApplicationFactory<T>
```

## Task Rules

- Keep each task scoped to the requested feature, module, or document.
- Before changing architecture, protocol format, framework choices, database, Redis, WebSocket, Docker, CI/CD, or deployment shape, write a short design note and wait for approval.
- For server code changes, run `dotnet test server/MmoDemo.sln` when feasible. If tests are not run, explain why.
- For documentation-only changes, no build or test run is required unless the change affects commands, generated code, or executable examples.
- Do not update the "Current Status" section unless the actual repository state has changed.

## Architecture

### Server Layer Dependency (top-down)

```text
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

- C# private fields: `_camelCase`, types/methods: PascalCase.
- Lua files: `lowercase_snake_case` (for example, `login_flow.lua`).
- Server namespace: `MmoDemo.*`; client namespace (planned): `MmoDemo.Client.*`.
- Proto files use `package mmorpg` with `option csharp_namespace = "AIMMO.Proto";`.
- Resource prefab naming: `UI_<Feature>_<Name>`, `Char_<Name>`, `Monster_<Name>`, `Map_<SceneName>`.

## Constraints

- Do not introduce new frameworks without explicit approval. Do not change the tech stack.
- Never hardcode secrets. Never store API keys in code.
- `.env` is gitignored (use `.env.example` as template). `*.local.json` is gitignored.
- Do not rewrite unrelated modules. Keep module names consistent with `docs/` and `proto/`.
- Update `docs/11_Changelog.md` for each completed phase/task.
- Do not commit generated folders or build outputs: .NET `bin/obj`, Unity `Library/`, `Temp/`, `Obj/`, `Logs/`, `Build/`, `Builds/`, or exported packages.
