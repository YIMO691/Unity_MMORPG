# Android Build

## 环境要求

- Unity 2022.3 LTS（Android Build Support 已安装）
- Android SDK / NDK（Unity Hub 可自动安装）
- 一台 Android 设备或模拟器

## 构建步骤

### 1. 配置服务器地址

在 Unity 中打开 `Bootstrap` 场景，选择 `GameLauncher` GameObject，在 Inspector 中设置：
- `Server Base Url`: 你的局域网服务器地址（如 `http://192.168.1.100:5000`）

或通过代码设置：
```csharp
PlayerPrefs.SetString("server_url", "http://192.168.1.100:5000");
```

### 2. 构建 APK

菜单：**MmoDemo → Build Android APK**

或命令行：
```powershell
Unity -batchmode -quit -projectPath client/MmoDemoClient \
  -executeMethod MmoDemo.Client.Editor.AndroidBuilder.BuildAndroid \
  -buildTarget Android
```

### 3. 部署到设备

```powershell
# USB 连接设备后
adb install MMORPGDemo.apk
```

### 4. 确保服务器可访问

手机和服务器必须在同一局域网。启动服务器时绑定所有接口：

```powershell
# 服务端（PC）
dotnet run --project server/src/MmoDemo.Gateway/MmoDemo.Gateway.csproj --urls http://0.0.0.0:5000
```

## 触屏操作

- **左下角**：虚拟摇杆，拖拽移动角色
- **右下角**：技能按钮 1/2/3，点击释放技能
- 聊天和任务 UI 自动适配竖屏布局
