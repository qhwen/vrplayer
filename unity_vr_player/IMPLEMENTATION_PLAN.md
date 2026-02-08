# Unity VR视频播放器 - 实施计划（任务拆分版）

## 1. 计划目标

本计划采用“按任务内容拆分”的方式推进，不按天拆分。

核心目标：
1. 在**本机不安装 Unity**前提下，通过云端流水线稳定产出 Android APK。
2. 先交付 Android MVP（本地播放 + WebDAV 下载播放 + 基础 VR 交互）。
3. 建立可扩展架构，为后续 iOS/Windows 扩展保留边界。

## 2. 约束与原则

### 2.1 约束
- 本地开发机不安装 Unity Editor。
- 构建环境在云端（优先 Unity Build Automation，备选 GitHub Actions + Unity License）。
- 当前仓库不是完整 Unity 工程，需要先补齐工程骨架。

### 2.2 实施原则
- 先跑通构建链路，再堆功能。
- 先 Android MVP，再扩平台。
- 业务分层，避免 UI 直接耦合底层协议。
- 每个任务包必须有“交付物 + 验收标准”。

## 3. MVP 范围定义

### 3.1 包含范围（In Scope）
- Android APK 云端构建、签名、产物归档。
- 本地视频播放（MP4 优先）。
- WebDAV 文件浏览、下载到本地缓存、播放缓存文件。
- 360 球幕渲染 + 基础视角控制（拖拽/陀螺仪二选一的可用实现）。
- 基础播放 UI（播放/暂停、进度、时长、错误提示）。

### 3.2 不包含范围（Out of Scope）
- Quest/Pico 深度 SDK 适配。
- 多平台一次性交付（iOS/Windows 在 MVP 后单独任务包）。
- 高级能力（字幕、多音轨、倍速、播放历史同步）。

## 4. 目标架构（MVP）

建议分层：

1. `Presentation`：UI 与交互编排。  
2. `Application`：播放用例、下载用例、状态流转。  
3. `Domain`：接口与实体（`IVideoSource`、`VideoItem`、`PlaybackState`）。  
4. `Infrastructure`：Local/WebDAV/Android 插件/文件系统实现。  
5. `Platform`：Android 权限、文件选择、构建与签名配置。  

关键接口（MVP 必需）：
- `IVideoSource.ListAsync(path)`
- `IVideoSource.DownloadAsync(remote, local, progress)`
- `IPlaybackService.Open(uri)` / `Play()` / `Pause()` / `Seek()`
- `ICacheService.GetPath(key)` / `Exists(key)` / `Evict()`

## 5. 任务包拆分（Workstreams）

## WS-01 工程基线与仓库重构

目标：把当前仓库从“文档+脚本草稿”升级为可编译 Unity 工程骨架。

任务清单：
- [ ] 建立标准 Unity 目录：`Assets/`、`Packages/`、`ProjectSettings/`。
- [ ] 脚本迁移至 `Assets/Scripts/`，按层次分目录。
- [ ] 新增 `Assets/Editor/BuildAndroid.cs` 作为唯一构建入口。
- [ ] 清理/修复当前脚本中的编译阻塞点（协程、空指针判断、API 拼写、括号结构等）。
- [ ] 增加基础 `.gitignore`（Unity 推荐模板）。

交付物：
- 可被 Unity CLI 打开的工程结构。
- 编译日志中无 C# 编译错误。

验收标准：
- `-batchmode -quit -projectPath ...` 可执行并完成脚本编译阶段。

依赖：无（最优先）。

---

## WS-02 云端构建链路（不安装本机 Unity）

目标：在云端稳定输出 APK。

实施策略：
- 主路径：Unity Build Automation（UBA）。
- 备选路径：GitHub Actions + Unity Builder（需要 License 和 Android 签名密钥）。

任务清单：
- [ ] 修正工作流触发分支与当前仓库一致（`master`/tag/manual）。
- [ ] 替换不可用的“手工拼工程”流程为“真实工程 + Unity 构建入口脚本”。
- [ ] 配置 Android keystore secrets（base64、alias、password）。
- [ ] 输出构建日志与 APK Artifact。
- [ ] 增加失败分类（License、SDK、编译、签名）与提示。

交付物：
- 可复用构建流水线配置（YAML 或 UBA 配置）。
- 成功产出的 debug/release APK。

验收标准：
- 连续 3 次触发均成功产出 APK。
- 产物可安装到 Android 真机。

依赖：WS-01。

---

## WS-03 播放内核（Playback Core）

目标：构建稳定播放状态机，避免 UI 和底层播放器直接耦合。

任务清单：
- [ ] 封装 `IPlaybackService`（Open/Play/Pause/Stop/Seek/Volume）。
- [ ] 建立播放状态机（Idle/Preparing/Ready/Playing/Paused/Error）。
- [ ] 实现事件回调（进度、时长、错误、缓冲）。
- [ ] 统一异常转换（文件不存在、格式不支持、解码失败）。

交付物：
- 播放服务实现与状态模型。
- 可被 UI 订阅的状态输出。

验收标准：
- 本地 MP4 可完整播放，暂停/继续/跳转可用。
- 播放失败可返回明确错误码。

依赖：WS-01。

---

## WS-04 VR 渲染与交互

目标：完成可用的 360 播放体验。

任务清单：
- [ ] Sphere + 材质 + RenderTexture 映射链路。
- [ ] 视角控制（优先触控拖拽，陀螺仪为增强项）。
- [ ] 旋转范围限制与平滑插值。
- [ ] 场景初始化与资源释放（防止内存泄漏）。

交付物：
- 可在 Android 设备中查看 360 内容的场景。

验收标准：
- 1080p 360 视频播放时无明显撕裂。
- 手势旋转可控，无严重抖动或反向。

