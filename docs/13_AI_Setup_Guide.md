# AI API 设置指南

## 1. 用户需要做什么

用户需要自己完成：
- 注册 OpenAI / 阿里云百炼 / Kimi / DeepSeek / MiMo
- 购买或开通 API 服务
- 创建 API Key
- 把 API Key 填入本地 `.env`

## 2. 不要做什么

- 不要把 API Key 发给别人
- 不要把 API Key 写进代码
- 不要提交 `.env`
- 不要把密钥截图发到公开仓库

## 3. 推荐最小开通组合

最小组合：
- OpenAI
- Qwen-Coder

增强组合：
- OpenAI
- Qwen-Coder
- Kimi
- DeepSeek

完整组合：
- OpenAI
- Qwen-Coder
- Kimi
- DeepSeek
- MiMo

## 4. 配置步骤

1. 复制 `.env.example` 为 `.env`
2. 填入已购买平台的 API Key
3. 运行：

```bash
pip install -r requirements-ai.txt
python ai/scripts/check_env.py
python ai/scripts/smoke_test.py
```

4. 查看：

```text
ai/outputs/smoke_test_report.md
```

## 5. 常见问题

### Q: 没有某个平台的 Key 会怎样？

A: 对应 provider 会被跳过，不影响其他模型。

### Q: 哪个模型最适合写 Unity？

A: 优先 Qwen-Coder，失败时用 OpenAI 或 Kimi。

### Q: 哪个模型最适合分析 bug？

A: 优先 DeepSeek，其次 Kimi。

### Q: MiMo 必买吗？

A: 不必。先确认 API 可用和实际效果，再决定是否购买。

### Q: 能不能让 AI 自动改完整项目？

A: 不建议。必须按小任务、小范围、可验收的方式执行。
