# Android 15 视频选择问题修复指南

## 问题描述

在 Android 15 设备上安装 APK 后，无法选择视频播放。

## 根本原因

### 1. 缺少主 AndroidManifest.xml

**问题：** 项目中缺少主 `AndroidManifest.xml` 文件，导致 Android 系统无法正确识别应用所需的权限。

**影响：**
- Unity 会自动生成一个默认的 AndroidManifest，但不会包含 Android 13+ 所需的新媒体权限
- 应用在运行时请求权限时，系统会拒绝，因为清单文件中没有声明这些权限
- 导致 SAF 文件选择器无法正常工作

### 2. Android 13-15 的权限模型变化

Android 引入了分区存储（Scoped Storage）和新的媒体权限模型：

- **Android 10 (API 29)**: 引入分区存储
- **Android 11 (API 30)**: 强制分区存储
- **Android 13 (API 33)**: 引入细分媒体权限（READ_MEDIA_VIDEO, READ_MEDIA_IMAGES, READ_MEDIA_AUDIO）
- **Android 14 (API 34)**: 引入 READ_MEDIA_VISUAL_USER_SELECTED 权限（用户选择的照片和视频）
- **Android 15 (API 35)**: 进一步强化权限控制

### 3. 权限请求逻辑不够完善

**原问题：**
```csharp
// Android 14+ 只在检测到没有 Movies 权限时才请求部分权限
if (sdkInt >= 34 && !HasMoviesDirectoryPermission())
{
    yield return RequestSinglePermission(AndroidPermissionReadMediaVisualUserSelected);
}
```

**问题：** 如果用户拒绝了 READ_MEDIA_VIDEO，代码不会主动请求 READ_MEDIA_VISUAL_USER_SELECTED。

## 解决方案

### 1. 创建主 AndroidManifest.xml

已在 `Assets/Plugins/Android/AndroidManifest.xml` 创建，包含：

```xml
<!-- Android 13+ 视频媒体权限 -->
<uses-permission android:name="android.permission.READ_MEDIA_VIDEO" />

<!-- Android 14+ 用户选择的媒体权限 -->
<uses-permission android:name="android.permission.READ_MEDIA_VISUAL_USER_SELECTED" />

<!-- Android 12 及以下的存储权限 -->
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" />
```

**关键点：**
- `android:maxSdkVersion="32"` 确保旧权限只在 Android 12 及以下请求
- 新权限声明让系统知道应用需要访问视频文件
- 支持从文件管理器直接打开视频文件

### 2. 优化权限请求逻辑

修改了 `LocalFileManager.cs` 中的权限请求流程：

**新逻辑：**
```csharp
if (sdkInt >= 33)
{
    // 1. 先请求完整的视频媒体权限
    yield return RequestSinglePermission(AndroidPermissionReadMediaVideo);
    
    // 2. Android 14+ 如果被拒绝，请求部分访问权限
    if (sdkInt >= 34)
    {
        bool hasVideoPermission = Permission.HasUserAuthorizedPermission(AndroidPermissionReadMediaVideo);
        bool hasSelectedPermission = Permission.HasUserAuthorizedPermission(AndroidPermissionReadMediaVisualUserSelected);
        
        if (!hasVideoPermission && !hasSelectedPermission)
        {
            yield return RequestSinglePermission(AndroidPermissionReadMediaVisualUserSelected);
        }
    }
}
```

**改进：**
- 明确的两步权限请求流程
- 用户可以拒绝完整访问，选择部分访问
- 提供更好的用户体验

## 权限说明

### READ_MEDIA_VIDEO (Android 13+)
- 允许应用访问设备上的所有视频文件
- 用户可以选择"允许"或"拒绝"

### READ_MEDIA_VISUAL_USER_SELECTED (Android 14+)
- 允许用户选择特定的照片和视频
- 应用只能访问用户选择的文件
- 提供更细粒度的隐私控制

### 权限组合策略

| 权限组合 | 访问范围 |
|---------|---------|
| READ_MEDIA_VIDEO ✅ | 所有视频文件 |
| READ_MEDIA_VISUAL_USER_SELECTED ✅ | 用户选择的视频 |
| 两者都没有 ❌ | 无法访问任何视频 |

