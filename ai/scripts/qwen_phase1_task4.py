import json
import os
import re
from pathlib import Path

from dotenv import load_dotenv
from openai import OpenAI


ROOT = Path(__file__).resolve().parents[2]

ALLOWED_FILES = {
    "proto/role.proto",
    "docs/21_Role_Select_Interface_Design.md",
    "docs/22_Role_Select_Sequence.md",
    "client/docs/ClientModuleBoundary.md",
    "server/README.md",
    "docs/11_Changelog.md",
}

CONTEXT_FILES = [
    "AGENTS.md",
    "MMORPG_AI_Agent_Workflow.md",
    "docs/16_Phase_1_Execution_Plan.md",
    "docs/05_Protocol_Rules.md",
    "docs/19_Login_Interface_Design.md",
    "docs/20_Login_Sequence.md",
    "proto/auth.proto",
    "proto/role.proto",
    "client/docs/ClientModuleBoundary.md",
    "server/README.md",
    "docs/11_Changelog.md",
]

SYSTEM_PROMPT = """你是 Qwen-Coder，现在作为本地代码生成助手为 Unity MMORPG Demo 项目生成单个文件内容。

你必须严格遵守：
1. 当前任务是 Phase 1 Task 4：选角流程设计。
2. 每次只生成用户指定的一个文件。
3. 不实现真实角色业务代码。
4. 不修改 server/src/。
5. 不修改 server/tests/。
6. 不创建真实 Unity 工程。
7. 不读取、输出或要求用户提供 .env。
8. 不输出任何真实 API Key、secret、password。
9. 客户端只上报意图，服务端保持权威。
10. 只返回严格合法 JSON，不要 Markdown 包裹，不要解释。

非常重要：
不要使用 base64。
不要使用 content 字段。
必须使用 content_lines 字段。
content_lines 是字符串数组，每个元素代表文件的一行内容。
如果文件中有空行，用空字符串 "" 表示。
如果文件中有 Markdown 代码块，也按普通文本行放入 content_lines。

输出格式必须是：
{
  "path": "用户指定的文件路径",
  "content_lines": [
    "第一行",
    "第二行",
    "",
    "第四行"
  ]
}

禁止输出 diff。
禁止省略文件内容。
"""

GLOBAL_TASK = """Phase 1 Task 4：选角流程设计。

本次目标：
- 检查并完善 proto/role.proto
- 创建选角接口设计文档
- 创建选角时序文档
- 补充客户端 RoleSelect 模块职责
- 补充服务端 README，说明选角接口尚未实现
- 更新 changelog
- 不实现角色业务代码
- 不接数据库
- 不接 Redis
- 不实现真实 Unity UI
- 不实现主城业务
- 不实现战斗、背包、任务、聊天、移动同步

角色流程：
登录成功
↓
进入选角
↓
获取角色列表
↓
如果没有角色，可创建角色
↓
选择角色
↓
为进入主城做准备

接口建议：
GET /api/roles
POST /api/roles/create
POST /api/roles/select

role.proto 至少包含：

syntax = "proto3";
package mmo.role;

message RoleInfo {
  string role_id = 1;
  string name = 2;
  int32 level = 3;
  int32 class_id = 4;
  string scene_id = 5;
  int64 gold = 6;
}

message C2S_GetRoleListReq {
  string player_id = 1;
  string token = 2;
}

message S2C_GetRoleListRes {
  int32 code = 1;
  string message = 2;
  repeated RoleInfo roles = 3;
}

message C2S_CreateRoleReq {
  string player_id = 1;
  string token = 2;
  string name = 3;
  int32 class_id = 4;
}

message S2C_CreateRoleRes {
  int32 code = 1;
  string message = 2;
  RoleInfo role = 3;
}

message C2S_SelectRoleReq {
  string player_id = 1;
  string token = 2;
  string role_id = 3;
}

message S2C_SelectRoleRes {
  int32 code = 1;
  string message = 2;
  RoleInfo role = 3;
}

Changelog 追加：
Phase 1 Task 4：完成选角流程设计，补充 role.proto、选角接口文档和选角时序说明，暂不实现角色业务。
"""

