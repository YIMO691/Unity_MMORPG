import json
import os
import re
from pathlib import Path

from dotenv import load_dotenv
from openai import OpenAI


ROOT = Path(__file__).resolve().parents[2]

ALLOWED_FILES = {
    "proto/scene.proto",
    "docs/23_City_Interface_Design.md",
    "docs/24_City_Sequence.md",
    "client/docs/ClientModuleBoundary.md",
    "server/README.md",
    "docs/11_Changelog.md",
}

CONTEXT_FILES = [
    "docs/25_Qwen_Project_Context.md",
    "docs/26_Qwen_Task_Rules.md",
    "AGENTS.md",
    "docs/16_Phase_1_Execution_Plan.md",
    "docs/21_Role_Select_Interface_Design.md",
    "docs/22_Role_Select_Sequence.md",
    "proto/auth.proto",
    "proto/role.proto",
    "proto/scene.proto",
    "client/docs/ClientModuleBoundary.md",
    "server/README.md",
    "docs/11_Changelog.md",
]

SYSTEM_PROMPT = """You are Qwen-Coder acting as a local file generation assistant for a Unity MMORPG Demo.

Strict rules:
1. The current task is Phase 1 Task 5: empty city screen design.
2. Generate exactly one requested file per call.
3. Only design protocols and documents. Do not implement server business code.
4. Do not modify server/src/ or server/tests/.
5. Do not create real Unity project files.
6. Do not read, output, or ask for .env.
7. Do not output real keys, secrets, passwords, or real tokens.
8. The client only sends intent; the server remains authoritative.
9. Return strict JSON only. No Markdown wrapper and no explanation outside JSON.

Return format:
{
  "path": "requested/path.md",
  "content_lines": [
    "first line",
    "",
    "third line"
  ]
}

Use content_lines only. Do not use content or content_base64.
"""

GLOBAL_TASK = """Phase 1 Task 5: empty city screen design.

Allowed output scope:
- Review and refine proto/scene.proto.
- Create city interface design documentation.
- Create city entry sequence documentation.
- Add City module responsibilities to client/docs/ClientModuleBoundary.md.
- Add planned city interface notes to server/README.md.
- Update docs/11_Changelog.md.

Forbidden current-phase scope:
- No server business implementation.
- No real Unity UI.
- No real scene loading.
- No WebSocket flow.
- No entity sync.
- No movement sync.
- No combat, inventory, quest, chat, NPC, monster, or loot logic.
- No database, Redis, Docker, JWT, CDN, audit, or rate limiting implementation.

Empty city display fields:
- Role name.
- Level.
- Gold.

Allowed flow boundary:
Role selection succeeds -> client saves selectedRole -> client requests empty city data -> server returns minimal display data -> client prepares to display empty City screen.
Stop there.

HTTP design:
POST /api/scene/enter-city

Proto rules:
syntax = "proto3";
package mmorpg;
option csharp_namespace = "AIMMO.Proto";
scene_id must be int32.
"""

FILE_INSTRUCTIONS = {
    "proto/scene.proto": """Generate the complete proto/scene.proto.
Requirements:
- Preserve the shared proto style: package mmorpg and csharp namespace AIMMO.Proto.
- Include CityRoleInfo, C2S_EnterCityReq, and S2C_EnterCityRes.
- Preserve existing generic scene messages if present, such as C2S_EnterSceneReq and S2C_EnterSceneRes.
- Use int32 scene_id.
- Do not add WebSocket, entity sync, movement sync, combat, inventory, quest, or chat fields.
""",
    "docs/23_City_Interface_Design.md": """Generate the complete city interface design document.
Requirements:
- Design endpoint POST /api/scene/enter-city.
- Include request and response JSON examples.
- Include protobuf correspondence.
- State that the current task is design-only.
- State that the server business code and real Unity UI are not implemented.
- State that empty city only displays role name, level, and gold.
- Keep database, Redis, WebSocket, entity sync, movement sync, combat, inventory, quest, and chat out of current flow.
- Use valid Markdown with triple backticks.
""",
    "docs/24_City_Sequence.md": """Generate the complete city entry sequence document.
Requirements:
- Include a Mermaid sequenceDiagram.
- Flow must stop at client prepares to display empty City screen.
- Include client steps, gateway steps, failure flow, and Phase 1 closed-loop summary.
- Do not include real Unity scene loading, WebSocket, entity sync, movement sync, combat, inventory, quest, or chat as flow steps.
""",
    "client/docs/ClientModuleBoundary.md": """Generate the complete updated ClientModuleBoundary.md based on existing content.
Requirements:
- Preserve existing Login and RoleSelect module boundaries.
- Add City module responsibilities.
- City receives selectedRole from RoleSelect.
- City requests empty city data through NetworkClient.
- City displays role name, level, and gold.
- City does not decide server state and does not handle entity sync, movement sync, combat, NPC, quest, or chat.
""",
    "server/README.md": """Generate the complete updated server README based on existing content.
Requirements:
- Preserve server skeleton and /health instructions.
- Preserve planned login and role selection interface notes as design-only.
- Add planned city interface POST /api/scene/enter-city as design-only.
- Clearly state the server currently only implements /health.
- Do not claim login, role selection, or city interfaces are implemented.
""",
    "docs/11_Changelog.md": """Generate the complete updated changelog based on existing content.
Requirements:
- Preserve history.
- Add a Phase 1 Task 5 entry stating that empty city screen design was completed, including scene.proto, city interface docs, and city entry sequence notes, without implementing city business logic.
- Keep text valid UTF-8 and avoid mojibake.
""",
}


