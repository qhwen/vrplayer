# Unity VR视频播放器 - 实施方案

## 项目概述

使用 Unity 2022.3+ 开发多端 VR 视频播放器，支持 Windows、iOS、Android，集成 WebDAV 和本地文件播放。

---

## 核心技术决策

### 1. VR框架选择
**推荐：** OpenXR（跨平台标准）

| 框架 | 优势 | 劣势 | 推荐度 |
|------|------|--------|---------|
| **OpenXR** | ✅ 标准化<br>✅ 跨平台<br>✅ 未来主流 | ⚠️ 新，文档少 | ⭐⭐⭐⭐⭐ |
| OpenVR (SteamVR) | ✅ 成熟<br>✅ 文档多 | ⚠️ VR Only<br>⚠️ 非VR模式需另做 | ⭐⭐⭐⭐ |
| Oculus SDK | ✅ 优化好<br>✅ 功能全 | ❌ 仅Oculus<br>❌ 需要多套代码 | ⭐⭐⭐ |
| Cardboard | ✅ 轻量<br>✅ 入门级 | ❌ 功能有限<br>❌ 非VR设备不支持 | ⭐⭐ |

**最终选择：** **OpenXR + Cardboard兼容模式**

### 2. 视频播放方式
**推荐：** Unity Video Player + RenderTexture

| 方式 | 优势 | 劣势 |
|------|------|--------|
| **Unity VideoPlayer** | ✅ 内置<br>✅ 跨平台<br>✅ 简单 | ⚠️ 格式支持有限<br>⚠️ 性能中等 |
| **AVPro Video** | ✅ 性能强<br>✅ 格式全<br>✅ 功能多 | ❌ 收费($95)<br>❌ 依赖量大 | |
| **自定义 FFmpeg** | ✅ 完全控制<br>✅ 格式全 | ❌ 开发复杂<br>❌ 需要插件 |

**第一阶段选择：** **Unity VideoPlayer**（快速原型）
**长期优化：** 评估 AVPro Video（如需更强性能）

### 3. UI框架选择
**推荐：** Unity UI Toolkit (2022+)

| 框架 | 优势 | 推荐度 |
|------|------|---------|
| Unity UI Toolkit | ✅ 新标准<br>✅ 性能好<br>✅ CSS样式 | ⭐⭐⭐⭐⭐ |
| uGUI | ✅ 成熟<br>✅ 教程多 | ⭐⭐⭐ |
| NGUI | ❌ 需购买<br>❌ 不推荐 | ⭐ |

**选择：** **Unity UI Toolkit**（项目长远考虑）

**第一阶段方案：** **uGUI**（快速实现，教程多）

---

## 详细实施步骤

### Week 1: 项目初始化（2-3天）

#### Day 1-2: Unity项目搭建
```bash
# 安装 Unity 2022.3 LTS
# 创建新项目
Project Template: 3D (Core)
Project Name: VRVideoPlayer
Location: unity_vr_player/

# 项目结构设置
Assets/
├── Scripts/           # 所有C#脚本
├── Scenes/           # 场景文件
├── Prefabs/          # 预置体
├── Materials/         # 材质
├── Textures/          # 纹理
└── Resources/          # 资源
```

**配置项目设置：**
- Player Settings → XR Plug-in Management → 勾选 OpenXR
- Player Settings → XR Plug-in Management → Cardboard XR Plugin (兼容模式)
- Player Settings → Resolution and Scaling → Target 1920x1080
- Player Settings → Other Settings → Auto Graphics API → Vulkan (Windows)、Metal (iOS/Mac)

**关键检查点：**
- [ ] OpenXR Plugin 已导入
- [ ] 项目能编译到目标平台
- [ ] 基础场景能运行

---

#### Day 2-3: 基础场景创建
```
创建场景：
1. VideoPlayerScene.unity（主播放器场景）
2. SettingsScene.unity（设置场景）
3. MainMenuScene.unity（主菜单场景）

场景内容：
- Main Camera (带 XR Origin 组件)
- Directional Light
- 空的GameObject作为根节点
- EventSystem (用于UI交互)
```

**关键配置：**
- Camera → Clear Flags → Solid Color (黑色背景)
- XR Origin → Camera Offset → Y=1.6 (模拟人眼高度)

---

### Week 2: 核心播放器（5-7天）

#### Day 4-6: 360°球体渲染
```
创建 SkySphere 预置：
1. 创建 Sphere (半径 50)
2. 创建 Material (Shader: Unlit/Texture)
3. 配置球体：
   - Scale: (-1, 1, 1) - 反转X轴使视频正确显示
   - Rotation: (0, 0, 0)
   - Culling Mask: Nothing
```

