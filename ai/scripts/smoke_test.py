from __future__ import annotations

from datetime import datetime

from common import get_provider_runtime, load_provider_config, provider_order, require_provider_ready, resolve_output_path


SMOKE_PROMPT = "请只回复 OK"


def run_provider_test(provider_name: str, config: dict) -> dict[str, str]:
    runtime = get_provider_runtime(provider_name, config)
    result = {
        "provider": runtime.name,
        "display_name": runtime.display_name,
        "base_url": runtime.base_url,
        "model": runtime.model or "",
        "status": "SKIPPED",
        "reason": "",
        "response": "",
    }

    issues = require_provider_ready(runtime)
    if issues:
        result["reason"] = "; ".join(issues)
        return result

    try:
        from openai import OpenAI
    except ImportError as exc:
        result["status"] = "FAILED"
        result["reason"] = f"Missing dependency: {exc}. Run: pip install -r requirements-ai.txt"
        return result

    try:
        client = OpenAI(api_key=runtime.api_key, base_url=runtime.base_url)
        response = client.chat.completions.create(
            model=runtime.model,
            messages=[{"role": "user", "content": SMOKE_PROMPT}],
            temperature=0,
            max_tokens=16,
        )
        content = (response.choices[0].message.content or "").strip()
        result["response"] = content
        if content.upper() == "OK":
            result["status"] = "OK"
            result["reason"] = "OK"
        else:
            result["status"] = "FAILED"
            result["reason"] = f"Expected OK, got: {content}"
    except Exception as exc:
        result["status"] = "FAILED"
        result["reason"] = f"provider={runtime.name} | base_url={runtime.base_url} | model={runtime.model} | error={exc}"

    return result


def write_report(results: list[dict[str, str]]) -> str:
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    lines = [
        "# AI Provider Smoke Test Report",
        "",
        f"Generated: {now}",
        "",
        "| Provider | Model | Status | Reason |",
        "|---|---|---|---|",
    ]
    for item in results:
        reason = item["reason"].replace("|", "\\|")
        lines.append(f"| {item['display_name']} | {item['model'] or '-'} | {item['status']} | {reason} |")

    lines.extend(
        [
            "",
            "## Prompt",
            "",
            "```text",
            SMOKE_PROMPT,
            "```",
            "",
            "No API keys are written to this report.",
        ]
    )
    report = "\n".join(lines) + "\n"
    path = resolve_output_path("smoke_test_report.md")
    path.write_text(report, encoding="utf-8")
    return str(path)


def main() -> int:
    config = load_provider_config()
    results = []
    has_failure = False

    for provider_name in provider_order(config):
        result = run_provider_test(provider_name, config)
        results.append(result)
        if result["status"] == "OK":
            print(f"[OK] {result['display_name']}: OK")
        elif result["status"] == "SKIPPED":
            print(f"[SKIPPED] {result['display_name']}: {result['reason']}")
        else:
            has_failure = True
            print(f"[FAILED] {result['display_name']}: {result['reason']}")

    report_path = write_report(results)
    print(f"[OK] Report written: {report_path}")
    return 1 if has_failure else 0


if __name__ == "__main__":
    raise SystemExit(main())