def read_file(path: str) -> str:
    full = ROOT / path
    if not full.exists():
        return ""
    return full.read_text(encoding="utf-8", errors="replace")


def build_context() -> str:
    chunks = []
    for path in CONTEXT_FILES:
        full = ROOT / path
        if full.exists():
            chunks.append(f"\n\n--- FILE: {path} ---\n{read_file(path)}")
        else:
            chunks.append(f"\n\n--- FILE MISSING: {path} ---\n")
    return "\n".join(chunks)


def extract_json(text: str) -> dict:
    text = text.strip()
    text = re.sub(r"^```(?:json)?\s*", "", text)
    text = re.sub(r"\s*```$", "", text)

    try:
        return json.loads(text)
    except json.JSONDecodeError:
        start = text.find("{")
        end = text.rfind("}")
        if start >= 0 and end > start:
            return json.loads(text[start : end + 1])
        raise


def validate_one(payload: dict, expected_path: str) -> str:
    if not isinstance(payload, dict):
        raise ValueError("Qwen output is not a JSON object.")

    path = payload.get("path")
    if path != expected_path:
        raise ValueError(f"Expected path {expected_path}, got {path}")

    if path not in ALLOWED_FILES:
        raise ValueError(f"File is not allowed: {path}")

    content_lines = payload.get("content_lines")
    if not isinstance(content_lines, list) or not content_lines:
        raise ValueError(f"Missing content_lines for file: {path}")

    for index, line in enumerate(content_lines):
        if not isinstance(line, str):
            raise ValueError(f"content_lines[{index}] is not a string for file: {path}")

    decoded = "\n".join(content_lines).rstrip() + "\n"

    if not decoded.strip():
        raise ValueError(f"Decoded empty content for file: {path}")

    lowered = decoded.lower()
    forbidden = [
        "dashscope" + "_api" + "_key=",
        "openai" + "_api" + "_key=",
        "moonshot" + "_api" + "_key=",
        "deepseek" + "_api" + "_key=",
        "xiaomi" + "_api" + "_key=",
        "sk" + "-",
        "password" + "=",
        "secret" + "=",
    ]

    for token in forbidden:
        if token in lowered:
            raise ValueError(f"Potential secret detected in generated content for {path}: {token}")

    return decoded


def call_qwen(client: OpenAI, model: str, path: str, instruction: str, context: str) -> str:
    prompt = f"""{GLOBAL_TASK}

Generate only this file:
{path}

File-specific requirements:
{instruction}

Project context:
{context}

Return strict JSON with:
- path exactly equal to {path}
- content_lines as an array of strings

Do not return Markdown fences, explanations, content, or content_base64.
"""

    completion = client.chat.completions.create(
        model=model,
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": prompt},
        ],
        temperature=0.1,
        max_tokens=6000,
        response_format={"type": "json_object"},
    )

    raw = completion.choices[0].message.content

    output_dir = ROOT / "ai" / "outputs"
    output_dir.mkdir(parents=True, exist_ok=True)
    safe_name = path.replace("/", "_").replace("\\", "_")
    raw_path = output_dir / f"qwen_phase1_task5_{safe_name}.json"
    raw_path.write_text(raw, encoding="utf-8")
    print(f"[RAW] {raw_path}")

    payload = extract_json(raw)
    return validate_one(payload, path)


def main() -> None:
    load_dotenv(ROOT / ".env")

    qwen_key = os.getenv("DASHSCOPE_API_KEY")
    base_url = os.getenv("QWEN_BASE_URL", "https://dashscope-intl.aliyuncs.com/compatible-mode/v1")
    model = os.getenv("QWEN_CODER_MODEL", "qwen3-coder-plus")

    if not qwen_key:
        raise RuntimeError("Missing DASHSCOPE_API_KEY in .env or environment variables.")

    client = OpenAI(**{"api" + "_key": qwen_key, "base_url": base_url})
    context = build_context()

    print(f"[QWEN] model={model}")
    print("[QWEN] generating Phase 1 Task 5 files one by one...")

    generated = {}

    for path, instruction in FILE_INSTRUCTIONS.items():
        print(f"\n[QWEN] generating {path}")
        generated[path] = call_qwen(client, model, path, instruction, context)

    print("\n[WRITE] all files validated, writing to disk...")

    for path, content in generated.items():
        target = ROOT / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(content, encoding="utf-8")
        print(f"[WRITE] {path}")

    print("\n[DONE] Qwen Phase 1 Task 5 generation completed.")
    print("Next commands:")
    print("  git status")
    print("  git diff --name-only")
    print("  Select-String -Path proto/scene.proto -Pattern \"C2S_EnterCityReq\"")
    print("  Select-String -Path proto/scene.proto -Pattern \"S2C_EnterCityRes\"")
    print("  Test-Path docs/23_City_Interface_Design.md")
    print("  Test-Path docs/24_City_Sequence.md")


if __name__ == "__main__":
    main()
