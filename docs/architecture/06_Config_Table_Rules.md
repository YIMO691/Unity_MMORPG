# Config Table Rules

Config templates live in `configs/` as CSV files.

## Rules

- Each table starts with a stable `id` column.
- Names are human-readable and stable enough for debug output.
- Numeric gameplay values must be server-readable.
- Client may read presentation fields, but server remains authoritative for rewards, drops, combat, and quest progress.
- Do not store secrets, tokens, API keys, passwords, or deployment credentials in config tables.
- Keep template data small: fields plus one or two example rows.

## Initial Templates

- `Role.template.csv`
- `Scene.template.csv`
- `Monster.template.csv`
- `Skill.template.csv`
- `Item.template.csv`
- `Drop.template.csv`
- `Quest.template.csv`

