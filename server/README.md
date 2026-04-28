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

## Not Implemented Yet

- Login.
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

Phase 1 Task 3: login protocol and interface design.