依赖：WS-03。

---

## WS-05 UI 与应用编排层

目标：提供可用的播放器界面和用户反馈。

任务清单：
- [ ] 播放控制 UI（播放/暂停、进度条、时长、返回）。
- [ ] 状态提示 UI（加载中、错误、下载进度）。
- [ ] UI 仅依赖应用层接口，不直连协议实现。
- [ ] 建立最小导航（播放器页 / 文件页 / 设置页）。

交付物：
- MVP UI 场景与交互逻辑。

验收标准：
- 用户可独立完成“选视频 -> 播放 -> 暂停/拖动进度 -> 退出”。

依赖：WS-03。

---

## WS-06 本地数据源与缓存服务

目标：本地文件与缓存能力可独立复用。

任务清单：
- [ ] 实现 `LocalVideoSource`（扫描、读取、过滤视频格式）。
- [ ] 实现 `CacheService`（路径管理、容量统计、清理策略）。
- [ ] 统一路径规范（Android 使用 `persistentDataPath`）。
- [ ] 建立缓存命名规则（hash 或安全文件名映射）。

交付物：
- 本地数据源与缓存服务实现。

验收标准：
- 本地文件列表可展示。
- 下载缓存文件可被播放器稳定打开。

依赖：WS-01、WS-03。

---

## WS-07 WebDAV 数据源

目标：实现与具体 WebDAV 服务解耦的远端数据能力。

任务清单：
- [ ] 实现 `WebDavVideoSource`，对齐 `IVideoSource`。
- [ ] PROPFIND XML 解析改为标准 XML 解析器（非字符串拼切）。
- [ ] Basic Auth 与连接测试。
- [ ] 下载进度回调与失败重试（MVP 一次重试即可）。
- [ ] Nextcloud/ownCloud 兼容测试。

交付物：
- WebDAV 文件浏览 + 下载 + 播放完整链路。

验收标准：
- 连接成功率稳定。
- 下载后文件校验通过并可播放。

依赖：WS-03、WS-06。

---

## WS-08 Android 平台适配

目标：补齐 Android 侧能力与权限模型。

任务清单：
- [ ] Manifest 权限最小化配置（按 Android 版本分支处理）。
- [ ] 文件选择方案：先固定目录，后接 SAF 插件。
- [ ] 横屏/沉浸模式设置。
- [ ] Release 签名配置与版本号策略（versionCode/versionName）。

交付物：
- 可安装、可运行、可授权的 Android 包。

验收标准：
- Android 10+ 与 13+ 设备均可完成播放主流程。

依赖：WS-02、WS-05、WS-06、WS-07。

---

## WS-09 质量保障与测试

目标：把“能跑”提升为“可持续交付”。

任务清单：
- [ ] 建立最小测试矩阵：格式、分辨率、网络条件、设备版本。
- [ ] 核心模块测试：WebDAV 解析、路径映射、状态机转换。
- [ ] 引入冒烟脚本（构建后自动做基础校验）。
- [ ] 统一日志规范（含错误码）。

交付物：
- 测试报告模板 + 每次构建附带结果。

验收标准：
- 核心链路回归可重复。
- 关键缺陷可定位到模块与错误码。

依赖：WS-03 之后持续进行。

---

## WS-10 发布与运维

目标：形成可复用发布流程和问题响应机制。

任务清单：
- [ ] 建立 release 流程（tag 触发、变更日志、产物归档）。
- [ ] 维护发布清单（已知限制、设备兼容性、安装说明）。
- [ ] 设立 issue 模板（Bug/构建失败/功能请求）。
- [ ] 输出运行手册（如何触发构建、如何替换签名、如何回滚）。

交付物：
- MVP 发布包 + 发布文档。

验收标准：
- 新成员可仅依赖文档复现一次完整发布。

依赖：WS-02~WS-09。

## 6. 任务执行顺序与里程碑（按内容）

### Milestone A：构建链路打通
- 包含：WS-01 + WS-02（最小可安装 APK）。
- 通过标准：云端稳定产出 APK。

### Milestone B：本地播放 MVP
- 包含：WS-03 + WS-04 + WS-05 + WS-06。
- 通过标准：本地视频可在 VR 场景播放与控制。

### Milestone C：WebDAV MVP
- 包含：WS-07 + WS-08。
- 通过标准：WebDAV 浏览、下载、缓存播放打通。

### Milestone D：发布就绪
- 包含：WS-09 + WS-10。
- 通过标准：有签名 release 包、测试报告和发布文档。

## 7. Definition of Done（DoD）

每个任务包完成必须同时满足：
1. 代码合入主分支并通过 CI。  
2. 有对应文档更新（配置或使用说明）。  
3. 有可验证的验收记录（日志、截图、测试结果）。  
4. 无阻塞级缺陷（崩溃、无法安装、主流程中断）。  

## 8. 主要风险与应对

1. Unity License/云构建配置失败。  
应对：先做最小场景构建验证，再引入业务代码。

2. Android 文件选择兼容性复杂。  
应对：MVP 先固定目录，SAF 作为后置增强。

3. WebDAV 服务实现差异大。  
应对：先锁 Nextcloud，再逐步扩 ownCloud/其他服务。

4. 性能问题（高码率/高分辨率）。  
应对：先 1080p 基线，后续再开高码率优化任务。

## 9. MVP 后续扩展任务包

1. iOS 构建与权限模型适配。  
2. Windows 桌面模式与输入适配。  
3. OpenXR 深度集成（头显/手柄）。  
4. 高级功能（字幕、倍速、播放历史、收藏）。

---

*计划版本：2.0（任务拆分版）*  
*更新日期：2026-02-08*  
*适用范围：Android MVP（本机不安装 Unity）*
