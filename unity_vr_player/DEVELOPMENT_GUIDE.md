# Unity VR 视频播放器 - 开发指南

> **版本**: 1.0.0  
> **更新日期**: 2026-03-23  
> **Unity 版本**: 2022.3 LTS  
> **框架**: Unity 2022.3 LTS

---

## 目录

- [项目概述](#项目概述)
- [架构设计](#架构设计)
- [核心模块](#核心模块)
- [技术栈](#技术栈)
- [开发环境配置](#开发环境配置)
- [代码结构](#代码结构)
- [核心功能实现](#核心功能实现)
- [平台适配](#平台适配)
- [构建与部署](#构建与部署)
- [API 文档](#api-文档)
- [开发规范](#开发规范)
- [常见问题](#常见问题)

---

## 项目概述

### 项目简介

Unity VR 视频播放器是一个跨平台的 VR 视频播放解决方案，支持 360° 全景视频和 3D 立体视频播放。项目采用分层架构设计，具有良好的扩展性和维护性。

### 核心功能

- **跨平台支持**: Windows、iOS、Android
- **VR 播放**: 360° 全景视频、头部追踪、手势控制
- **多数据源**: 本地文件、WebDAV 云存储
- **智能缓存**: 自动缓存管理、下载进度追踪
- **权限管理**: Android 运行时权限适配（Android 10-15）
- **系统选择器**: Android SAF (Storage Access Framework) 文件选择

### 支持格式

- **视频**: MP4、MKV、MOV
- **协议**: 本地文件、HTTP/HTTPS、WebDAV、Android Content URI

---

## 架构设计

### 分层架构

项目采用清晰的分层架构，遵循单一职责原则：

```
┌─────────────────────────────────────────┐
│           Presentation Layer            │
│  (UI、交互、场景管理)                      │
├─────────────────────────────────────────┤
│          Application Layer              │
│  (播放用例、下载用例、状态流转)             │
├─────────────────────────────────────────┤
│            Domain Layer                 │
│  (接口定义、实体、业务规则)                 │
├─────────────────────────────────────────┤
│         Infrastructure Layer            │
│  (本地存储、WebDAV、缓存服务)              │
├─────────────────────────────────────────┤
│           Platform Layer                │
│  (Android/iOS/Windows 平台适配)          │
└─────────────────────────────────────────┘
```

### 核心接口

#### IPlaybackService - 播放服务接口

```csharp
public interface IPlaybackService
{
    PlaybackState State { get; }
    PlaybackSnapshot Snapshot { get; }
    PlaybackError LastError { get; }
    bool HasSource { get; }
    string CurrentSource { get; }
    Texture CurrentTexture { get; }

    bool Open(string source);
    void Play();
    void Pause();
    void Stop();
    void Seek(float seconds);
    void SetVolume(float volume);

    event Action<PlaybackState> StateChanged;
    event Action<PlaybackSnapshot> PlaybackUpdated;
    event Action<PlaybackError> ErrorOccurred;
}
```

#### IVideoSource - 视频源接口

```csharp
public interface IVideoSource
{
    string SourceName { get; }
    Task<List<VideoFile>> ListAsync(string path = "");
    Task<bool> DownloadAsync(VideoFile sourceFile, string localPath, Action<float> onProgress = null);
}
```

#### ICacheService - 缓存服务接口

```csharp
public interface ICacheService
{
    string GetPath(string key, string extension = ".mp4");
    bool Exists(string key, string extension = ".mp4");
    bool Store(string key, string sourcePath, string extension = ".mp4");
    bool Evict(string key, string extension = ".mp4");
    long GetTotalSizeBytes();
    void Clear();
}
```

### 状态机设计

播放器采用显式状态机管理播放状态：

```
    ┌──────┐
    │ Idle │
    └──┬───┘
       │ Open()
       ▼
  ┌──────────┐
  │Preparing │◄────────┐
  └────┬─────┘         │
       │ Prepare()     │ Error + Retry
       ▼               │
    ┌─────┐            │
    │Ready│────────────┘
    └──┬──┘
       │ Play()
       ▼
   ┌───────┐
   │Playing│◄──► Pause() ──► ┌──────┐
   └───┬───┘                 │Paused│
       │                     └──────┘
       │ Error
       ▼
    ┌─────┐
    │Error│
    └─────┘
```

**状态说明**：
- **Idle**: 初始状态，无媒体源
- **Preparing**: 正在准备媒体资源
- **Ready**: 准备完成，可以播放
- **Playing**: 正在播放
- **Paused**: 已暂停
- **Error**: 播放错误

---

## 核心模块

### 1. 播放模块 (Playback)

#### UnityVideoPlaybackService

Unity VideoPlayer 的封装实现，提供完整的播放控制。

**关键特性**：
- 显式状态机管理
- 超时检测机制
- 错误映射与转换
- 路径规范化处理

**配置参数**：
```csharp
[Header("Playback Settings")]
[SerializeField, Range(5f, 60f)] private float prepareTimeoutSeconds = 20f;
[SerializeField] private bool autoPlayOnOpen;
[SerializeField] private bool loopPlayback;
[SerializeField, Range(0f, 1f)] private float initialVolume = 1f;
```

**播放快照** (PlaybackSnapshot)：
```csharp
public struct PlaybackSnapshot
{
    public PlaybackState state;
    public float positionSeconds;
    public float durationSeconds;
    public float normalizedProgress;
    public bool isBuffering;
    public string source;
}
```

### 2. VR 渲染模块

#### VRVideoPlayer

VR 视频渲染和交互层。

**核心功能**：
- 360° 球体投影
- 头部追踪（平滑插值）
- 手势拖动控制
- RenderTexture 纹理更新

**配置参数**：
```csharp
[Header("Video Settings")]
[SerializeField] private string defaultVideoPath = "";
[SerializeField] private GameObject skySpherePrefab;
[SerializeField, Range(512, 4096)] private int renderTextureWidth = 1920;
[SerializeField, Range(512, 4096)] private int renderTextureHeight = 1080;

[Header("Input Settings")]
[SerializeField] private bool enableHeadTracking = true;
[SerializeField] private float rotationSensitivity = 0.5f;
[SerializeField, Range(0.01f, 1f)] private float smoothingFactor = 0.1f;
[SerializeField] private bool enablePointerDrag = true;
[SerializeField] private float pointerDeltaScale = 1f;
```

**VR 旋转实现**：
```csharp
private void SmoothHeadTracking()
{
    currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, smoothingFactor);
    currentPitch = Mathf.Lerp(currentPitch, targetPitch, smoothingFactor);
}

private void ApplyVRRotation()
{
    if (skySphereTransform == null) return;
    skySphereTransform.rotation = Quaternion.Euler(currentPitch, -currentYaw, 0f);
}
```

### 3. UI 模块

#### VideoBrowserUI

移动端友好的视频浏览器界面。

**功能列表**：
- 视频列表展示
- 播放控制（播放/暂停/停止/进度）
- 权限管理按钮
- 系统文件选择器入口

**UI 结构**：
```
VideoBrowserCanvas (Canvas)
└── VideoBrowserPanel (Image)
    ├── Title (Text)
    ├── Status (Text)
    ├── CurrentVideo (Text)
    ├── ControlRow (HorizontalLayout)
    │   ├── RefreshButton
    │   ├── PauseResumeButton
    │   ├── StopButton
    │   └── BackButton
    ├── ProgressRow
    │   ├── ProgressSlider
    │   └── TimeLabel
    ├── PermissionRow
    │   ├── SelectVideosButton
    │   └── ScanSettingsButton
    └── VideoScroll (ScrollRect)
        └── Content (Video Items)
```

### 4. 本地文件管理模块

#### LocalFileManager

本地媒体发现和缓存管理器。

**核心功能**：
- 本地视频扫描（递归目录扫描）
- Android MediaStore 查询
- Android SAF 文件选择
- 权限请求流程
- 缓存管理

**Android 权限适配**：
```csharp
// Android 13+ (API 33+)
private const string AndroidPermissionReadMediaVideo = "android.permission.READ_MEDIA_VIDEO";

// Android 14+ (API 34+) - 部分媒体权限
private const string AndroidPermissionReadMediaVisualUserSelected = 
    "android.permission.READ_MEDIA_VISUAL_USER_SELECTED";
```

**权限请求流程**：
```csharp
private IEnumerator RequestReadableMediaPermissionRoutine()
{
    permissionRequestInFlight = true;
    
    // Android 13+
    if (sdkInt >= 33)
    {
        yield return RequestSinglePermission(AndroidPermissionReadMediaVideo);
        
        // Android 14+ 部分权限
        if (sdkInt >= 34 && !HasMoviesDirectoryPermission())
        {
            yield return RequestSinglePermission(AndroidPermissionReadMediaVisualUserSelected);
        }
    }
    else
    {
        yield return RequestSinglePermission(Permission.ExternalStorageRead);
    }
    
    permissionRequestInFlight = false;
}
```

**Movies 目录扫描范围限制**：
```csharp
private bool MatchesMoviesScope(string filePath, string relativePath)
{
    if (!string.IsNullOrWhiteSpace(relativePath))
    {
        string normalizedRelative = relativePath.Replace("\\", "/")
            .Trim().TrimStart('/').ToLowerInvariant();
            
        if (includeMoviesSubdirectories)
        {
            return normalizedRelative == "movies" || 
                   normalizedRelative.StartsWith("movies/");
        }
        
        return normalizedRelative == "movies";
    }
    // ... 文件路径检查
}
```

### 5. WebDAV 模块

#### WebDAVManager

WebDAV 协议集成。

**支持的服务**：
- Nextcloud
- ownCloud
- 坚果云
- 标准 WebDAV (RFC 4918)

**认证方式**：
- Basic Authentication (Base64 编码)

**XML 解析**：
```csharp
private List<VideoFile> ParseWebDavResponse(string xml)
{
    XmlDocument document = new XmlDocument();
    document.LoadXml(xml);
    
    XmlNamespaceManager ns = new XmlNamespaceManager(document.NameTable);
    ns.AddNamespace("d", "DAV:");
    
    XmlNodeList responseNodes = document.SelectNodes("//d:response", ns);
    // ... 解析逻辑
}
```

### 6. 缓存服务模块

#### FileCacheService

基于文件系统的缓存服务。

**特性**：
- SHA256 哈希键映射
- 自动目录创建
- 缓存大小统计
- 批量清理

**缓存键生成**：
```csharp
private static string BuildSafeKey(string key)
{
    string input = string.IsNullOrWhiteSpace(key) 
        ? Guid.NewGuid().ToString("N") 
        : key.Trim();
    
    using (SHA256 sha256 = SHA256.Create())
    {
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder sb = new StringBuilder(hash.Length * 2);
        
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("x2"));
        }
        
        return sb.ToString();
    }
}
```

---

## 技术栈

### 核心技术

| 技术 | 版本 | 用途 |
|------|------|------|
| Unity | 2022.3 LTS | 游戏引擎 |
| C# | 9.0 | 编程语言 |
| .NET | Standard 2.1 | 运行时 |

### Unity 模块

- **VideoPlayer**: 视频播放
- **UI System**: 用户界面
- **UnityEngine.Networking**: 网络请求
- **UnityEngine.Video**: 视频解码

### 平台 SDK

#### Android
- **minSdkVersion**: 21 (Android 5.0)
- **targetSdkVersion**: 34 (Android 14)
- **Storage Access Framework**: 文件选择
- **MediaStore**: 媒体库查询

#### iOS
- **最低版本**: iOS 11.0
- **PHPickerViewController**: 文件选择（待实现）

#### Windows
- **最低版本**: Windows 10
- **DirectX**: 11+

### 第三方库

无外部依赖，仅使用 Unity 内置模块。

---

## 开发环境配置

### 必需软件

1. **Unity Hub** 3.0.0+
2. **Unity Editor** 2022.3 LTS
3. **Git** (版本控制)
4. **Visual Studio** / **VS Code** (代码编辑)

### 平台开发工具

#### Android
- **Android SDK** (通过 Unity Hub 安装)
- **Android Build Tools**
- **JDK 11**

#### iOS (Mac only)
- **Xcode** 15.0+
- **CocoaPods** (如需要)

#### Windows
- **Visual Studio** 2019+ (Windows 构建支持)

### 项目设置

1. **克隆仓库**：
   ```bash
   git clone https://github.com/qhwen/vrplayer.git
   cd vrplayer
   ```

2. **打开项目**：
   - 启动 Unity Hub
   - 点击 "Add" → 选择 `unity_vr_player` 文件夹
   - 双击打开项目

3. **安装依赖**：
   - Unity 会自动下载缺失模块
   - 等待编译完成

4. **配置场景**：
   - 打开 `Assets/Scenes/Bootstrap.unity`
   - 确认场景包含必要组件

---

## 代码结构

### 目录组织

```
unity_vr_player/
├── Assets/
│   ├── Editor/                    # 编辑器扩展
│   │   └── BuildAndroid.cs        # Android 构建脚本
│   ├── Plugins/                   # 平台插件
│   │   └── Android/
│   │       ├── SAFPicker.androidlib/  # SAF 文件选择器
│   │       └── MediaPermissions.aar   # 权限辅助库
│   ├── Scenes/                    # 场景文件
│   │   └── Bootstrap.unity        # 启动场景
│   ├── Scripts/                   # C# 脚本
│   │   ├── Application/           # 应用层接口
│   │   │   ├── ICacheService.cs
│   │   │   └── IVideoSource.cs
│   │   ├── Data/                  # 数据层实现
│   │   │   ├── FileCacheService.cs
│   │   │   ├── LocalVideoSource.cs
│   │   │   └── WebDavVideoSource.cs
│   │   ├── Playback/              # 播放层
│   │   │   ├── IPlaybackService.cs
│   │   │   ├── PlaybackError.cs
│   │   │   ├── PlaybackErrorCode.cs
│   │   │   ├── PlaybackSnapshot.cs
│   │   │   ├── PlaybackState.cs
│   │   │   └── UnityVideoPlaybackService.cs
│   │   ├── Platform/              # 平台层
│   │   │   └── AndroidRuntimeConfigurator.cs
│   │   ├── AppRuntimeBootstrap.cs # 运行时引导
│   │   ├── LocalFileManager.cs    # 本地文件管理
│   │   ├── SceneLoader.cs         # 场景加载
│   │   ├── VideoBrowserUI.cs      # 视频浏览器 UI
│   │   ├── VideoFile.cs           # 视频文件实体
│   │   ├── VRUIManager.cs         # VR UI 管理器
│   │   ├── VRVideoPlayer.cs       # VR 播放器
│   │   └── WebDAVManager.cs       # WebDAV 管理
│   ├── Packages/                  # Unity 包配置
│   └── ProjectSettings/           # 项目设置
├── README.md                      # 项目说明
├── IMPLEMENTATION_PLAN.md         # 实施计划
├── REQUIREMENTS_TRACKER.md        # 需求追踪
└── DETAILED_PLAN.md               # 详细计划
```

### 代码分层

**Presentation Layer** (表现层)：
- `VideoBrowserUI.cs` - 视频浏览器 UI
- `VRUIManager.cs` - VR 播放器 UI
- `SceneLoader.cs` - 场景管理

**Application Layer** (应用层)：
- `IPlaybackService.cs` - 播放服务接口
- `IVideoSource.cs` - 视频源接口
- `ICacheService.cs` - 缓存服务接口

**Domain Layer** (领域层)：
- `VideoFile.cs` - 视频文件实体
- `PlaybackState.cs` - 播放状态枚举
- `PlaybackError.cs` - 播放错误结构
- `PlaybackSnapshot.cs` - 播放快照

**Infrastructure Layer** (基础设施层)：
- `UnityVideoPlaybackService.cs` - Unity 播放器实现
- `LocalVideoSource.cs` - 本地视频源
- `WebDavVideoSource.cs` - WebDAV 视频源
- `FileCacheService.cs` - 文件缓存服务

**Platform Layer** (平台层)：
- `AndroidRuntimeConfigurator.cs` - Android 运行时配置
- `SAFPicker.androidlib` - Android SAF 插件
- `MediaPermissions.aar` - Android 权限插件

---

## 核心功能实现

### 1. 视频播放流程

```csharp
// 1. 初始化播放服务
playbackService = GetComponent<UnityVideoPlaybackService>();

// 2. 打开视频源
if (playbackService.Open(videoPath))
{
    // 3. 开始播放
    playbackService.Play();
}

// 4. 监听事件
playbackService.StateChanged += OnStateChanged;
playbackService.PlaybackUpdated += OnPlaybackUpdated;
playbackService.ErrorOccurred += OnErrorOccurred;
```

### 2. VR 渲染流程

```csharp
// 1. 创建天空球
skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
skySphereTransform.localScale = new Vector3(-1f, 1f, 1f) * 50f;

// 2. 创建渲染纹理
renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);

// 3. 创建材质
videoMaterial = new Material(Shader.Find("Unlit/Texture"))
{
    mainTexture = renderTexture
};

// 4. 应用材质
skySphere.GetComponent<Renderer>().material = videoMaterial;

// 5. 更新纹理
void Update()
{
    if (playbackService.CurrentTexture != null)
    {
        Graphics.Blit(playbackService.CurrentTexture, renderTexture);
    }
}
```

### 3. Android 权限处理

```csharp
// 1. 检查权限状态
bool hasPermission = localFileManager.HasMoviesScanPermission();

// 2. 请求权限
localFileManager.RequestReadableMediaPermission();

// 3. 监听权限结果
while (localFileManager.IsPermissionRequestInFlight())
{
    yield return null;
}

// 4. 处理永久拒绝
if (localFileManager.WasLastPermissionRequestDeniedAndDontAskAgain())
{
    localFileManager.OpenAppPermissionSettings();
}
```

### 4. WebDAV 文件下载

```csharp
// 1. 连接服务器
bool connected = await webDAVManager.Connect(serverUrl, username, password);

// 2. 列出文件
List<VideoFile> files = await webDAVManager.ListFiles("/Videos");

// 3. 下载文件
bool success = await webDAVManager.DownloadFile(
    remotePath, 
    localPath, 
    progress => Debug.Log($"Progress: {progress}%")
);
```

---

## 平台适配

### Android 平台

#### 权限配置

**AndroidManifest.xml** (已配置):
```xml
<!-- Android 10-12 -->
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />

<!-- Android 13+ -->
<uses-permission android:name="android.permission.READ_MEDIA_VIDEO" />

<!-- Android 14+ 部分媒体权限 -->
<uses-permission android:name="android.permission.READ_MEDIA_VISUAL_USER_SELECTED" />
```

#### SAF 文件选择器

**插件结构**：
```
SAFPicker.androidlib/
├── src/main/
│   ├── java/com/vrplayer/saf/
│   │   ├── SafPickerBridge.java
│   │   └── SafPickerProxyActivity.java
│   └── AndroidManifest.xml
└── project.properties
```

**调用方式**：
```csharp
// C# 端调用
AndroidJavaClass bridge = new AndroidJavaClass("com.vrplayer.saf.SafPickerBridge");
bridge.CallStatic("launchVideoPicker", gameObject.name, "OnAndroidVideoPickerResult");

// 接收回调
public void OnAndroidVideoPickerResult(string payload)
{
    AndroidPickerResult result = JsonUtility.FromJson<AndroidPickerResult>(payload);
    // 处理选择结果
}
```

#### MediaStore 查询

```csharp
// 查询 Movies 目录下的视频
AndroidJavaObject cursor = resolver.Call<AndroidJavaObject>(
    "query",
    externalContentUri,
    projection,
    "relative_path=? OR relative_path=?",
    new[] { "Movies", "Movies/" },
    "date_added DESC"
);
```

### iOS 平台

**待实现功能**：
- PHPickerViewController 文件选择
- 照片库权限请求
- iCloud 文档访问

### Windows 平台

**文件选择**：
```csharp
#if UNITY_EDITOR
string path = EditorUtility.OpenFilePanel("Select video file", "", "mp4,mkv,mov");
#endif
```

---

## 构建与部署

### 本地构建

#### Android APK

1. **Unity Editor 构建**：
   ```
   File → Build Settings
   Platform: Android
   Run In Background: ✓
   Orientation: Landscape Left
   Build: 构建
   ```

2. **命令行构建**：
   ```bash
   Unity.exe -quit -batchmode \
     -projectPath c:/code/vrplayer/unity_vr_player \
     -executeMethod BuildAndroid.PerformBuild \
     -logFile unity-build.log
   ```

### CI/CD 构建

#### GitHub Actions

**触发条件**：
- 推送到 `master` 分支
- 推送 `v*` 标签
- 手动触发 `workflow_dispatch`

**必需 Secrets**：
```
UNITY_LICENSE          # Unity 许可证
UNITY_EMAIL           # Unity 账号邮箱
UNITY_PASSWORD        # Unity 账号密码
ANDROID_KEYSTORE_BASE64  # 签名密钥库 (base64)
ANDROID_KEYSTORE_PASS    # 密钥库密码
ANDROID_KEYALIAS_NAME    # 密钥别名
ANDROID_KEYALIAS_PASS    # 密钥密码
```

**构建产物**：
- APK: `unity_vr_player/builds/Android/*.apk`
- 构建日志: `unity-build.log`

#### 构建脚本配置

```csharp
// BuildAndroid.cs 关键配置
private static void ApplyBuildMetadata()
{
    // 从环境变量读取版本信息
    int versionCode = ResolveVersionCode();
    string versionName = ResolveVersionName(versionCode);
    
    PlayerSettings.Android.bundleVersionCode = versionCode;
    PlayerSettings.bundleVersion = versionName;
}
```

**版本号策略**：
- `versionCode`: 从环境变量 `GITHUB_RUN_NUMBER` 或时间戳生成
- `versionName`: 从环境变量 `UNITY_VERSION_NAME` 或自动生成

---

## API 文档

### VideoFile

视频文件实体。

```csharp
[Serializable]
public class VideoFile
{
    public string name;        // 文件名
    public string path;        // 文件路径
    public string url;         // 访问 URL
    public bool is360;         // 是否为 360° 视频
    public long size;          // 文件大小 (字节)
    public string localPath;   // 本地缓存路径
}
```

### PlaybackState

播放状态枚举。

```csharp
public enum PlaybackState
{
    Idle,        // 空闲
    Preparing,   // 准备中
    Ready,       // 准备完成
    Playing,     // 播放中
    Paused,      // 已暂停
    Error        // 错误
}
```

### PlaybackError

播放错误信息。

```csharp
[Serializable]
public struct PlaybackError
{
    public PlaybackErrorCode code;    // 错误码
    public string message;            // 错误消息
    public string source;             // 错误源
    
    public bool HasError { get; }     // 是否有错误
}
```

### PlaybackErrorCode

错误码枚举。

```csharp
public enum PlaybackErrorCode
{
    None,               // 无错误
    InvalidSource,      // 无效源
    FileNotFound,       // 文件未找到
    UnsupportedFormat,  // 不支持的格式
    PrepareTimeout,     // 准备超时
    DecoderFailure,     // 解码失败
    PermissionDenied,   // 权限拒绝
    Unknown             // 未知错误
}
```

---

## 开发规范

### 命名约定

**类名**: PascalCase
```csharp
public class VideoBrowserUI : MonoBehaviour { }
```

**方法名**: PascalCase
```csharp
public void PlayVideo(string path) { }
```

**私有字段**: camelCase with underscore prefix
```csharp
private string currentSource;
private bool isPlaying;
```

**常量**: PascalCase or UPPER_CASE
```csharp
private const int MaxVisibleItems = 60;
private const string PickedVideosPrefsKey = "local_picked_videos_v1";
```

### 代码风格

**使用区域划分**：
```csharp
#region Private Fields
private int count;
#endregion

#region Public Methods
public void Play() { }
#endregion

#region Private Methods
private void UpdateState() { }
#endregion
```

**注释规范**：
```csharp
/// <summary>
/// 视频播放控制器，管理播放状态和交互。
/// </summary>
public class VideoPlayer : MonoBehaviour
{
    /// <summary>
    /// 播放指定视频。
    /// </summary>
    /// <param name="path">视频文件路径</param>
    public void PlayVideo(string path) { }
}
```

### 性能优化建议

1. **避免频繁的 GameObject.Find**：
   ```csharp
   // ✓ 在 Start/ Awake 中缓存引用
   private VRVideoPlayer player;
   private void Start()
   {
       player = FindObjectOfType<VRVideoPlayer>();
   }
   
   // ✗ 避免在 Update 中查找
   private void Update()
   {
       FindObjectOfType<VRVideoPlayer>(); // 不推荐
   }
   ```

2. **使用对象池**：
   ```csharp
   private static readonly List<RaycastResult> UIRaycastResults = new List<RaycastResult>(8);
   ```

3. **缓存组件引用**：
   ```csharp
   private Transform cachedTransform;
   private void Awake()
   {
       cachedTransform = transform;
   }
   ```

4. **避免字符串拼接**：
   ```csharp
   // ✓ 使用字符串插值或格式化
   string message = $"Playing: {videoName}";
   
   // ✗ 避免频繁拼接
   string message = "Playing: " + videoName;
   ```

---

## 常见问题

### 1. Android 权限问题

**Q: 为什么 Android 14 上无法扫描 Movies 目录？**

A: Android 14 引入了部分媒体权限，需要单独请求：
```csharp
// 检查并请求 READ_MEDIA_VISUAL_USER_SELECTED 权限
if (sdkInt >= 34 && !HasMoviesDirectoryPermission())
{
    yield return RequestSinglePermission(AndroidPermissionReadMediaVisualUserSelected);
}
```

**Q: 用户拒绝权限后如何处理？**

A: 检测永久拒绝并引导用户到设置页面：
```csharp
if (localFileManager.WasLastPermissionRequestDeniedAndDontAskAgain())
{
    localFileManager.OpenAppPermissionSettings();
}
```

### 2. 视频播放问题

**Q: 视频播放失败，显示 "Prepare timeout"？**

A: 可能原因：
- 视频文件过大，需要增加超时时间
- 视频格式不支持
- 存储权限未授予

解决方案：
```csharp
// 增加超时时间
[SerializeField, Range(5f, 60f)] private float prepareTimeoutSeconds = 30f;
```

**Q: WebDAV 下载失败？**

A: 检查以下项：
- 服务器地址是否正确（需要完整 URL）
- 认证信息是否正确
- 网络连接是否稳定

### 3. VR 渲染问题

**Q: VR 视频显示镜像或扭曲？**

A: 检查 SkySphere 的 Scale：
```csharp
// 正确的 VR 球体缩放（X 轴为负）
skySphereTransform.localScale = new Vector3(-1f, 1f, 1f) * 50f;
```

**Q: 视角旋转不平滑？**

A: 调整平滑因子：
```csharp
[SerializeField, Range(0.01f, 1f)] private float smoothingFactor = 0.1f;
// 值越小越平滑，但延迟越大
```

### 4. 构建问题

**Q: CI 构建失败，提示 "No enabled scenes"？**

A: 确保 `Build Settings` 中已添加场景，或让构建脚本自动创建：
```csharp
// BuildAndroid.cs 会自动创建默认场景
private static void EnsureBuildSceneExists()
{
    if (EditorBuildSettings.scenes.Length > 0) return;
    
    // 创建默认场景...
}
```

**Q: 如何自定义构建版本号？**

A: 通过环境变量传递：
```bash
export UNITY_VERSION_CODE=123
export UNITY_VERSION_NAME="1.2.3"
Unity.exe -executeMethod BuildAndroid.PerformBuild
```

### 5. 性能问题

**Q: 播放高分辨率视频卡顿？**

A: 优化建议：
- 降低 `renderTexture` 分辨率
- 使用硬件解码（默认启用）
- 减少同时播放的视频数量
- 使用对象池管理 UI 元素

**Q: 应用内存占用过高？**

A: 解决方案：
- 及时释放未使用的资源
- 限制缓存大小
- 使用 `Resources.UnloadUnusedAssets()`

---

## 附录

### 项目里程碑

- **Milestone A**: 构建链路打通 ✓
- **Milestone B**: 本地播放 MVP ✓
- **Milestone C**: WebDAV MVP ✓
- **Milestone D**: 发布就绪 (进行中)

### 相关文档

- [README.md](./README.md) - 项目概述
- [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md) - 实施计划
- [REQUIREMENTS_TRACKER.md](./REQUIREMENTS_TRACKER.md) - 需求追踪
- [DETAILED_PLAN.md](./DETAILED_PLAN.md) - 详细计划
- [GIT_PROXY_SETUP.md](../GIT_PROXY_SETUP.md) - Git 代理配置

### 许可证

MIT License

---

**文档维护者**: VR Player Team  
**最后更新**: 2026-03-23  
**文档版本**: 1.0.0
