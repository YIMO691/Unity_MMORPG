# Module Boundary

## Client Modules

- `Client.Network`: message connection, serialization, request sending, notification dispatch.
- `Client.Entity`: local entity view model and presentation state.
- `Client.Scene`: scene loading and scene UI entry points.
- `Client.Resource`: local asset loading and remote resource update flow.
- `Client.Lua`: Lua VM bootstrap, script loading, and hotfix entry points.
- `Client.UI`: UI panels and Lua-driven UI flow.

## Server Modules

- `Server.Auth`: login and session token issuing.
- `Server.Role`: role list, role creation, and selected role state.
- `Server.Scene`: scene entry and player scene state.
- `Server.Movement`: movement intent validation and snapshots.
- `Server.Combat`: skill validation and combat events.
- `Server.Inventory`: item stack authority and item use.
- `Server.Quest`: quest state and reward progress.
- `Server.Chat`: chat intent validation and broadcast routing.

## Boundary Rules

- Client code must not calculate final rewards, combat damage, inventory mutations, or quest completion.
- Lua must not bypass C# networking, entity, or resource ownership.
- Server modules expose protocol handlers and internal services, not UI-specific logic.

