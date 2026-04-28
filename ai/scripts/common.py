from __future__ import annotations

import json
import os
from dataclasses import dataclass
from pathlib import Path
from typing import Any


ROOT_DIR = Path(__file__).resolve().parents[2]
AI_DIR = ROOT_DIR / "ai"
OUTPUT_DIR = AI_DIR / "outputs"
PROVIDERS_EXAMPLE_PATH = AI_DIR / "providers" / "providers.example.json"
PROVIDERS_LOCAL_PATH = AI_DIR / "providers" / "providers.local.json"
ENV_PATH = ROOT_DIR / ".env"

PROVIDER_DISPLAY_NAMES = {
    "openai": "OpenAI",
    "qwen": "Qwen",
    "kimi": "Kimi",
    "deepseek": "DeepSeek",
    "mimo": "MiMo",
}


@dataclass(frozen=True)
class ProviderRuntime:
    name: str
    display_name: str
    enabled: bool
    api_key_env: str
    api_key: str | None
    base_url: str
    model: str | None
    config: dict[str, Any]


def load_dotenv_if_available() -> None:
    try:
        from dotenv import load_dotenv
    except ImportError:
        return

    if ENV_PATH.exists():
        load_dotenv(ENV_PATH, override=False)


def deep_merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    merged = dict(base)
    for key, value in override.items():
        if isinstance(value, dict) and isinstance(merged.get(key), dict):
            merged[key] = deep_merge(merged[key], value)
        else:
            merged[key] = value
    return merged


def read_json(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as file:
        return json.load(file)


def load_provider_config() -> dict[str, Any]:
    if not PROVIDERS_EXAMPLE_PATH.exists():
        raise FileNotFoundError(f"Provider config not found: {PROVIDERS_EXAMPLE_PATH}")

    config = read_json(PROVIDERS_EXAMPLE_PATH)
    if PROVIDERS_LOCAL_PATH.exists():
        config = deep_merge(config, read_json(PROVIDERS_LOCAL_PATH))
    return config


def mask_secret(secret: str | None) -> str:
    if not secret:
        return ""
    if len(secret) <= 8:
        return "****"
    return f"{secret[:3]}****{secret[-4:]}"


def provider_order(config: dict[str, Any]) -> list[str]:
    preferred = ["openai", "qwen", "kimi", "deepseek", "mimo"]
    providers = config.get("providers", {})
    ordered = [name for name in preferred if name in providers]
    ordered.extend(name for name in providers if name not in ordered)
    return ordered


def get_provider_runtime(name: str, config: dict[str, Any] | None = None) -> ProviderRuntime:
    load_dotenv_if_available()
    config = config or load_provider_config()
    providers = config.get("providers", {})
    if name not in providers:
        known = ", ".join(provider_order(config))
        raise ValueError(f"Unknown provider '{name}'. Known providers: {known}")

    provider = providers[name]
    api_key_env = provider["api_key_env"]
    base_url_env = provider["base_url_env"]
    model_env = provider.get("default_model_env")

    api_key = os.getenv(api_key_env) or None
    base_url = os.getenv(base_url_env) or provider.get("default_base_url") or ""
    model = None
    if model_env:
        model = os.getenv(model_env) or None
    model = model or provider.get("default_model")

    return ProviderRuntime(
        name=name,
        display_name=PROVIDER_DISPLAY_NAMES.get(name, name),
        enabled=bool(provider.get("enabled", False)),
        api_key_env=api_key_env,
        api_key=api_key,
        base_url=base_url,
        model=model,
        config=provider,
    )


def default_provider_name() -> str:
    load_dotenv_if_available()
    return os.getenv("AI_DEFAULT_PROVIDER", "qwen")


def default_temperature() -> float:
    load_dotenv_if_available()
    value = os.getenv("AI_TEMPERATURE", "0.2")
    try:
        return float(value)
    except ValueError:
        return 0.2


def default_max_tokens() -> int:
    load_dotenv_if_available()
    value = os.getenv("AI_MAX_OUTPUT_TOKENS", "4096")
    try:
        return int(value)
    except ValueError:
        return 4096


def read_text_file(path_arg: str) -> str:
    path = Path(path_arg)
    if not path.is_absolute():
        path = ROOT_DIR / path
    return path.read_text(encoding="utf-8")


def resolve_output_path(output_arg: str | None, default_name: str | None = None) -> Path | None:
    if output_arg:
        raw = Path(output_arg)
        if raw.is_absolute():
            path = raw
        elif raw.parts and raw.parts[0] == "ai":
            path = ROOT_DIR / raw
        else:
            path = OUTPUT_DIR / raw
    elif default_name:
        path = OUTPUT_DIR / default_name
    else:
        return None

    path = path.resolve()
    output_root = OUTPUT_DIR.resolve()
    if output_root != path and output_root not in path.parents:
        raise ValueError(f"Output path must be under {OUTPUT_DIR}")

    path.parent.mkdir(parents=True, exist_ok=True)
    return path


def require_provider_ready(runtime: ProviderRuntime) -> list[str]:
    issues: list[str] = []
    if not runtime.enabled:
        issues.append(f"{runtime.display_name} provider is disabled in provider config.")
    if not runtime.api_key:
        issues.append(f"{runtime.display_name} API key missing. Set {runtime.api_key_env} in .env or environment variables.")
    if not runtime.model:
        model_env = runtime.config.get("default_model_env", "provider model env")
        issues.append(f"{runtime.display_name} model missing. Set {model_env} or provider default_model.")
    if not runtime.base_url:
        issues.append(f"{runtime.display_name} base_url missing. Set {runtime.config.get('base_url_env')}.")
    return issues
