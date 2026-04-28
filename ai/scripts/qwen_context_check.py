import os
from pathlib import Path

from dotenv import load_dotenv
from openai import OpenAI


ROOT = Path(__file__).resolve().parents[2]

CONTEXT_FILES = [
    "docs/25_Qwen_Project_Context.md",
    "docs/26_Qwen_Task_Rules.md",
    "docs/16_Phase_1_Execution_Plan.md",
    "docs/21_Role_Select_Interface_Design.md",
    "docs/22_Role_Select_Sequence.md",
    "proto/auth.proto",
    "proto/role.proto",
    "proto/scene.proto",
]


def read_file(path: str) -> str:
    full = ROOT / path
    if not full.exists():
        return f"\n--- FILE MISSING: {path} ---\n"
    return f"\n--- FILE: {path} ---\n" + full.read_text(encoding="utf-8", errors="replace")


def main() -> None:
    load_dotenv(ROOT / ".env")

    api_key = os.getenv("DASHSCOPE_API_KEY")
    base_url = os.getenv("QWEN_BASE_URL", "https://dashscope-intl.aliyuncs.com/compatible-mode/v1")
    model = os.getenv("QWEN_CODER_MODEL", "qwen3-coder-plus")

    if not api_key:
        raise RuntimeError("Missing DASHSCOPE_API_KEY.")

    client = OpenAI(api_key=api_key, base_url=base_url)

    context = "\n".join(read_file(path) for path in CONTEXT_FILES)

    prompt = f"""
You are doing a project understanding check only.
Do not generate files.
Do not modify code.
Do not claim that any design-only interface is implemented.

Read the project context below, then answer these questions:

1. Which phase is the project currently preparing to enter?
2. What is Phase 1 Task 5 allowed to do?
3. What is Phase 1 Task 5 forbidden to do?
4. What package and csharp_namespace must current proto files use?
5. Which files may Task 5 modify?
6. Which files or directories must Task 5 not modify?
7. Which fields may the empty city screen display?
8. Can Task 5 write WebSocket, entity sync, or movement sync? Why?
9. If generating docs/24_City_Sequence.md, where exactly should the flow stop? Include the allowed city data request and client display-preparation step, but exclude real Unity scene loading, WebSocket, entity sync, and movement sync.
10. List no more than 10 rules you will follow to avoid scope drift.

Project context:
{context}
"""

    completion = client.chat.completions.create(
        model=model,
        messages=[
            {
                "role": "system",
                "content": (
                    "You are a review assistant for a Unity MMORPG Demo. "
                    "Only check understanding. Do not generate code or files."
                ),
            },
            {"role": "user", "content": prompt},
        ],
        temperature=0.1,
        max_tokens=3000,
    )

    output = completion.choices[0].message.content
    out_path = ROOT / "ai" / "outputs" / "qwen_context_check.md"
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(output, encoding="utf-8")

    print(output)
    print(f"\n[OK] Written to {out_path}")


if __name__ == "__main__":
    main()
