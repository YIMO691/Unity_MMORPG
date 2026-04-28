# AI 开发任务模板

## 当前任务

填写任务名称。

## 所属模块

填写模块名。

## 背景

这是一个 Unity MMORPG Demo。

## 技术栈

客户端：
- Unity
- C#
- xLua
- YooAsset/Addressables
- Luban
- Protobuf
- uGUI

服务端：
- .NET
- ASP.NET Core WebSocket
- PostgreSQL
- Redis
- Docker Compose

## 架构约束

- 客户端只负责表现和输入
- 服务端保持权威
- 客户端只上报意图
- 服务端决定伤害、掉落、奖励、任务进度
- C# 负责高频和底层
- Lua 负责 UI、任务、活动和轻玩法
- 不允许引入未确认的新技术栈

## 需要修改的文件

列出明确文件路径。

## 不允许修改的文件

列出禁止修改的范围。

## 需要产出

1. 代码
2. 测试
3. 文档更新
4. 运行方式
5. 验收标准

## 验收标准

列出可执行检查项。
