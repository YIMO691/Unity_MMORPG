# GitHub 仓库设置

当前项目远程仓库：

```text
https://github.com/YIMO691/MMORPGDemo3
```

本地 `origin` 已设置为上面的仓库地址。不要在未检查 `.gitignore` 和密钥前执行 `git push`。

## 验证 remote

```powershell
cd F:\AI_MMORPG
git remote -v
```

期望看到：

```text
origin  https://github.com/YIMO691/MMORPGDemo3.git (fetch)
origin  https://github.com/YIMO691/MMORPGDemo3.git (push)
```

## 如果需要重新设置 remote

```powershell
git remote set-url origin https://github.com/YIMO691/MMORPGDemo3.git
```

## 如果要用 GitHub CLI

当前本机如果没有安装 GitHub CLI `gh`，可以按下面安装：

1. 安装 GitHub CLI：

```powershell
winget install --id GitHub.cli
```

2. 关闭并重新打开终端，登录 GitHub：

```powershell
gh auth login
```

## 日常分支流程

```powershell
git checkout -b codex/<short-task-name>
# 完成一个可验证小任务
git status
git add .
git commit -m "Describe the scoped change"
git push -u origin codex/<short-task-name>
```

然后在 GitHub 上打开 Pull Request。PR 模板已经放在 `.github/PULL_REQUEST_TEMPLATE.md`。

## 参考

- GitHub CLI Windows 安装说明：<https://raw.githubusercontent.com/cli/cli/trunk/docs/install_windows.md>
