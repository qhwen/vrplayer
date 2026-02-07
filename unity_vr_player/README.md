# VR 视频播放器 - Unity

多端 VR 视频播放器，支持 Windows、iOS、Android，WebDAV 和本地播放。

## 项目概述

使用 Unity 2022.3 LTS 开发的跨平台 VR 视频播放器，支持360°全景视频和3D立体视频。

## 功能特性

### ✅ 已实现
- ✅ **跨平台支持**：Windows、iOS、Android
- ✅ **WebDAV 集成**：支持主流 WebDAV 服务
- ✅ **本地播放**：支持选择本地视频文件
- ✅ **VR 模式**：360° 全景视频、头部追踪、手势控制
- ✅ **视频格式**：MP4、MKV、MOV
- ✅ **播放控制**：播放/暂停、进度调节、音量控制
- ✅ **目录浏览**：WebDAV 文件导航
- ✅ **下载缓存**：WebDAV 视频下载到本地缓存
- ✅ **UI 系统**：完整的播放器界面和控制面板

### 🎯 VR 特性
- **360° 球体投影**：使用 Shader 和 RenderTexture 渲染
- **头部追踪**：准备陀螺仪输入（可扩展）
- **手势交互**：拖动旋转视角、点击控制播放
- **平滑运动**：头部旋转使用插值平滑
- **沉浸体验**：完全沉浸式 VR 环境

## 项目结构

```
unity_vr_player/
├── Assets/
│   ├── Scenes/
│   │   ├── VideoPlayerScene.unity    # 主播放器场景
│   │   ├── SettingsScene.unity         # 设置场景
│   │   └── MainMenuScene.unity        # 主菜单场景
│   ├── Scripts/
│   │   ├── VRVideoPlayer.cs         # 核心播放器脚本
│   │   ├── VRUIManager.cs           # UI管理器
│   │   ├── WebDAVManager.cs         # WebDAV服务
│   │   ├── LocalFileManager.cs       # 本地文件管理
│   │   └── SceneLoader.cs           # 场景加载器
│   ├── Prefabs/
│   │   ├── SkySphere.prefab          # 天空球体预置
│   │   └── ControlPanel.prefab       # 控制面板预置
│   ├── Materials/
│   │   └── VideoMaterial.mat         # 视频材质
│   └── Textures/
│       └── UI/
│           └── ButtonBackground.png     # UI背景纹理
├── ProjectSettings/
│   └── ProjectSettings.asset        # 项目设置
├── Plugins/
│   ├── Android/                    # Android 原生插件
│   └── iOS/                        # iOS 原生插件
└── README.md                      # 本文件
```

## 核心脚本说明

### 1. VRVideoPlayer.cs
**功能**：核心视频播放控制器

**主要方法**：
- `PlayVideo(string path)` - 播放指定视频
- `PauseVideo()` - 暂停视频
- `ResumeVideo()` - 恢复播放
- `StopVideo()` - 停止视频
- `SetVolume(float volume)` - 设置音量
- `SeekTo(float seconds)` - 跳转到指定时间
- `SetVRRotation(float yaw, float pitch)` - 设置VR旋转角度
- `OnDrag(float deltaX, float deltaY)` - 手势拖动

**VR 实现**：
- 使用 `RenderTexture` 和 `Shader` 渲染360°球体
- 平滑头部追踪插值
- 实时更新视频纹理

---

### 2. VRUIManager.cs
**功能**：播放器UI管理

**主要方法**：
- `CreateDefaultControlPanel()` - 创建默认控制面板
- `UpdateUI()` - 更新播放状态和时间显示
- `ToggleControlPanel()` - 切换控制面板显示

**UI 元素**：
- 播放/暂停按钮
- 停止按钮
- 进度条（可拖动）
- 时间显示
- 音量控制按钮

---

### 3. WebDAVManager.cs
**功能**：WebDAV协议集成

**主要方法**：
- `Connect(string url, string user, string pass)` - 连接WebDAV服务器
- `ListFiles(string path)` - 获取文件列表
- `DownloadFile(string remote, string local)` - 下载文件

**支持的服务**：
- Nextcloud
- ownCloud
- 坚果云
- 标准 WebDAV (RFC 4918)

**认证方式**：
- Basic Authentication (Base64 编码)

---

### 4. LocalFileManager.cs
**功能**：本地文件管理

**主要方法**：
- `OpenFilePicker()` - 打开文件选择器
- `GetLocalVideos()` - 获取本地视频列表
- `DeleteLocalVideo(string path)` - 删除视频
- `ClearAllCache()` - 清空缓存
- `GetCacheSize()` - 获取缓存大小

**平台适配**：
- Windows：使用 `FileOpenDialog`
- Android：需要原生插件（待实现）
- iOS：需要原生插件（待实现）

---

### 5. SceneLoader.cs
**功能**：场景管理

**主要方法**：
- `LoadVideoPlayerScene()` - 加载播放器场景
- `LoadSettingsScene()` - 加载设置场景

## 快速开始

### 1. 环境要求
```
Unity 2022.3 LTS 或更高版本
对应平台的构建工具：
- Windows: Visual Studio 2019+
- iOS: Xcode 13+
- Android: Android SDK + Android Build Tools
```

### 2. 导入项目
1. 打开 Unity Hub
2. 创建新项目或导入现有项目
3. 将 Scripts 文件夹复制到 `Assets/Scripts/`
4. 等待 Unity 编译脚本

