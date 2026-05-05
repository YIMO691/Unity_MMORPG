# City Interface Design

This document defines the Phase 1 empty city screen interface design. It is a design document only; no server or client business implementation is included in Task 5.

## Main City Empty Screen Goals

- Let the client request empty city screen data after role selection succeeds.
- Let the server return minimal role display data: name, level, gold.
- Keep the server authoritative over role data and scene state.
- Keep the minimum display fields clear: role name, level, and gold.
- Prepare for the next phase where real scene loading and entity sync will be added.

## Current Phase Not Included Scope

- Real city business logic.
- Database or Redis persistence.
- Formal authentication or complex permission checks.
- Scene entity synchronization.
- Player movement synchronization.
- WebSocket connection establishment.
- Combat system.
- Inventory system.
- Quest system.
- Chat system.
- NPC or monster spawning.
- Real Unity scene loading.
- Resource loading or asset management.
- UI interaction beyond displaying static data.

## Interface Path Design

Use POST for the Phase 1 city entry request so the development request body stays explicit and consistent.

```text
POST /api/scene/enter-city
```

Current Phase 1 design keeps `token` in the request body for development simplicity. A later phase can move it to an `Authorization` header and let the server derive `playerId` from token/session state.

## Request JSON Example

```json
{
  "playerId": "player_demo_001",
  "token": "dev_token_demo_001",
  "roleId": "role_demo_001"
}
```

## Response JSON Example

```json
{
  "code": 0,
  "message": "OK",
  "role": {
    "roleId": "role_demo_001",
    "name": "HeroName",
    "level": 1,
    "classId": 1,
    "sceneId": 1001,
    "gold": 100
  },
  "serverTime": 1730000000000
}
```

### Error Response Example

```json
{
  "code": 1001,
  "message": "Player not authenticated",
  "role": null,
  "serverTime": 1730000000000
}
```

## Protobuf Correspondence

The protocol lives in `proto/scene.proto`:

```protobuf
syntax = "proto3";

package mmorpg;

option csharp_namespace = "AIMMO.Proto";

message CityRoleInfo {
  string role_id = 1;
  string name = 2;
  int32 level = 3;
  int32 class_id = 4;
  int32 scene_id = 5;
  int64 gold = 6;
}

message C2S_EnterCityReq {
  string player_id = 1;
  string token = 2;
  string role_id = 3;
}

message S2C_EnterCityRes {
  int32 code = 1;
  string message = 2;
  CityRoleInfo role = 3;
  int64 server_time = 4;
}
```

## Server Gateway Responsibilities

- Validate the development token at a basic design level.
- Return the selected role's basic information for display.
- Accept enter-city intent and return server-owned role data.
- Reject invalid player, token, or role ownership.
- Maintain authority over role state and scene assignment.
- Provide minimal data needed for empty city screen display.

## Client City Module Responsibilities

- Request empty city data after role selection succeeds.
- Display role name, level, and gold from server response.
- Prepare for future scene loading and entity sync.
- Store role information for UI display.
- Handle enter-city errors and retries.
- Transition to real scene loading in later phases.

## Error Code Draft

| Code | Message | Description |
|---|---|---|
| 0 | OK | Success |
| 1001 | Player not authenticated | Missing or invalid development token |
| 1002 | Role not found | Requested role does not exist or belong to player |
| 1003 | Scene access denied | Player cannot enter requested scene |
| 1004 | Server error | Internal server error |
| 1005 | Invalid request | Missing required fields in request |

## Implementation Note

When implementation starts, use in-memory fake data first to complete the `login -> role selection -> empty city` loop. Database, Redis, and formal authentication belong to later phases.

## Current Status

This document completes the interface and flow design for the empty city screen. The server business code and Unity UI implementation are not implemented yet. The current Phase 1 Task 5 only designs the interface and sequence flow.

The empty city screen currently only displays role name, level, and gold. All other features like movement, combat, NPCs, monsters, inventory, quest, and chat are out of scope for this phase.