FILE_INSTRUCTIONS = {
    "proto/role.proto": """生成完整 proto/role.proto。

要求：
1. 使用 syntax = "proto3";
2. package 使用 mmo.role;
3. 必须包含 RoleInfo、C2S_GetRoleListReq、S2C_GetRoleListRes、C2S_CreateRoleReq、S2C_CreateRoleRes、C2S_SelectRoleReq、S2C_SelectRoleRes。
4. 字段命名使用 snake_case。
5. 不引入复杂鉴权。
6. 不引入背包、任务、战斗字段。
""",
    "docs/21_Role_Select_Interface_Design.md": """生成完整 docs/21_Role_Select_Interface_Design.md。

内容必须包括：
1. 选角目标
2. 当前阶段不做范围
3. 接口路径设计：GET /api/roles、POST /api/roles/create、POST /api/roles/select
4. 请求 JSON 示例
5. 响应 JSON 示例
6. Protobuf 对应关系
7. 服务端 Gateway 职责
8. 客户端 RoleSelect 模块职责
9. 错误码设计
10. 后续实现建议
11. 明确说明：当前仅完成接口和流程设计，尚未实现服务端业务代码。
""",
    "docs/22_Role_Select_Sequence.md": """生成完整 docs/22_Role_Select_Sequence.md。

内容必须包括：
1. 选角流程文字说明
2. 客户端步骤
3. 服务端步骤
4. 无角色时的创建角色流程
5. 选择角色成功后进入主城流程
6. 失败流程
7. Mermaid sequenceDiagram 时序图
8. 明确说明：当前仅设计流程，不实现真实业务。
""",
    "client/docs/ClientModuleBoundary.md": """基于现有 client/docs/ClientModuleBoundary.md 生成完整更新版。

要求：
1. 保留原有内容。
2. 补充 RoleSelect 模块最小职责。
3. RoleSelect 负责请求角色列表、显示角色列表、发起创建角色、发起选择角色、选择成功后进入 City 流程。
4. RoleSelect 不直接决定服务端状态。
5. RoleSelect 不直接写数据库。
6. RoleSelect 不处理主城业务。
7. RoleSelect 通过 NetworkClient 调用服务端。
""",
    "server/README.md": """基于现有 server/README.md 生成完整更新版。

要求：
1. 保留当前服务端空工程和 /health 说明。
2. 补充 Phase 1 Task 4 当前只设计选角接口。
3. 说明后续拟实现接口：GET /api/roles、POST /api/roles/create、POST /api/roles/select。
4. 明确说明：当前服务端实际只实现了 /health，尚未实现登录接口和选角接口。
5. 不要误写已经实现选角接口。
6. 不要加入数据库、Redis、JWT 实现内容。
""",
    "docs/11_Changelog.md": """基于现有 docs/11_Changelog.md 生成完整更新版。

要求：
1. 保留原有 changelog 内容。
2. 在末尾追加：
Phase 1 Task 4：完成选角流程设计，补充 role.proto、选角接口文档和选角时序说明，暂不实现角色业务。
3. 不要删除历史记录。
""",
}


def read_file(path: str) -> str:
    full = ROOT / path
    if not full.exists():
        return ""
    try:
        return full.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        return full.read_text(encoding="utf-8-sig")


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
        "dashscope_api_key=",
        "openai_api_key=",
        "moonshot_api_key=",
        "deepseek_api_key=",
        "xiaomi_api_key=",
        "sk-",
        "password=",
        "secret=",
    ]

    for token in forbidden:
        if token in lowered:
            raise ValueError(f"Potential secret detected in generated content for {path}: {token}")

    return decoded


def call_qwen(client: OpenAI, model: str, path: str, instruction: str, context: str) -> str:
    prompt = f"""{GLOBAL_TASK}

现在只生成这一个文件：
{path}

该文件的具体要求：
{instruction}

项目上下文：
{context}

最终输出要求：
1. 只输出严格合法 JSON。
2. 不要输出 Markdown。
3. 不要输出 ```json。
4. 不要输出解释文字。
5. path 必须等于：{path}
6. 必须使用 content_lines 字段。
7. content_lines 必须是字符串数组。
8. content_lines 每个元素代表文件的一行。
9. 空行使用空字符串 ""。
10. 禁止使用 content 字段。
11. 禁止使用 content_base64 字段。
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
    raw_path = output_dir / f"qwen_phase1_task4_{safe_name}.json"
    raw_path.write_text(raw, encoding="utf-8")
    print(f"[RAW] {raw_path}")

    payload = extract_json(raw)
    return validate_one(payload, path)


def main() -> None:
    load_dotenv(ROOT / ".env")

    api_key = os.getenv("DASHSCOPE_API_KEY")
    base_url = os.getenv("QWEN_BASE_URL", "https://dashscope-intl.aliyuncs.com/compatible-mode/v1")
    model = os.getenv("QWEN_CODER_MODEL", "qwen3-coder-plus")

    if not api_key:
        raise RuntimeError("Missing DASHSCOPE_API_KEY in .env or environment variables.")

    client = OpenAI(api_key=api_key, base_url=base_url)
    context = build_context()

    print(f"[QWEN] model={model}")
    print("[QWEN] generating Phase 1 Task 4 files one by one...")

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

    print("\n[DONE] Qwen Phase 1 Task 4 split generation completed.")
    print("Next commands:")
    print("  git status")
    print("  git diff --name-only")
    print("  Select-String -Path proto/role.proto -Pattern \"C2S_GetRoleListReq\"")
    print("  Select-String -Path proto/role.proto -Pattern \"C2S_CreateRoleReq\"")
    print("  Select-String -Path proto/role.proto -Pattern \"C2S_SelectRoleReq\"")
    print("  Test-Path docs/21_Role_Select_Interface_Design.md")
    print("  Test-Path docs/22_Role_Select_Sequence.md")


if __name__ == "__main__":
    main()