# Client Unity Skeleton

Phase 1 Task 2 prepares the Unity client structure for the first vertical slice: login, role selection, and an empty city screen.

This task does not create a real Unity project and does not implement gameplay or network calls.

## Client Goal

- Open the client.
- Enter Login flow.
- Enter RoleSelect flow after server login is implemented later.
- Enter City flow after role selection is implemented later.
- Display only role name, level, and gold in the empty city UI in Phase 1.

## C# And Lua Boundary

C# owns:
- Boot and lifecycle.
- Network client.
- Entity and resource foundations.
- UI root and view binding.
- High-frequency runtime code.

Lua owns:
- UI flow.
- Tutorial and lightweight activity flow in later phases.
- Hotfixable lightweight behavior.

Lua must not own final combat, inventory, loot, quest reward, or movement authority.

## Boot Flow Design

Planned boot flow:

```text
Unity starts
-> GameLauncher initializes core services
-> UIRoot is created
-> NetworkClient is prepared
-> LoginView is opened
```

No real scene or script is implemented in this task.

## Module Responsibilities

### Login

- Show account input or quick login entry.
- Send login intent in a later task.
- Display login errors returned by server in a later task.
- On success, switch to RoleSelect flow.

### RoleSelect

- Show role list returned by server.
- Send create-role and select-role intent in later tasks.
- On success, switch to City flow.

### City

- Show empty city shell.
- Display role name, level, and gold.
- Does not implement movement, NPC, quest, combat, or chat in Phase 1.

## Planned Class Responsibilities

| Class | Planned directory | Responsibility | Implement now | Later task |
|---|---|---|---|---|
| `GameLauncher.cs` | `Assets/Game/Boot/` | Unity entry point, initializes core services and first flow. | No | Task 2 implementation follow-up |
| `UIRoot.cs` | `Assets/Game/Framework/UI/` | Owns UI canvas root and panel layering. | No | Task 2 implementation follow-up |
| `LoginView.cs` | `Assets/Game/Feature/Login/` | Displays login UI and user input. | No | Login UI task |
| `LoginController.cs` | `Assets/Game/Feature/Login/` | Coordinates login view and network intent. | No | Login implementation task |
| `RoleSelectView.cs` | `Assets/Game/Feature/RoleSelect/` | Displays role list, create role UI, and selection UI. | No | RoleSelect implementation task |
| `RoleSelectController.cs` | `Assets/Game/Feature/RoleSelect/` | Coordinates role list, create-role, and select-role flow. | No | RoleSelect implementation task |
| `CityView.cs` | `Assets/Game/Feature/City/` | Displays empty city HUD fields: role name, level, gold. | No | City empty UI task |
| `CityController.cs` | `Assets/Game/Feature/City/` | Coordinates city entry data and view state. | No | City empty UI task |
| `NetworkClient.cs` | `Assets/Game/Framework/Network/` | Sends client intent and dispatches server responses. | No | Login interface implementation task |

## Temporary Non-Implementation Scope

- No real Unity project files.
- No scene files.
- No prefabs.
- No C# implementation files.
- No Lua implementation files.
- No login request.
- No role request.
- No city interface.
- No combat, skill, inventory, quest, chat, movement sync, resource hot update, or Lua hotfix.

