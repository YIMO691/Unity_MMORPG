# AI Development Environment

This folder contains the AI API router, prompt templates, provider configuration, and smoke tests for the MMORPG Demo project.

## Quick Start

1. Install Python dependencies:

```bash
pip install -r requirements-ai.txt
```

2. Copy `.env.example` to `.env` and fill only the API keys you have purchased or enabled.

3. Check configured providers:

```bash
python ai/scripts/check_env.py
```

4. Run smoke tests:

```bash
python ai/scripts/smoke_test.py
```

The smoke test report is written to `ai/outputs/smoke_test_report.md`.

## CLI Chat Examples

```bash
python ai/scripts/ai_chat.py --provider qwen --prompt "生成一个 Unity C# 单例模板"
python ai/scripts/ai_chat.py --provider kimi --prompt-file ai/prompts/tasks/task_template.md
python ai/scripts/ai_chat.py --provider deepseek --system ai/prompts/system/bug_analyzer.md --prompt "分析这个报错"
```

To save an answer:

```bash
python ai/scripts/ai_chat.py --provider qwen --prompt "生成登录模块任务拆解" --output ai/outputs/login_plan.md
```

## Secret Rules

- Do not commit `.env`.
- Do not put API keys in code, prompts, docs, or test reports.
- Use environment variables for all providers.
- Use `providers.local.json` only for local overrides; it is ignored by Git.
