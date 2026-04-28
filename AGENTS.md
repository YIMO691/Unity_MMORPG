# AGENTS.md

## Project

This is a Unity + C# + Lua + .NET MMORPG Demo project.

## Goal

Build a small but complete MMORPG vertical slice suitable for a resume:
- login
- role selection
- city
- world map
- movement sync
- combat
- monster AI
- loot
- inventory
- quest
- chat
- Lua hotfix
- remote resource update
- Android build
- backend deployment

## Architecture Rules

1. Client is responsible for rendering, UI, input, local presentation, resource loading, and network message sending.
2. Server is authoritative for movement validation, combat result, inventory, loot, quest progress, and rewards.
3. Client must only send intent, never final result.
4. C# handles low-level, high-frequency, performance-sensitive logic.
5. Lua handles UI flow, quests, activities, tutorial, and lightweight hotfix logic.
6. Do not introduce new frameworks without explicit approval.
7. Do not hardcode secrets.
8. Do not commit `.env`.
9. Keep module names consistent.
10. Update docs and changelog after meaningful changes.

## AI Usage Rules

1. Use small scoped tasks.
2. Always list files to modify before editing.
3. Always provide validation steps.
4. Never rewrite unrelated modules.
5. Never change the tech stack without approval.
6. Never store API keys in code.
7. When generating code, prefer maintainable, testable code over clever code.

## Preferred Model Routing

- Unity C#: Qwen-Coder, fallback OpenAI
- Lua: Qwen-Coder, fallback Kimi
- .NET backend: Qwen-Coder, fallback DeepSeek
- Bug analysis: DeepSeek, fallback Kimi
- Architecture review: Kimi or MiMo, fallback OpenAI