**技术要点：**
```csharp
// 反转球体X轴（视频纹理在内部，需要翻转）
skySphere.transform.localScale = new Vector3(-1, 1, 1) * 50;
```

**检查点：**
- [ ] 360°视频能正确显示
- [ ] 无明显变形
- [ ] 性能流畅 (>30fps)

---

#### Day 6-7: 视频播放脚本
```
实现 VRVideoPlayer.cs 核心功能：

必需功能：
1. VideoPlayer 初始化
   - videoPlayer = gameObject.AddComponent<VideoPlayer>()
   - videoPlayer.url = path
   - videoPlayer.prepare()

2. RenderTexture 更新
   - renderTexture = new RenderTexture(1920, 1080)
   - Graphics.Blit(videoPlayer.texture, renderTexture)
   - sphereMaterial.mainTexture = renderTexture

3. 播放控制
   - PlayVideo(string path)
   - PauseVideo()
   - ResumeVideo()
   - StopVideo()
   - SetVolume(float)
   - SeekTo(float seconds)

4. VR旋转控制
   - headYaw, headPitch 变量
   - SetVRRotation(float yaw, float pitch)
   - SmoothHeadTracking() (插值)
```

**检查点：**
- [ ] 视频能正常加载和播放
- [ ] 播放/暂停功能正常
- [ ] 音量控制有效
- [ ] 进度跳转准确

---

#### Day 7-9: VR头部追踪
```
实现头部追踪：

方案A - 输入管理器（推荐）：
1. 创建 XRInputManager.cs
2. 使用 OpenXR Input Subsystem
3. 监听头部旋转：
   InputAction positionAction
   InputAction rotationAction

方案B - 陀螺仪（简单）：
1. 使用 Input.gyro.attitude (Unity内置)
2. 应用到VR旋转

推荐：方案A（OpenXR）
```

**检查点：**
- [ ] 头部旋转能实时控制视角
- [ ] 旋转平滑（无明显抖动）
- [ ] 角度范围合理（-180°~180°）

---

#### Day 9-11: UI系统实现
```
使用 uGUI 创建控制面板：

1. 创建 Canvas (Screen Space - Overlay)
2. 添加控制元素：
   - Play/Pause 按钮
   - Stop 按钮
   - 进度条 Slider
   - 时间显示 Text
   - 音量按钮 (+/-)
   - VR模式开关

3. VRUIManager.cs 管理
   - 按钮事件绑定
   - UI显示更新
   - 与VRVideoPlayer通信
```

**检查点：**
- [ ] UI显示正常（不遮挡视频）
- [ ] 按钮响应灵敏
- [ ] 时间显示准确
- [ ] 支持手柄输入（如果可用）

---

### Week 3: WebDAV集成（3-4天）

#### Day 12-14: WebDAV连接管理
```
实现 WebDAVManager.cs：

核心功能：
1. 连接测试
   - Connect(string url, string user, string pass)
   - Basic Authentication (Base64)

2. 文件列表
   - ListFiles(string path)
   - PROPFIND 方法 (WebDAV协议)
   - XML解析响应

3. 文件下载
   - DownloadFile(string remote, string local)
   - UnityWebRequest 异步下载
   - 进度回调

4. 配置保存
   - PlayerPrefs 保存服务器信息
   - 加载时自动连接
```

**关键代码：**
```csharp
// Basic Authentication
string auth = "Basic " + System.Convert.ToBase64String(
    System.Text.Encoding.UTF8.GetBytes(username + ":" + password)
);
request.SetRequestHeader("Authorization", auth);
```

**检查点：**
- [ ] 能成功连接到WebDAV服务器
- [ ] 能获取文件列表
- [ ] 能下载文件到本地
- [ ] 进度显示准确

---

#### Day 15-17: 本地文件管理
```
实现 LocalFileManager.cs：

核心功能：
1. 文件选择器
   - Windows: FileOpenDialog
   - Android: 原生插件
   - iOS: 原生插件

2. 本地缓存管理
   - GetCacheDirectory()
   - GetLocalVideos()
   - AddLocalVideo(path)
   - DeleteLocalVideo(path)
   - ClearAllCache()

3. 缓存路径设置
   - Application.persistentDataPath/VRVideos/ (移动端)
   - Application.dataPath/VRVideos/ (桌面端)
```

**关键点：**
- 文件选择器需要平台原生插件

**移动端插件示例（Android）：**
```java
// Android/iOS 需要原生插件调用系统文件选择器
```

**检查点：**
- [ ] 能选择本地视频文件
- [ ] 本地播放正常
- [ ] 缓存管理正确
- [ ] 文件删除有效

---

### Week 4: 平台构建（3-4天）

