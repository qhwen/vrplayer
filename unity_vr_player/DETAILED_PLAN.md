# Unity VR视频播放器 - 详细实施步骤

## 开发环境要求

### 软件工具
- **Unity Hub** 3.0.0+ （管理Unity版本）
- **Unity Editor** 2022.3 LTS
- **Visual Studio 2022** （Windows平台开发）
- **Xcode 15.0+** （iOS平台开发，Mac系统）
- **Android Studio** （Android平台插件开发）
- **Git** （版本控制）
- **VS Code** （代码编辑，可选）

### 硬件要求
- **Windows PC** 开发机：i5/Ryzen 5+，16GB RAM，GPU支持DX11
- **Mac** 开发机（iOS）：M1/M2 芯片，8GB RAM，macOS 12+
- **Android 设备**：Android 5.0+（API 21+），用于真机测试
- **iOS 设备**：iOS 11.0+，用于真机测试
- **VR设备**：Oculus Quest / Pico / Cardboard 眼镜（可选）

### 开发账号/服务
- **GitHub账号**：代码托管和版本控制
- **WebDAV测试服务**：Nextcloud / ownCloud 实例
- **测试云存储**：用于存储测试视频文件

---

## 实施步骤 - Week 1

### 第1天：开发环境搭建

#### 步骤 1.1：安装 Unity Hub 和 Editor
```bash
# Windows (下载Unity Hub)
1. 访问 https://unity.com/download
2. 下载 Unity Hub (Windows)
3. 运行安装程序，默认设置
4. Unity Hub → Installs → Install Editor
5. 选择 Unity 2022.3 LTS
6. 等待下载和安装（约2-3GB）
```

```bash
# macOS (下载Unity Hub)
1. 访问 https://unity.com/download
2. 下载 Unity Hub (Mac)
3. 安装 .dmg 镜像
4. Unity Hub → Installs → Install Editor
5. 选择 Unity 2022.3 LTS
6. 等待安装完成
```

**验收标准：**
- [ ] Unity Hub 能正常启动
- [ ] Unity 2022.3 LTS 已安装
- [ ] 能创建新项目

---

#### 步骤 1.2：创建项目目录
```bash
# 在工作目录创建项目
cd /path/to/your/workspace
mkdir -p unity_vr_player
cd unity_vr_player
```

---

#### 步骤 1.3：初始化 Git 仓库
```bash
# 初始化 Git
git init

# 创建 .gitignore
cat > .gitignore << 'EOF'
# Unity 生成文件
[Ll]/
[Ll]/
Temp/
Obj/
Library/
UserSettings/
*.csproj
*.sln
EOF

# 提交初始版本
git add .
git commit -m "Initial commit: Unity VR Video Player project"
```

---

#### 步骤 1.4：创建 Unity 项目
```
操作步骤：

1. 启动 Unity Hub
2. 点击 "New Project"
3. 选择项目模板：
   - Template: 3D (Core)
4. 填写项目信息：
   - Project Name: VRVideoPlayer
   - Location: (指向unity_vr_player目录)
5. 点击 "Create Project"
```

**项目结构验证：**
```
VRVideoPlayer/
├── Assets/             # 资源文件夹
├── ProjectSettings/    # 项目设置
├── Packages/          # 包管理器
└── Library/           # 自动生成的缓存目录
```

**验收标准：**
- [ ] 项目能在 Unity Editor 中打开
- [ ] 项目结构正确（Assets、ProjectSettings、Library）
- [ ] 能编译脚本（没有错误）

---

#### 步骤 1.5：配置项目设置
```
在 Unity 中操作：

Edit → Project Settings → 配置以下设置：

1. Player → XR Plug-in Management
   - 勾选 "OpenXR"
   - 点击 "Install Package"

2. Player → Other Settings
   - Color Space: Linear
   - Rendering Path: Forward
   - Auto Graphics API: Vulkan (Windows) / Metal (Mac)

3. Player → Resolution and Presentation
   - Fullscreen Mode: Fullscreen Window
   - Run In Background: Enabled
   - Default Screen Width: 1920
   - Default Screen Height: 1080

4. Editor → Project Settings → Editor
   - Asset Serialization: Force Text
   - Version Control Mode: Visible Meta Files

5. 保存设置（File → Save Project）
```

**验收标准：**
- [ ] OpenXR 插件已安装
- [ ] 项目设置保存成功
- [ ] Player Settings 配置正确

