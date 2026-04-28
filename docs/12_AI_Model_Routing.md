# AI 模型路由规则

## 默认模型

默认使用 Qwen-Coder 处理代码生成。

## 任务到模型映射

| 任务类型 | 首选模型 | 备用模型 |
|---|---|---|
| Unity C# | qwen | openai |
| Lua 热更新 | qwen | kimi |
| .NET 服务端 | qwen | deepseek |
| Protobuf | qwen | openai |
| Docker/CI | qwen | deepseek |
| Bug 分析 | deepseek | kimi |
| 架构审查 | kimi 或 mimo | openai |
| 长文档总结 | kimi 或 mimo | openai |
| 代码审查 | kimi | deepseek |
| 简历/README | kimi | qwen |

## 使用原则

1. 小代码任务优先 Qwen-Coder。
2. 长上下文文档优先 Kimi 或 MiMo。
3. 复杂推理和 bug 分析优先 DeepSeek。
4. OpenAI 作为高稳定兜底。
5. MiMo 只在 API 可用且效果验证通过后启用。

## Provider 开关

Provider 默认配置在 `ai/providers/providers.example.json`。
如需本机覆盖，复制 `ai/providers/providers.local.example.json` 为 `ai/providers/providers.local.json` 后修改。

`providers.local.json` 不允许提交到 Git。
