# Phase 1: Login / Role / City

## Goal

- Unity client can open a login UI.
- Server can provide a login interface.
- Client can receive `playerId` and `token`.
- Client can enter role selection.
- Client can enter an empty city screen.
- Server can return basic player data.

## Planned Client Work

- Create Unity project under `client/`.
- Add a minimal C# network layer placeholder.
- Add Lua bootstrap placeholder.
- Add login UI flow.
- Add role selection UI flow.
- Add city entry UI shell.

## Planned Server Work

- Create .NET server project under `server/`.
- Add auth endpoint or protocol handler.
- Add role list and create-role handler.
- Add enter-scene handler for the initial city.
- Return basic player data from server-owned state.

## Deferred From Phase 1

- Combat.
- Inventory.
- Quest.
- Chat.
- Movement sync.
- Monster AI.
- Loot.