---

### 第2天：创建基础场景

#### 步骤 2.1：创建主场景
```
在 Unity 中操作：

1. 右键 Assets 文件夹
2. Create → Folder → Scenes
3. 重命名为 "Scenes"

4. 双击打开 Scenes 文件夹
5. 右键 → Create → Scene
6. 命名为 "VideoPlayerScene"
7. 保存场景（Ctrl+S）
```

**场景内容：**
- 默认创建的 Main Camera
- 默认创建的 Directional Light
- 空的 GameObject 作为场景根节点

---

#### 步骤 2.2：配置摄像机
```
选中 Main Camera → Inspector 中设置：

1. Transform 组件
   - Position: (0, 0, 0)
   - Rotation: (0, 0, 0)
   - Scale: (1, 1, 1)

2. Camera 组件
   - Clear Flags: Solid Color
   - Background: 黑色 (0, 0, 0, 1)
   - Near Clip Plane: Near
   - Far Clip Plane: 1000
   - Field of View: 60

3. 添加 XR Origin 组件
   - 在 Camera 上右键 → XR → Add XR Origin
   - 位置设置为 (0, 1.6, 0) 模拟人眼高度
```

**验收标准：**
- [ ] 场景保存成功
- [ ] 摄像机配置正确
- [ ] 场景能运行（按Play按钮）

---

#### 步骤 2.3：创建光照
```
操作步骤：

1. 在 Hierarchy 右键
2. Light → Directional Light
3. 放置在合适位置 (2, 5, -2)
4. Inspector 中设置：
   - Intensity: 1.0
   - Color: 白色
   - Shadow Type: Soft Shadows
   - Shadow Strength: 0.5
5. 保存场景
```

**验收标准：**
- [ ] 光照正常工作
- [ ] 阴影效果可见
- [ ] 场景没有错误

---

### 第3天：创建核心脚本

#### 步骤 3.1：创建 Scripts 文件夹
```
在 Unity 中操作：

1. 右键 Assets 文件夹
2. Create → Folder
3. 命名为 "Scripts"
4. 验证文件夹创建成功
```

---

#### 步骤 3.2：创建 VRVideoPlayer.cs
```
操作步骤：

1. 右键 Scripts 文件夹
2. Create → C# Script
3. 命名为 "VRVideoPlayer"
4. 双击打开脚本（VS Code会自动打开）
```

**粘贴以下完整代码到 VRVideoPlayer.cs：**
（参考之前创建的完整脚本，包含所有必需功能）

**代码保存：**
```
1. 保存脚本（Ctrl+S）
2. 等待 Unity 编译完成
3. 在 Console 中检查是否有编译错误
```

**验收标准：**
- [ ] 脚本编译成功（没有错误）
- [ ] 脚本功能完整（播放/暂停/音量/VR控制）
- [ ] 注释清晰

---

#### 步骤 3.3：创建其他核心脚本
```
依次创建以下脚本（重复步骤 3.2）：

1. VRUIManager.cs
2. WebDAVManager.cs
3. LocalFileManager.cs
4. SceneLoader.cs
```

**脚本清单：**
- [ ] VRVideoPlayer.cs - 视频播放核心
- [ ] VRUIManager.cs - UI 管理
- [ ] WebDAVManager.cs - WebDAV 集成
- [ ] LocalFileManager.cs - 本地文件管理
- [ ] SceneLoader.cs - 场景加载

---

### 第4天：创建预制体和资源

#### 步骤 4.1：创建 SkySphere 预制体
```
操作步骤：

1. 右键 Assets
2. Create → Folder → Prefabs
3. 命名为 "Prefabs"

4. 在 Prefabs 文件夹中：
   - 右键 → Create → 3D Object → Sphere
   - 命名为 "SkySphere"
5. Inspector 中设置 Sphere：
   - Radius: 50
   - Position: (0, 0, 0)
   - Rotation: (0, 0, 0)
   - Scale: (1, 1, 1)

6. 拖拽 SkySphere 到 Prefabs 文件夹
7. 保存（Ctrl+S）
```

**验证预制体：**
- [ ] SkySphere 创建成功
- [ ] 半径设置正确（50）
- [ ] 能作为预制体使用

---