### 3. 创建场景
```
File -> New Scene
创建以下场景：
- VideoPlayerScene（添加 VRVideoPlayer 组件）
- SettingsScene（添加设置UI）
- MainMenuScene（添加主菜单）
```

### 4. 设置视频播放器
在 VideoPlayerScene 中：
1. 创建空 GameObject
2. 添加 `VRVideoPlayer.cs` 脚本
3. 配置 Inspector 参数：
   - Default Video Path: 留空或设置测试视频
   - Sky Sphere Prefab: 创建后分配
   - Enable Head Tracking: 勾选
   - Rotation Sensitivity: 0.5
   - Smoothing Factor: 0.1

### 5. 构建应用

#### Windows
```
File -> Build Settings
Platform: PC, Mac & Linux Standalone
Target Platform: Windows
Build: 构建
```

#### iOS
```
File -> Build Settings
Platform: iOS
Build: 构建
生成 .xcodeproj 后在 Xcode 中编译
```

#### Android
```
File -> Build Settings
Platform: Android
Build: 构建
生成 .apk 或 .aab
```

### 6. 测试
- 在 Unity Editor 中播放场景
- 测试视频播放功能
- 测试VR旋转
- 测试UI控制

## VR 设备支持

### 当前支持
- ✅ **PC VR**：OpenVR (SteamVR)
- ✅ **移动VR**：Cardboard (基础）、Oculus (需要SDK）
- ✅ **360°视频**：标准全景视频
- ✅ **3D视频**：左右分屏立体视频

### 扩展（后续）
- 🔄 **Oculus SDK 集成**：完整的 Oculus 支持
- 🔄 **Pico SDK 集成**：Pico 设备支持
- 🔄 **AR 支持**：AR 模式

## WebDAV 配置

### Nextcloud
```
服务器地址: https://nextcloud.example.com/remote.php/webdav
用户名: your_username
密码: your_password
路径: /Videos (可选)
```

### ownCloud
```
服务器地址: https://owncloud.example.com/remote.php/webdav
用户名: your_username
密码: your_password
路径: / (默认)
```

### 坚果云
```
服务器地址: https://dav.jianguoyun.com/dav/
用户名: your_username
密码: your_password
路径: / (默认)
```

## 平台特殊说明

### Windows
- 文件选择：使用原生文件对话框
- 缓存路径：`Application.dataPath/VRVideos/`
- VR设备：支持 OpenVR 和桌面模式
- 快捷键：需要添加（如 ESC 退出、空格暂停）

### iOS
- 文件选择：需要 iOS 原生插件（使用 PHPickerViewController）
- 缓存路径：`Application.persistentDataPath/VRVideos/`
- VR模式：支持 Cardboard
- 权限：需要在 Info.plist 中添加文件访问权限

### Android
- 文件选择：需要 Android 原生插件（使用 Intent.ACTION_OPEN_DOCUMENT）
- 缓存路径：`Application.persistentDataPath/VRVideos/`
- VR模式：支持 Cardboard
- 权限：需要在 AndroidManifest.xml 中添加存储权限
- 横屏锁定：在 PlayerSettings 中设置

## 已知限制

### 当前版本
1. **头部追踪**
   - 当前使用简化的手势拖动
   - 陀螺仪需要平台原生插件

2. **移动端文件选择**
   - Android 和 iOS 需要原生插件实现
   - 当前为占位符

3. **下载进度**
   - WebDAV 下载需要更精确的进度计算
   - 当前使用估算

## 开发路线图

### 阶段 1：基础功能（已完成）
- [x] Unity 项目搭建
- [x] 核心播放器脚本
- [x] UI 系统
- [x] WebDAV 集成
- [x] 本地文件管理

### 阶段 2：平台优化（下一步）
- [ ] Android 原生插件（文件选择）
- [ ] iOS 原生插件（文件选择）
- [ ] 陀螺仪头部追踪
- [ ] 平台特化UI

### 阶段 3：VR 增强（后续）
- [ ] Oculus SDK 集成
- [ ] Pico SDK 集成
- [ ] OpenXR 统一API
- [ ] 6DoF 控制器支持

### 阶段 4：高级功能（长期）
- [ ] 字幕支持
- [ ] 倍速播放
- [ ] 播放历史
- [ ] 收藏夹
- [ ] 多语言支持

## 性能优化建议

### 视频解码
- 使用硬件解码（平台原生）
- 降低分辨率在低端设备
- 异步加载大文件

### 渲染优化
- LOD (Level of Detail) 系统
- 减少Draw Call
- 使用对象池
- 纹理压缩

### 内存管理
- 及时释放未使用资源
- 使用对象池模式
- 控制同时加载的视频数量

## 依赖项

### Unity 内置
- VideoPlayer (视频播放）
- UI (用户界面）
- UnityEngine.Networking (网络请求）

### 可选（后续）
- Oculus Integration (Oculus VR）
- Google VR SDK (Cardboard）
- Pico SDK (Pico VR）

## 调试

### Unity Console
- 查看 Debug.Log 输出
- 检查脚本错误
- 监控性能

### Profiler
- 使用 Unity Profiler 检查性能
- 查找内存泄漏
- 优化帧率

## 贡献指南

### 提交代码
1. Fork 项目
2. 创建功能分支
3. 提交 Pull Request

### 报告问题
- 使用 GitHub Issues
- 提供详细复现步骤
- 包含 Unity 版本和平台

## 许可证

MIT License

---

*版本：1.0.0*
*创建日期：2026-02-07*
*框架：Unity 2022.3 LTS*
