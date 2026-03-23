# Android SAF 文件选择器无法工作 - 完整修复

## 🚨 问题诊断

经过深入分析，发现以下**关键问题**导致 Android SAF 文件选择器无法工作：

### 问题 1: 缺少 build.gradle 文件（最关键）

**位置：** `Assets/Plugins/Android/SAFPicker.androidlib/`

**问题：** SAFPicker.androidlib 目录缺少 `build.gradle` 文件

**影响：** Unity 构建系统无法识别和编译 Java 代码，导致：
- `SafPickerBridge.class` 未打包到 APK
- `SafPickerProxyActivity.class` 未打包到 APK
- 点击 "Select Videos" 时 Unity 无法找到 Java 类
- `LaunchAndroidVideoPicker()` 失败并输出 "Failed to invoke Android picker bridge"

### 问题 2: AndroidManifest.xml 冲突

**位置：** `Assets/Plugins/Android/SAFPicker.androidlib/AndroidManifest.xml`

**问题：** SAFPicker.androidlib 的 AndroidManifest.xml 与主 AndroidManifest.xml 重复声明了 `SafPickerProxyActivity`

**影响：** 可能导致 Activity 注册冲突或构建警告

### 问题 3: project.properties 版本过旧

**位置：** `Assets/Plugins/Android/SAFPicker.androidlib/project.properties`

**问题：** `target=android-9` 太旧，不支持新的 Android SDK

**影响：** 可能导致编译错误或警告

---

## ✅ 已实施的修复

### 修复 1: 创建 build.gradle 文件

**新建文件：** `Assets/Plugins/Android/SAFPicker.androidlib/build.gradle`

**内容：**
```groovy
plugins {
    id 'com.android.library'
}

android {
    namespace 'com.vrplayer.saf'
    compileSdkVersion 34

    defaultConfig {
        minSdkVersion 24
        targetSdkVersion 34
        versionCode 1
        versionName "1.0"
    }

    buildTypes {
        release {
            minifyEnabled false
            proguardFiles getDefaultProguardFile('proguard-android-optimize.txt'), 'proguard-rules.pro'
        }
    }

    compileOptions {
        sourceCompatibility JavaVersion.VERSION_17
        targetCompatibility JavaVersion.VERSION_17
    }

    lintOptions {
        abortOnError false
    }
}

dependencies {
    implementation 'androidx.appcompat:appcompat:1.6.1'
}
```

**作用：**
- ✅ 定义 Android 库模块配置
- ✅ 指定编译 SDK 和目标 SDK 版本
- ✅ 配置 Java 17 兼容性
- ✅ 包含必要的 AndroidX 依赖

---

### 修复 2: 更新 AndroidManifest.xml

**修改文件：** `Assets/Plugins/Android/SAFPicker.androidlib/AndroidManifest.xml`

**修改前：**
```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.vrplayer.safpicker">

    <application>
        <activity
            android:name="com.vrplayer.saf.SafPickerProxyActivity"
            ... />
    </application>

</manifest>
```

**修改后：**
```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.vrplayer.safpicker">

    <!-- 这个库模块的清单应该为空，让主 AndroidManifest.xml 接管 activity 注册 -->

</manifest>
```

**作用：**
- ✅ 避免 Activity 重复声明
- ✅ 让主 AndroidManifest.xml 统一管理 Activity 注册

---

### 修复 3: 更新 project.properties

**修改文件：** `Assets/Plugins/Android/SAFPicker.androidlib/project.properties`

**修改前：**
```
target=android-9
android.library=true
```

**修改后：**
```
target=android-34
android.library=true
```

**作用：**
- ✅ 支持 Android 14 (API 34)
- ✅ 与 build.gradle 配置一致

---

### 修复 4: 创建 proguard-rules.pro

**新建文件：** `Assets/Plugins/Android/SAFPicker.androidlib/proguard-rules.pro`

**内容：**
```
# Keep SafPickerBridge class accessible from Unity
-keep class com.vrplayer.saf.SafPickerBridge { *; }

# Keep SafPickerProxyActivity class
-keep class com.vrplayer.saf.SafPickerProxyActivity { *; }

# Keep JSON classes
-keep class org.json.** { *; }
```

**作用：**
- ✅ 防止 ProGuard 混淆关键类
- ✅ 确保 Unity 能找到这些类

---

### 修复 5: 创建 gradle wrapper 配置

**新建文件：** `Assets/Plugins/Android/SAFPicker.androidlib/gradle/wrapper/gradle-wrapper.properties`

**内容：**
```
distributionBase=GRADLE_USER_HOME
distributionPath=wrapper/dists
distributionUrl=https\://services.gradle.org/distributions/gradle-8.4-bin.zip
networkTimeout=10000
validateDistributionUrl=true
zipStoreBase=GRADLE_USER_HOME
zipStorePath=wrapper/dists
```

**作用：**
- ✅ 提供 Gradle 构建所需的基础设施
- ✅ 指定 Gradle 8.4 版本

---

## 📋 修复文件清单

| 文件 | 状态 | 说明 |
|------|------|------|
| `SAFPicker.androidlib/build.gradle` | ✅ 新建 | Android 库模块构建配置 |
| `SAFPicker.androidlib/proguard-rules.pro` | ✅ 新建 | ProGuard 混淆规则 |
| `SAFPicker.androidlib/gradle/wrapper/gradle-wrapper.properties` | ✅ 新建 | Gradle wrapper 配置 |
| `SAFPicker.androidlib/AndroidManifest.xml` | ✅ 修改 | 移除重复的 Activity 声明 |
| `SAFPicker.androidlib/project.properties` | ✅ 修改 | 更新 target SDK 版本 |