#### Day 18-19: Windows构建
```
Windows 平台配置：

1. Player Settings:
   - Platform: PC, Mac & Linux Standalone
   - Architecture: x86_64
   - Scripting Backend: IL2CPP

2. 构建设置:
   - Build Settings → Build
   - Target Platform: Windows
   - Development Build: (调试时勾选)

3. 生成文件:
   - VRVideoPlayer.exe (可执行)
   - VRVideoPlayer_Data/ (资源文件夹)

4. 文件关联:
   - 在设置中注册.mp4文件关联
```

**测试清单：**
- [ ] .exe能正常启动
- [ ] VR设备检测正常
- [ ] 视频播放流畅
- [ ] 窗口模式/全屏切换正常

---

#### Day 20-21: iOS构建
```
iOS 平台配置：

1. Player Settings:
   - Platform: iOS
   - Target Device: iPhone + iPad
   - Architecture: ARM64
   - Minimum iOS Version: 11.0

2. iOS 专有设置:
   - Camera Usage Description: 需要用于VR
   - Photo Library Usage Description: 用于文件访问

3. 构建流程:
   - Build Settings → Build
   - 生成 .xcodeproj

4. Xcode 配置:
   - 打开生成的Xcode项目
   - 添加 Info.plist 权限
   - 编译并生成 .ipa

5. 测试:
   - 在真机或模拟器测试
   - TestFlight分发
```

**关键权限：**
```xml
<!-- Info.plist -->
<key>NSPhotoLibraryUsageDescription</key>
<string>需要访问相册选择视频文件</string>

<key>NSPhotoLibraryAddUsageDescription</key>
<string>需要保存视频文件到相册</string>
```

---

#### Day 22-23: Android构建
```
Android 平台配置：

1. Player Settings:
   - Platform: Android
   - Minimum API Level: 21 (Android 5.0)
   - Target API Level: 33 (Android 13)

2. Android 专有设置:
   - Graphics APIs: OpenGLES 3.0 / Vulkan
   - Auto Graphics API: Vulkan

3. 构建设置:
   - Build Settings → Build
   - Build System: Gradle
   - Export Project: (如果需要自定义配置)

4. AndroidManifest.xml:
   - 添加存储权限
   - 添加相机权限（如果需要）

5. 签名:
   - Debug Build: 使用调试keystore
   - Release Build: 使用发布keystore

6. 生成文件:
   - .apk (用于测试)
   - .aab (用于发布到Play Store)
```

**关键权限：**
```xml
<!-- AndroidManifest.xml -->
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE"/>
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE"/>
<uses-permission android:name="android.permission.CAMERA"/>
```

---

### Week 5: 测试优化（2-3天）

#### Day 24-26: 跨平台测试
```
测试矩阵：

视频格式测试:
- [ ] MP4 (H.264)
- [ ] MP4 (H.265)
- [ ] MKV
- [ ] MOV

分辨率测试:
- [ ] 1080p (1920x1080)
- [ ] 1440p (2560x1440)
- [ ] 4K (3840x2160)

VR设备测试:
- [ ] PC VR (Oculus Rift / HTC Vive)
- [ ] 移动VR (Oculus Quest / Pico)
- [ ] Cardboard (入门级)

WebDAV测试:
- [ ] Nextcloud
- [ ] ownCloud
- [ ] 坚果云

测试设备:
- Windows 10/11
- iOS 14+/15+
- Android 10+/13+
```

---

#### Day 27-28: 性能优化
```
性能优化检查点：

渲染性能:
- [ ] 帧率 > 30fps
- [ ] Draw Call < 100
- [ ] 内存 < 500MB
- [ ] GPU 使用 < 80%

加载性能:
- [ ] 视频加载时间 < 5秒
- [ ] 场景加载时间 < 2秒
- [ ] WebDAV连接 < 3秒

用户体验:
- [ ] 头部旋转延迟 < 50ms
- [ ] UI响应 < 100ms
- [ ] 切换场景 < 1秒
```

---

## 风险评估与对策

### 高风险
| 风险 | 影响 | 对策 |
|------|------|------|
| **移动端文件选择器实现复杂** | Android/iOS需要原生插件 | Week 2预留3天开发插件 |
| **WebDAV协议兼容性** | 不同服务实现可能不同 | 先测试主流服务，做适配层 |
| **视频解码性能** | 4K/8K视频卡顿 | 提供分辨率选项，降级播放 |
| **Unity跨平台Bug** | iOS/Android特定问题 | 每个平台独立测试 |

### 中风险
| 风险 | 影响 | 对策 |
|------|------|------|
| **360°视频畸变** | 球体渲染可能变形 | 使用高质量球体模型，调整UV映射 |
| **头部追踪精度** | 陀螺仪噪声 | 添加低通滤波和插值 |
| **内存泄漏** | 长时间运行崩溃 | 定期监控，及时释放资源 |

