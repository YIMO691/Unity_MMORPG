# Role Selection Interface Design

This document defines the Phase 1 role selection interface design. It is a design document only; no server or client business implementation is included in Task 4.

## Goals

- Let the client request the player's role list after login.
- Let the client send create-role intent when no role exists.
- Let the client send select-role intent before entering the City flow.
- Keep the server authoritative over role data and selected role state.
- Keep the minimum role fields clear: `roleId`, `name`, `level`, `classId`, `sceneId`, and `gold`.

## Out Of Scope

- Real role business logic.
- Database or Redis persistence.
- Formal authentication or complex permission checks.
- Character appearance customization.
- Character deletion or transfer.
- Combat stats, inventory, equipment, quest, or scene loading.

## Interface Path Design

Use POST for all Phase 1 role selection requests so the development request body stays explicit and consistent.

```text
POST /api/roles/list
POST /api/roles/create
POST /api/roles/select
```

Current Phase 1 design keeps `token` in the request body for development simplicity. A later phase can move it to an `Authorization` header and let the server derive `playerId` from token/session state.

## Request JSON Examples

### Role List Request

```json
{
  "playerId": "player_demo_001",
  "token": "dev_token_demo_001"
}
```

### Create Role Request

```json
{
  "playerId": "player_demo_001",
  "token": "dev_token_demo_001",
  "name": "HeroName",
  "classId": 1
}
```

### Select Role Request

```json
{
  "playerId": "player_demo_001",
  "token": "dev_token_demo_001",
  "roleId": "role_demo_001"
}
```

## Response JSON Examples

### Role List Response

```json
{
  "code": 0,
  "message": "OK",
  "roles": [
    {
      "roleId": "role_demo_001",
      "name": "HeroName",
      "level": 1,
      "classId": 1,
      "sceneId": 1001,
      "gold": 100
    }
  ]
}
```

### Create Role Response

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
  }
}
```

### Select Role Response

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
  }
}
```

### Error Response Example

```json
{
  "code": 1001,
  "message": "Player not authenticated"
}
```

## Protobuf Correspondence

The protocol lives in `proto/role.proto`:

```protobuf
syntax = "proto3";

package mmorpg;

option csharp_namespace = "AIMMO.Proto";

message RoleInfo {
  string role_id = 1;
  string name = 2;
  int32 level = 3;
  int32 class_id = 4;
  int32 scene_id = 5;
  int64 gold = 6;
}

message C2S_GetRoleListReq {
  string player_id = 1;
  string token = 2;
}

message S2C_GetRoleListRes {
  int32 code = 1;
  string message = 2;
  repeated RoleInfo roles = 3;
}

message C2S_CreateRoleReq {
  string player_id = 1;
  string token = 2;
  string name = 3;
  int32 class_id = 4;
}

message S2C_CreateRoleRes {
  int32 code = 1;
  string message = 2;
  RoleInfo role = 3;
}

message C2S_SelectRoleReq {
  string player_id = 1;
  string token = 2;
  string role_id = 3;
}

message S2C_SelectRoleRes {
  int32 code = 1;
  string message = 2;
  RoleInfo role = 3;
}
```

## Server Gateway Responsibilities

- Validate the development token at a basic design level.
- Return the player's role list.
- Accept create-role intent and return server-owned role data.
- Accept select-role intent and return selected role data.
- Reject invalid player, token, class, role name, or role ownership.

## Client RoleSelect Responsibilities

- Request role list after login.
- Display empty and non-empty role list states.
- Collect role name and class selection for create-role intent.
- Send selected role intent.
- Store the selected role response for the next City flow.

## Error Code Draft

| Code | Message | Description |
|---|---|---|
| 0 | OK | Success |
| 1001 | Player not authenticated | Missing or invalid development token |
| 1002 | Role name invalid | Name is empty or fails basic validation |
| 1003 | Role limit reached | Player cannot create more roles |
| 1004 | Class ID invalid | Requested class does not exist |
| 1005 | Role not found | Role does not belong to player |
| 1006 | Server error | Internal server error |

## Implementation Note

When implementation starts, use in-memory fake data first to complete the `login -> role selection -> empty city` loop. Database, Redis, and formal authentication belong to later phases.
