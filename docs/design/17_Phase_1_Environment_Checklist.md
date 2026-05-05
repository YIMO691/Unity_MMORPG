# Phase 1 Environment Checklist

Use this checklist before implementing Phase 1 Task 1.

## Required Software

- Unity Hub.
- Unity Editor.
- Rider.
- .NET SDK.
- Docker Desktop.
- Git.

## Install Later

- Android Studio / Android SDK.
- Protobuf Compiler.
- Node.js.

## PowerShell Checks

Run from any PowerShell window:

```powershell
git --version
dotnet --version
docker --version
```

Optional checks when tools are on `PATH`:

```powershell
where git
where dotnet
where docker
```

## GUI Checks

Unity:
- Open Unity Hub.
- Confirm at least one Unity Editor version is installed.
- Confirm the installed Editor includes the modules needed for the current platform. Android modules can be installed later before Android build work.

Rider:
- Open Rider.
- Confirm it can create or open a C#/.NET solution.
- Confirm the IDE detects the installed .NET SDK.

Docker Desktop:
- Open Docker Desktop.
- Confirm the engine is running before using Docker in later phases.

## Expected Before Task 1

- `git --version` returns a version.
- `dotnet --version` returns a version.
- `docker --version` returns a version, or Docker Desktop installation is planned before database/deployment work.
- Unity can be installed after the server skeleton if Task 1 is done first.

