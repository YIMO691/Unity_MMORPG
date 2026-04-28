from __future__ import annotations

from common import get_provider_runtime, load_provider_config, mask_secret, provider_order


def main() -> int:
    config = load_provider_config()
    for provider_name in provider_order(config):
        runtime = get_provider_runtime(provider_name, config)
        if not runtime.enabled:
            print(f"[DISABLED] {runtime.display_name} provider disabled in config: {runtime.api_key_env}")
            continue

        if runtime.api_key:
            model_text = runtime.model or "<missing model>"
            print(f"[OK] {runtime.display_name} key found: {mask_secret(runtime.api_key)} | model={model_text}")
        else:
            print(f"[MISSING] {runtime.display_name} key missing: {runtime.api_key_env}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
