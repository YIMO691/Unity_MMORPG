# Project Brief

AI MMORPG Demo is a small Unity, C#, Lua, and .NET MMORPG vertical slice for a resume portfolio.

The target slice covers login, role selection, city entry, world map preparation, movement sync, combat, monster AI, loot, inventory, quest, chat, Lua hotfix, remote resource update, Android build, and backend deployment over multiple phases.

Phase 0 only creates foundation assets: stable directories, protocol drafts, config templates, and documentation. It does not implement business systems.

## Scope Rules

- Client sends player intent only.
- Server remains authoritative for movement validation, combat results, inventory, loot, quest progress, and rewards.
- C# handles low-level, high-frequency, network, entity, and resource logic.
- Lua handles UI flow, quests, activities, tutorial, and lightweight hotfix logic.
- Resource hot update and Lua hotfix are separate pipelines.

## First Version Exclusions

- Guild system.
- Auction house.
- Seamless open world.
- Massive same-screen player concurrency.
- Full production operations platform.

