# Changelog

## 2026-04-28

### Phase 1

- Added a Qwen context check script and verified Task 5 scope understanding before city design generation.
- Added Qwen project context and task rules to reduce scope drift before Phase 1 Task 5.
- Phase 1 Task 4: fixed role selection design by aligning `role.proto` with the shared proto namespace, using `POST /api/roles/list`, removing out-of-scope city/WebSocket steps, and repairing Markdown/changelog text.
- Phase 1 Task 4: completed role selection flow design, including `role.proto`, role interface documentation, and role selection sequence notes, without implementing role business logic.
- Phase 1 Task 3: completed login protocol and interface design, including `auth.proto`, login interface documentation, and login sequence notes, without implementing login business logic.
- Phase 1 Task 2: prepared the Unity client skeleton documentation, including client directory planning, module boundaries, and Bootstrap/Login/RoleSelect/City responsibilities.
- Phase 1 Task 1: created the server skeleton with .NET solution, Gateway/API, layered projects, test project, and `/health` endpoint.
- Prepared Phase 1 Login / Role / City execution plan and environment checklist.
- Added copy-ready Codex prompts for Phase 1 server skeleton, Unity skeleton planning, login design, role design, and city design.
- Updated roadmap to mark Phase 0 completed and Phase 1 preparing.

### Phase 0

- Re-verified Phase 0 foundation against AGENTS.md, MMORPG_AI_Agent_Workflow.md, AI task rules, model routing rules, and setup guide.
- Confirmed required directories, documentation, protocol drafts, and config templates are present for Phase 0.
- Initialized Phase 0 foundation documentation.
- Added root directory placeholders for client, server, tools, deploy, and source assets.
- Added minimal protocol drafts for auth, role, scene, movement, combat, inventory, quest, and chat.
- Added config table templates for roles, scenes, monsters, skills, items, drops, and quests.
- Documented server-authoritative architecture, client intent rules, Lua hotfix boundaries, and first-version exclusions.
