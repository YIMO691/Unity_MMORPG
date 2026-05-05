# Lua Hotfix Rules

Lua is used for UI flow, quests, activities, tutorial, and lightweight hotfix logic.

## Boundaries

- Lua can drive UI panel flow and lightweight feature switches.
- Lua can read client-side config needed for presentation.
- Lua must not decide final combat damage, inventory changes, loot rewards, or quest rewards.
- Lua must call C# network APIs to send intent messages.
- Lua must not store API keys, tokens, or deployment secrets.

## Hotfix Separation

- Lua hotfix updates script behavior.
- Resource hot update updates assets and packaged resources.
- The two pipelines should be versioned separately so a script-only fix does not require an asset update.