---

## 🔍 技术背景

### Unity Android 插件结构

Unity Android 项目中，纯 Java/Kotlin 代码需要通过以下方式之一打包：

**方式 1: .androidlib 目录（推荐用于复杂项目）**
```
Assets/Plugins/Android/MyLibrary.androidlib/
├── AndroidManifest.xml
├── build.gradle          ← 需要此文件
├── project.properties    ← 需要配置正确
├── src/main/java/        ← Java 源代码
│   └── com/example/
│       └── MyClass.java
└── build/                ← Unity/Gradle 编译输出
```

**方式 2: JAR/AAR 文件（适用于简单库）**
```
Assets/Plugins/Android/
├── MyLibrary.jar         ← 预编译的 Java 库
└── MyFramework.aar       ← Android 归档文件
```

### 为什么 build.gradle 缺失会导致问题？

1. **Unity 使用 Gradle 构建 Android 项目**
2. **没有 build.gradle，Gradle 不知道如何处理这个目录**
3. **Java 源代码不会被编译**
4. **最终 APK 中不包含这些类**

### SAF (Storage Access Framework) 工作原理

```
Unity (C#)
    ↓ 调用
SafPickerBridge.launchVideoPicker()
    ↓
SafPickerProxyActivity (启动系统选择器)
    ↓ 用户选择文件
onActivityResult() 获取 URI
    ↓
UnitySendMessage() 回调到 Unity
    ↓
LocalFileManager.OnAndroidVideoPickerResult() 处理结果
```

**SAF 优势：**
- ✅ 无需存储权限（Android 10+）
- ✅ 用户完全控制文件访问
- ✅ 持久化权限（可选）

---

## 🧪 测试步骤

### 1. 重新构建 APK

```bash
# 在 Unity 中
# File -> Build Settings -> Build
# 或使用 GitHub Actions（推送到 master）
```

### 2. 卸载旧版本

```bash
adb uninstall com.vrplayer.app
```

### 3. 安装新版本

```bash
adb install VRVideoPlayer.apk
```

### 4. 测试 SAF 文件选择器

**测试场景：**
1. 打开应用
2. 点击 **"Select Videos"** 按钮
3. ✅ 应该看到系统文件选择器（Android 文档选择器）
4. ✅ 选择一个或多个视频文件
5. ✅ 点击"打开"或"确定"
6. ✅ 视频应该出现在列表中

**预期日志：**
```
Opening system picker...
Refreshed local videos: X   ← X > 0 表示成功
```

**错误日志（如仍有问题）：**
```
Failed to invoke Android picker bridge: ...
```
这表示 Java 类仍然没有被正确打包。

---

## 🔧 如果仍然失败

### 检查 1: 验证 Java 类是否被打包

```bash
# 解压 APK
unzip VRVideoPlayer.apk -d apk_contents

# 检查是否存在 Java 类
find apk_contents -name "SafPickerBridge.class"
find apk_contents -name "SafPickerProxyActivity.class"

# 如果这些文件不存在，说明 build.gradle 没有生效
```

### 检查 2: 查看构建日志

在 GitHub Actions 的构建日志中搜索：
- `Gradle`
- `SAFPicker`
- `compileJava`
- `BUILD FAILED`

### 检查 3: Unity 控制台错误

如果在 Unity 编辑器中构建，查看控制台是否有：
- `AndroidManifest.xml` 合并错误
- Gradle 构建错误
- Java 编译错误

---

## 📚 相关文档

- [Android 15 修复指南](./ANDROID_15_FIX.md) - 权限配置问题
- [修复总结报告](./FIX_SUMMARY.md) - 本次修复概述
- [开发指南](./DEVELOPMENT_GUIDE.md) - 项目架构说明

---

## 🚀 后续优化建议

### 1. 添加错误处理

在 `SafPickerProxyActivity` 中添加更多错误处理：

```java
private void sendError(String message) {
    Log.e("SafPicker", "Error: " + message);
    // ... 发送错误到 Unity
}
```

### 2. 添加日志

在关键位置添加 Android 日志：

```java
private void openSystemPicker() {
    Log.d("SafPicker", "Opening system picker...");
    // ...
}
```

### 3. 验证 URI 权限

在读取文件之前验证 URI 权限：

```java
private boolean hasReadPermission(Uri uri) {
    try {
        ContentResolver.checkGrantedUriPermission(
            getContentResolver(),
            android.os.Process.myPid(),
            uri,
            Intent.FLAG_GRANT_READ_URI_PERMISSION);
        return true;
    } catch (SecurityException e) {
        return false;
    }
}
```

---

## 📝 总结

### 本次修复解决了什么？

❌ **根本原因：** SAFPicker.androidlib 缺少 build.gradle，导致 Java 代码从未被编译和打包到 APK 中

✅ **解决方案：** 创建完整的 Android 库模块配置，使 Unity/Gradle 能够正确编译 Java 代码

### 关键教训

1. **Unity Android 插件需要正确的项目结构**
   - .androidlib 目录必须有 build.gradle
   - project.properties 必须配置正确
   - Java 源码必须在 src/main/java/ 下

2. **Android 库模块的 AndroidManifest.xml 会被合并**
   - 避免重复声明组件
   - 让主清单文件统一管理

3. **测试 Android 原生功能时，解压 APK 验证**
   - 确保类文件被正确打包
   - 检查资源文件是否包含

---

**修复完成时间：** 2026-03-23  
**状态：** ✅ 就绪，可提交构建  
**预计效果：** SAF 文件选择器将正常工作，无需存储权限即可选择视频文件
