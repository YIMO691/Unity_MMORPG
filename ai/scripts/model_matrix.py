from __future__ import annotations


ROWS = [
    ("Unity C# 代码", "qwen", "openai"),
    ("Lua 热更新", "qwen", "kimi"),
    (".NET 服务端", "qwen", "deepseek"),
    ("Protobuf 协议", "qwen", "openai"),
    ("Docker/CI", "qwen", "deepseek"),
    ("Bug 分析", "deepseek", "kimi"),
    ("架构审查", "kimi/mimo", "openai"),
    ("长文档总结", "kimi/mimo", "openai"),
    ("代码审查", "kimi", "deepseek"),
    ("简历/README", "kimi", "qwen"),
]


def main() -> int:
    print("AI model routing matrix")
    print()
    for task, primary, fallback in ROWS:
        print(f"{task}: {primary} -> {fallback} fallback")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