#### 步骤 4.2：创建视频材质
```
操作步骤：

1. 右键 Assets
2. Create → Folder → Materials
3. 命名为 "Materials"

4. 在 Materials 文件夹中：
   - 右键 → Create → Material
   - 命名为 "VideoMaterial"
5. Inspector 中设置：
   - Shader: Unlit/Texture（不使用光照，直接显示纹理）
   - Maps: 无（清空所有Map）

6. 保存材质
```

**验证材质：**
- [ ] VideoMaterial 创建成功
- [ ] Shader 设置正确（Unlit/Texture）

---

### 第5-6天：整合脚本到场景

#### 步骤 5.1：添加 VRVideoPlayer 组件到场景
```
操作步骤：

1. 打开 VideoPlayerScene
2. 右键 Hierarchy → Create Empty
3. 命名为 "VRPlayer"
4. 选中 VRPlayer
5. Inspector → Add Component → VR Video Player (Script)
6. Inspector 中配置：
   - Default Video Path: 留空（测试时手动设置）
   - Sky Sphere Prefab: 拖入刚才创建的 SkySphere
   - Enable Head Tracking: 勾选
   - Rotation Sensitivity: 0.5
   - Smoothing Factor: 0.1

7. 保存场景
```

---

#### 步骤 5.2：创建和配置 RenderTexture
```
在 VRVideoPlayer 脚本的 Inspector 中操作：

1. 选中 VRPlayer GameObject
2. Inspector → 查看组件列表
3. 确认 VideoPlayer 组件已添加（Unity自动添加）
```

**自动化的 RenderTexture 创建：**
VRVideoPlayer.cs 脚本中会自动创建：
```csharp
renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
```

**验证：**
- [ ] VideoPlayer 组件存在
- [ ] SkySphere Prefab 已分配
- [ ] 场景能运行

---

#### 步骤 6.1：创建 EventSystem
```
操作步骤：

1. 打开 VideoPlayerScene
2. Hierarchy 右键 → UI → Event System
3. 场景中会出现 "EventSystem" GameObject
```

**作用：**
- 处理 UI 交互事件
- 支持鼠标、触摸、VR手柄输入

---

### 第7天：测试基础功能

#### 步骤 7.1：测试视频播放
```
操作步骤：

1. 准备测试视频：
   - 将一个 MP4 视频文件放到 Assets/Resources/
   - 或使用本地路径

2. 在 Inspector 中设置视频路径：
   - 选中 VRPlayer
   - Default Video Path: 拖入视频文件或填写完整路径

3. 点击 Unity 的 Play 按钮

4. 观察效果：
   - 视频是否播放
   - 球体是否显示视频
   - 音频是否正常

5. 测试控制：
   - 按空格键：暂停/恢复
   - 测试是否有效

6. 查看 Console 日志：
   - 检查是否有错误或警告
```

**验收标准：**
- [ ] 视频能正常播放
- [ ] 360°球体正确显示视频
- [ ] 暂停/播放功能正常
- [ ] 没有Console错误

---

#### 步骤 7.2：测试 VR 旋转
```
测试手势控制：

1. 运行场景
2. 按住鼠标左键拖动：
   - 左右拖动：观察球体旋转（偏航角）
   - 上下拖动：观察球体旋转（俯仰角）
3. 验证旋转范围：
   - 上下角度限制在 -90° ~ 90°
   - 左右旋转无限制（360°）

验收标准：
- [ ] 鼠标拖动能控制VR视角
- [ ] 旋转平滑（无明显抖动）
- [ ] 角度范围合理
```

---

### 第8-9天：添加 WebDAV 和本地文件管理

#### 步骤 8.1：创建 WebDAV 管理器 GameObject
```
操作步骤：

1. 打开 VideoPlayerScene
2. 右键 Hierarchy → Create Empty
3. 命名为 "WebDAVManager"
4. 添加 WebDAVManager.cs 脚本：
   - Inspector → Add Component → Web DAV Manager
5. 配置参数（如果需要）：
   - Server URL: （可选，用于测试）
   - Username: （可选）
   - Password: （可选）
6. 保存场景
```

---

#### 步骤 8.2：创建本地文件管理器 GameObject
```
操作步骤：

1. 右键 Hierarchy → Create Empty
2. 命名为 "LocalFileManager"
3. 添加 LocalFileManager.cs 脚本
4. Inspector → Add Component → Local File Manager
5. 保存场景
```

---

