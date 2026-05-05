# Protocol Rules

Protocol drafts live in `proto/` and use Protocol Buffers syntax.

## Naming

- Client requests use `C2S_` prefix.
- Server responses use `S2C_` prefix.
- Server notifications use `S2C_` prefix and `Ntf` suffix.
- Request and response pairs use matching action names where possible.

Required initial names:

- `C2S_LoginReq`
- `S2C_LoginRes`
- `C2S_CreateRoleReq`
- `S2C_CreateRoleRes`
- `C2S_EnterSceneReq`
- `S2C_EnterSceneRes`
- `C2S_MoveReq`
- `S2C_EntitySnapshotNtf`
- `C2S_CastSkillReq`
- `S2C_CombatEventNtf`
- `C2S_UseItemReq`
- `S2C_InventoryChangedNtf`
- `C2S_AcceptQuestReq`
- `S2C_QuestChangedNtf`
- `C2S_ChatReq`
- `S2C_ChatNtf`

## Authority

- Client messages describe intent.
- Server messages describe accepted state, rejected state, or authoritative notifications.
- Do not add client messages that submit final damage, rewards, item counts, quest completion, or authoritative position.

