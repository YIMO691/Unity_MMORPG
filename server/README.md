# MmoDemo Server

This directory contains the Phase 1 Task 1 server skeleton for the MMORPG Demo.

## Current Stage

The server is a minimal .NET Gateway/API foundation. It only exposes a health endpoint so later Phase 1 work can add login, role selection, and city entry in small scoped tasks.

## Project Structure

```text
server/
  MmoDemo.sln
  src/
    MmoDemo.Gateway/        ASP.NET Core Minimal API entry point
    MmoDemo.Application/    application service layer placeholder
    MmoDemo.Domain/         domain model layer placeholder
    MmoDemo.Infrastructure/ infrastructure adapter layer placeholder
    MmoDemo.Contracts/      shared server contracts placeholder
  tests/
    MmoDemo.Gateway.Tests/  Gateway endpoint tests
```

## Build

From the repository root:

```powershell
dotnet build server/MmoDemo.sln
```

## Run Gateway

From the repository root:

```powershell
dotnet run --project server/src/MmoDemo.Gateway/MmoDemo.Gateway.csproj
```

The development launch profile listens on:

```text
http://localhost:5000
```

## Health Check

Open a browser or use PowerShell:

```powershell
Invoke-RestMethod http://localhost:5000/health
```

Expected JSON:

```json
{
  "status": "OK",
  "service": "MmoDemo.Gateway",
  "phase": "Phase 1 Task 1"
}
```

## Planned Login Interface

The guest login endpoint is designed for a later implementation task. It is not implemented in the current Gateway skeleton.

```
POST /api/auth/guest-login
```

Request body example:

```json
{
  "deviceId": "dev-local-001",
  "platform": "editor",
  "appVersion": "0.1.0"
}
```

Response example:

```json
{
  "code": 0,
  "message": "OK",
  "playerId": "player_demo_001",
  "token": "dev_token_demo_001",
  "serverTime": 1730000000000
}
```

**Note**: The current server only implements `/health`. The guest login interface is design-only in Phase 1 Task 3 and has no server business implementation yet.

## Planned Role Selection Interface

The role selection endpoints are designed for Phase 1 Task 4. They are not implemented in the current Gateway skeleton.

```text
POST /api/roles/list
POST /api/roles/create
POST /api/roles/select
```

**Note**: The current server only implements `/health`. Login and role selection interfaces are design-only and have no server business implementation yet.

## Not Implemented Yet

- Login business logic.
- Role selection.
- City entry.
- Database.
- Redis.
- WebSocket.
- Combat.
- Inventory.
- Quest.
- Chat.
- Movement sync.

## Next Step

Phase 1 Task 5: empty city screen design.