## 测试步骤

### 1. 重新构建 APK

```bash
# 在 Unity 中
# File -> Build Settings -> Build
```

### 2. 安装到 Android 15 设备

```bash
adb install -r YourApp.apk
```

### 3. 测试权限流程

**场景 A: 首次使用**
1. 打开应用
2. 点击 "Select Videos" 按钮
3. 应该会弹出系统文件选择器（SAF）
4. 选择视频文件
5. 视频应该出现在列表中

**场景 B: 请求完整权限**
1. 打开应用
2. 点击 "Scan Settings" 按钮
3. 系统应该请求 "允许访问视频" 权限
4. 选择 "允许"
5. 应用应该能自动扫描 Movies 目录

**场景 C: 部分权限**
1. 在权限对话框中选择 "选择照片和视频"
2. 在系统选择器中选择部分视频
3. 应用应该能访问这些选中的视频

### 4. 调试日志

查看 Unity 日志：
```bash
adb logcat -s Unity
```

关键日志信息：
- `Cache directory:` - 缓存目录初始化
- `Refreshed local videos: X` - 扫描到的视频数量
- `Failed to invoke Android picker bridge` - SAF 选择器调用失败
- `Picker returned error:` - 选择器返回错误

## 常见问题

### Q1: 仍然无法选择视频？

**检查：**
1. 确认 AndroidManifest.xml 在正确的位置
2. 重新构建并安装 APK
3. 卸载旧版本，重新安装
4. 检查设备设置 -> 应用 -> 权限

### Q2: 权限对话框不显示？

**原因：**
- 清单文件中缺少权限声明
- Unity 缓存问题

**解决：**
```bash
# 清理 Unity 缓存
# 删除 Library 文件夹
# 重新打开项目
```

### Q3: 可以选择视频，但列表为空？

**检查：**
1. 确认 SAFPicker 正常工作
2. 查看日志中的错误信息
3. 检查 VideoBrowserUI 的刷新逻辑

## 代码结构

```
Assets/Plugins/Android/
├── AndroidManifest.xml              # 主清单文件（新增）
├── MediaPermissions.aar             # 权限辅助库
└── SAFPicker.androidlib/            # SAF 文件选择器
    ├── AndroidManifest.xml          # 选择器 Activity 配置
    └── src/main/java/com/vrplayer/saf/
        ├── SafPickerBridge.java      # Unity 桥接类
        └── SafPickerProxyActivity.java # 选择器 Activity

Assets/Scripts/
├── LocalFileManager.cs              # 本地文件管理（已优化）
└── VideoBrowserUI.cs                # UI 控制器
```

## 权限最佳实践

### 1. 不要一次性请求所有权限
- 按需请求
- 解释为什么需要权限

### 2. 处理权限拒绝
- 提供替代方案（如 SAF 选择器）
- 引导用户到设置页面

### 3. 使用 SAF (Storage Access Framework)
- 无需存储权限
- 用户完全控制文件访问
- 适用于 Android 10+

### 4. 测试不同 Android 版本
- Android 10 (API 29)
- Android 11-12 (API 30-32)
- Android 13 (API 33)
- Android 14 (API 34)
- Android 15 (API 35)

## 相关文件

- **AndroidManifest.xml**: `Assets/Plugins/Android/AndroidManifest.xml`
- **LocalFileManager.cs**: `Assets/Scripts/LocalFileManager.cs` (已修改)
- **SafPickerProxyActivity.java**: `Assets/Plugins/Android/SAFPicker.androidlib/src/main/java/com/vrplayer/saf/SafPickerProxyActivity.java`
- **VideoBrowserUI.cs**: `Assets/Scripts/VideoBrowserUI.cs`

## 更新历史

- **2026-03-23**: 创建 AndroidManifest.xml，优化权限请求逻辑
- **2026-03-23**: 添加 Android 15 兼容性支持

## 参考资料

- [Android 存储权限指南](https://developer.android.com/training/data-storage/shared/media)
- [Storage Access Framework](https://developer.android.com/guide/topics/providers/document-provider)
- [Unity Android 权限](https://docs.unity3d.com/Manual/android-RequestUserAuthorization.html)
