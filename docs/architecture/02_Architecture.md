# Architecture

The project uses a server-authoritative MMORPG architecture.

## Client Responsibilities

- Rendering and animation presentation.
- UI display and local interaction.
- Input collection.
- Local prediction or interpolation where later phases require it.
- Resource loading and resource update client flow.
- Network message sending.

The client must send intent, not final results. Examples include login requests, movement input, skill cast intent, item use intent, quest accept intent, and chat send intent.

## Server Responsibilities

- Account/session validation.
- Player and role data ownership.
- Movement validation.
- Combat result calculation.
- Monster AI decisions.
- Inventory, loot, quest progress, and reward authority.
- Chat routing and filtering hooks.

## Runtime Split

- C# owns performance-sensitive and foundational runtime code.
- Lua owns UI flow and lightweight gameplay scripting.
- Resource hot update handles assets and bundles.
- Lua hotfix handles script-level behavior updates.