### 低风险
| 风险 | 影响 | 对策 |
|------|------|------|
| **UI设计** | 用户体验受影响 | 参考VR UI最佳实践 |
| **跨平台UI差异** | 需要单独调整 | 建立UI抽象层 |

---

## 里程碑与交付物

### Milestone 1: MVP（Week 4结束）
**交付物：**
- [ ] Windows桌面版 .exe（可播放本地MP4）
- [ ] Android .apk（可播放本地MP4）
- [ ] 基础VR模式（360°视频+手势控制）
- [ ] WebDAV基础连接

**验收标准：**
- 能播放本地视频文件
- 360°视频能旋转
- 基础UI控制正常
- 三个平台能运行

---

### Milestone 2: WebDAV集成（Week 5结束）
**交付物：**
- [ ] WebDAV服务器连接
- [ ] WebDAV文件列表显示
- [ ] WebDAV文件下载
- [ ] 下载进度显示
- [ ] 本地缓存管理

**验收标准：**
- 能连接到主流WebDAV服务
- 能浏览文件目录
- 能下载文件到本地
- 缓存管理正常

---

### Milestone 3: 完整产品（Week 6结束）
**交付物：**
- [ ] 完整VR体验（头部追踪）
- [ ] 播放历史记录
- [ ] 播放列表管理
- [ ] 设置界面（WebDAV配置）
- [ ] 性能优化
- [ ] 所有平台测试通过

**验收标准：**
- VR体验流畅（>30fps）
- 三个平台功能一致
- 无严重Bug
- 性能满足基本要求

---

## 开发资源需求

### 团队配置
- Unity开发者：1人
- 熟悉C#
- 了解VR基本概念

### 开发工具
- Unity 2022.3 LTS（免费）
- Visual Studio 2022（Windows开发）
- Xcode 14+（iOS开发）
- Android Studio（Android插件）
- Git（版本控制）

### 开发设备
- Windows PC（开发+测试）
- iOS设备（可选，用于真机测试）
- Android设备（可选）
- VR头显（可选，用于测试VR模式）

### 第三方服务（测试用）
- Nextcloud/ownCloud 实例
- 公开视频源

---

## 时间表

```
Week 1: ████████░░░░░░░░░ 20%  项目初始化
Week 2: ████░░░░░░░░░░░░ 35%  核心播放器
Week 3: ██████░░░░░░░░░ 25%  WebDAV集成
Week 4: ██████░░░░░░░░░ 20%  平台构建
Week 5: ████████████░░░░ 10%  测试优化
         ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%
```

**总开发周期：** 5-6周
**关键里程碑：**
- Week 2结束：MVP可用（基础播放）
- Week 3结束：WebDAV集成完成
- Week 4结束：平台构建完成
- Week 5结束：产品发布就绪

---

## 下一步行动

### 立即开始
1. [ ] 创建Unity项目
2. [ ] 创建基础场景
3. [ ] 实现VRVideoPlayer.cs核心脚本
4. [ ] 测试360°球体渲染

### Week 2
1. [ ] 实现UI系统
2. [ ] 添加播放控制
3. [ ] 实现基础头部追踪

### Week 3
1. [ ] 实现WebDAVManager
2. [ ] 实现LocalFileManager
3. [ ] 测试文件浏览和下载

### Week 4
1. [ ] 构建Windows版本
2. [ ] 构建iOS版本
3. [ ] 构建Android版本
4. [ ] 跨平台测试

### Week 5
1. [ ] 性能优化
2. [ ] Bug修复
3. [ ] 用户体验改进
4. [ ] 准备发布

---

## 附录：参考资源

### Unity官方文档
- [Unity XR](https://docs.unity3d.com/Packages/com.unity.xr.openxr@latest)
- [VideoPlayer](https://docs.unity3d.com/ScriptReference/VideoPlayer.html)
- [UI Toolkit](https://docs.unity3d.com/Manual/UIElements.html)

### VR SDK资源
- [OpenXR](https://github.com/KhronosGroup/OpenXR-SDK-Unity)
- [Oculus Integration](https://developer.oculus.com/documentation/unity/unity-overview)
- [Cardboard XR Plugin](https://developers.google.com/vr/discover/cardboard)

### WebDAV协议
- [RFC 4918](https://tools.ietf.org/html/rfc4918)
- [Nextcloud WebDAV](https://docs.nextcloud.com/server/stable/developer_manual/webdav)

---

*方案版本：1.0*
*创建日期：2026-02-07*
*预估周期：5-6周*
