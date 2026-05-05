# Tech Stack

## Client

- Unity for rendering, input, UI shell, assets, Android build, and local presentation.
- C# for low-level systems, networking, entity model, resource loading, protocol binding, and high-frequency runtime code.
- Lua for UI flow, quest scripts, activities, tutorial, and lightweight hotfix logic.

## Server

- .NET for authoritative backend services.
- C# for service code, protocol handlers, movement validation, combat settlement, inventory, loot, quest progress, and rewards.

## Data And Protocol

- Protocol Buffers draft files live in `proto/`.
- Config table templates live in `configs/`.
- Runtime secrets must be loaded from local environment files and must not be committed.

## Current AI Provider State

- Qwen provider is available and smoke test has passed.
- OpenAI, Kimi, DeepSeek, and MiMo are not configured yet.
- Do not depend on unavailable providers and do not claim their tests passed.

