from __future__ import annotations

import argparse
import sys
from datetime import datetime

from common import (
    default_max_tokens,
    default_provider_name,
    default_temperature,
    get_provider_runtime,
    read_text_file,
    require_provider_ready,
    resolve_output_path,
)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Call an OpenAI-compatible chat model through a configured provider.")
    parser.add_argument("--provider", default=default_provider_name(), help="Provider name, for example qwen, kimi, deepseek, openai.")
    parser.add_argument("--prompt", help="Prompt text.")
    parser.add_argument("--prompt-file", help="Path to a prompt file.")
    parser.add_argument("--system", help="Path to a system prompt file.")
    parser.add_argument("--output", help="Optional output file under ai/outputs/. If only a file name is given, it is saved there.")
    parser.add_argument("--temperature", type=float, default=default_temperature(), help="Sampling temperature.")
    parser.add_argument("--max-tokens", type=int, default=default_max_tokens(), help="Maximum output tokens.")
    return parser


def load_prompt(args: argparse.Namespace) -> str:
    parts: list[str] = []
    if args.prompt:
        parts.append(args.prompt)
    if args.prompt_file:
        parts.append(read_text_file(args.prompt_file))
    if not parts:
        raise ValueError("Provide --prompt, --prompt-file, or both.")
    return "\n\n".join(parts)


def call_model(provider: str, system_prompt: str | None, user_prompt: str, temperature: float, max_tokens: int) -> str:
    runtime = get_provider_runtime(provider)
    issues = require_provider_ready(runtime)
    if issues:
        for issue in issues:
            print(f"[ERROR] {issue}", file=sys.stderr)
        return ""

    try:
        from openai import OpenAI
    except ImportError:
        print("[ERROR] Missing dependency: openai. Run: pip install -r requirements-ai.txt", file=sys.stderr)
        return ""

    messages = []
    if system_prompt:
        messages.append({"role": "system", "content": system_prompt})
    messages.append({"role": "user", "content": user_prompt})

    try:
        client = OpenAI(api_key=runtime.api_key, base_url=runtime.base_url)
        response = client.chat.completions.create(
            model=runtime.model,
            messages=messages,
            temperature=temperature,
            max_tokens=max_tokens,
        )
    except Exception as exc:
        print(
            f"[ERROR] Request failed | provider={runtime.name} | base_url={runtime.base_url} | model={runtime.model} | error={exc}",
            file=sys.stderr,
        )
        return ""

    return response.choices[0].message.content or ""


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    try:
        user_prompt = load_prompt(args)
        system_prompt = read_text_file(args.system) if args.system else None
    except Exception as exc:
        print(f"[ERROR] {exc}", file=sys.stderr)
        return 2

    content = call_model(args.provider, system_prompt, user_prompt, args.temperature, args.max_tokens)
    if not content:
        return 1

    print(content)

    if args.output:
        try:
            output_path = resolve_output_path(args.output)
            output_path.write_text(content, encoding="utf-8")
            print(f"\n[OK] Saved output: {output_path}")
        except Exception as exc:
            print(f"[ERROR] Failed to save output: {exc}", file=sys.stderr)
            return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
