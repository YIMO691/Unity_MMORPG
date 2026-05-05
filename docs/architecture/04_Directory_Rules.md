# Directory Rules

## Root Directories

- `client/`: Unity client project location.
- `server/`: .NET server project location.
- `proto/`: Protocol Buffers message drafts.
- `configs/`: config table templates and later exported data.
- `tools/`: local development scripts and generation tools.
- `deploy/`: deployment manifests and backend deployment notes.
- `docs/`: project documentation and changelog.
- `assets_source/`: source art/audio/design assets before Unity import or packaging.
- `ai/`: AI routing prompts, scripts, and setup documents.

## Rules

- Keep module names consistent across docs, proto, client, and server.
- Do not commit `.env`, `.env.local`, local provider configs, or generated secrets.
- Do not commit build outputs, cache folders, or generated binary artifacts unless explicitly documented.
- Empty root directories use `.gitkeep` until real project files are created.