#### 步骤 9.1：创建设置场景
```
操作步骤：

1. File → New Scene
2. 命名为 "SettingsScene"
3. 创建基础UI布局：
   - Canvas (Screen Space - Overlay)
   - InputField（服务器地址、用户名、密码）
   - Button（连接、保存）
4. 保存场景
5. 添加到 Build Settings：
   - File → Build Settings → Scenes in Build
   - 拖入 SettingsScene
```

---

### 第10天：Git提交和备份

#### 步骤 10.1：提交到远程仓库
```bash
# 查看修改
git status

# 添加所有文件
git add .

# 提交
git commit -m "feat: core video player with WebDAV and local file support"

# 推送到远程（如果已配置远程仓库）
git remote add origin https://github.com/your-repo.git
git push -u origin main
```

---

#### 步骤 10.2：创建里程碑分支（可选）
```bash
# 创建分支用于阶段开发
git checkout -b feature/mvp
git checkout -b feature/webdav
git checkout -b feature/windows-build
```

---

## 常见问题排查

### Unity Editor 无法打开项目
```
问题：双击 .unity 文件无法打开
解决方案：
1. 检查 Unity 版本是否正确（2022.3 LTS）
2. 重启 Unity Hub
3. 尝试 File → Open Project
4. 检查 ProjectSettings 文件是否完整
```

### 脚本编译错误
```
问题：Console 显示红色错误
解决方案：
1. 检查命名空间（using UnityEngine;）
2. 检查语法错误（分号、括号）
3. 查看错误详细信息
4. 修正后保存脚本
```

### 视频无法播放
```
问题：视频不显示或报错
解决方案：
1. 检查视频路径是否正确
2. 检查视频格式是否支持（MP4/MKV/MOV）
3. 查看Console的错误日志
4. 尝试使用不同的视频文件
```

### VR球体显示不正确
```
问题：视频镜像或扭曲
解决方案：
1. 检查 Sphere Scale 是否为 (-1, 1, 1)
2. 检查 RenderTexture 大小是否匹配视频
3. 检查 Shader 是否为 Unlit/Texture
```

---

## 下一步预览（Week 2-6）

### Week 2：UI系统和播放历史
- 创建主菜单场景
- 实现播放列表UI
- 添加播放历史记录
- 实现设置持久化

### Week 3：OpenXR 头部追踪
- 集成 OpenXR 插件
- 实现真实的VR头部输入
- 支持 VR手柄控制

### Week 4：WebDAV 完整实现
- 完善WebDAV PROPFIND
- 实现文件分页
- 添加断点续传

### Week 5：平台构建
- 构建Windows桌面版
- 构建iOS版本（真机测试）
- 构建Android版本

### Week 6：性能优化和发布
- 性能分析和优化
- 修复所有已知Bug
- 准备发布版本

---

## 检查清单

### 每日检查
- [ ] Unity项目能正常打开和运行
- [ ] 脚本编译无错误
- [ ] 场景保存成功
- [ ] 提交到Git

### 每周检查
- [ ] 功能开发按计划进行
- [ ] 测试通过验收标准
- [ ] 文档更新

### 每个里程碑检查
- [ ] Week 1结束：基础播放器工作
- [ ] Week 2结束：UI系统完成
- [ ] Week 3结束：WebDAV集成
- [ ] Week 4结束：跨平台构建
- [ ] Week 5结束：产品发布就绪

---

## 环境搭建完成后的目录结构

```
unity_vr_player/
├── Assets/
│   ├── Scenes/
│   │   ├── VideoPlayerScene.unity
│   │   ├── SettingsScene.unity
│   │   └── MainMenuScene.unity
│   ├── Scripts/
│   │   ├── VRVideoPlayer.cs
│   │   ├── VRUIManager.cs
│   │   ├── WebDAVManager.cs
│   │   ├── LocalFileManager.cs
│   │   └── SceneLoader.cs
│   ├── Prefabs/
│   │   ├── SkySphere.prefab
│   │   └── ControlPanel.prefab
│   ├── Materials/
│   │   └── VideoMaterial.mat
│   └── Resources/
│       └── (可选）测试视频文件
├── ProjectSettings/
│   ├── ProjectVersion.txt
│   ├── ProjectSettings.asset
│   └── （其他配置文件）
├── Library/
│   └── （自动生成，不提交到Git）
├── .gitignore
├── README.md
└── IMPLEMENTATION_PLAN.md
```

---

*详细计划版本：1.0*
*创建日期：2026-02-07*
*预计开发周期：5-6周*
