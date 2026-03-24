# SAF Picker 构建修复补丁 - 手动应用指南

## 🚨 构建错误

GitHub Actions 构建失败，错误信息：

```
1. minSdkVersion 冲突
   uses-sdk:minSdkVersion 22 cannot be smaller than version 24
   
2. Java 版本错误
   error: invalid source release: 17
   
3. StackOverflowError
```

---

## ✅ 已修复的内容

### 修复 1: build.gradle 配置

**文件：** `Assets/Plugins/Android/SAFPicker.androidlib/build.gradle`

**修改：**
```diff
-    minSdkVersion 24
+    minSdkVersion 22
```

```diff
-        sourceCompatibility JavaVersion.VERSION_17
-        targetCompatibility JavaVersion.VERSION_17
+        sourceCompatibility JavaVersion.VERSION_11
+        targetCompatibility JavaVersion.VERSION_11
```

---

## 📝 手动应用步骤

### 步骤 1: 打开文件

在 Unity 项目中找到并打开：
```
Assets/Plugins/Android/SAFPicker.androidlib/build.gradle
```

### 步骤 2: 修改配置

将文件内容改为：

```groovy
// SAF Picker Android Library Module
// This module provides the Storage Access Framework (SAF) file picker functionality

plugins {
    id 'com.android.library'
}

android {
    namespace 'com.vrplayer.saf'
    compileSdkVersion 34

    defaultConfig {
        minSdkVersion 22
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
        sourceCompatibility JavaVersion.VERSION_11
        targetCompatibility JavaVersion.VERSION_11
    }

    lintOptions {
        abortOnError false
    }
}

dependencies {
    implementation 'androidx.appcompat:appcompat:1.6.1'
}
```

### 步骤 3: 保存文件

保存修改后的文件。

### 步骤 4: 提交更改

在你的本地仓库中：
```bash
git add Assets/Plugins/Android/SAFPicker.androidlib/build.gradle
git commit -m "Fix Gradle minSdkVersion and Java version"
git push origin master
```

### 步骤 5: 等待构建

推送到 GitHub 后，等待 GitHub Actions 完成构建（约 10-15 分钟）。

---

## 🔍 修复说明

### 问题 1: minSdkVersion 冲突

**错误：** `minSdkVersion 22 cannot be smaller than version 24`

**原因：** 
- Unity 主项目的 minSdkVersion 是 22
- 我们创建的库模块设置了 minSdkVersion 24
- Android 要求库的 minSdkVersion 不能大于主项目

**解决：** 将 minSdkVersion 从 24 改为 22

### 问题 2: Java 版本不兼容

**错误：** `error: invalid source release: 17`

**原因：**
- Unity 2022.3 使用的 Gradle 版本是 7.5.1
- Gradle 7.5.1 不支持 Java 17
- 只支持 up to Java 11

**解决：** 将 Java 版本从 VERSION_17 改为 VERSION_11

### 问题 3: StackOverflowError

**原因：** 可能是 Gradle 配置问题，修复前两个问题后应该解决

---

## 📋 完整文件内容

如果你需要替换整个文件，使用以下内容：

```groovy
// SAF Picker Android Library Module
// This module provides the Storage Access Framework (SAF) file picker functionality

plugins {
    id 'com.android.library'
}

android {
    namespace 'com.vrplayer.saf'
    compileSdkVersion 34

    defaultConfig {
        minSdkVersion 22
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
        sourceCompatibility JavaVersion.VERSION_11
        targetCompatibility JavaVersion.VERSION_11
    }

    lintOptions {
        abortOnError false
    }
}

dependencies {
    implementation 'androidx.appcompat:appcompat:1.6.1'
}
```

---

## 🧪 验证修复

应用修复后，你应该能够在 GitHub Actions 日志中看到：

```
:SAFPicker.androidlib:compileReleaseJavaWithJavac 
BUILD SUCCESSFUL
```

而不是之前的：

```
error: invalid source release: 17
BUILD FAILED
```

---

## 📚 相关文件

此修复需要以下文件（已在之前创建）：

1. ✅ `Assets/Plugins/Android/SAFPicker.androidlib/build.gradle` - 需要修改
2. ✅ `Assets/Plugins/Android/SAFPicker.androidlib/proguard-rules.pro` - 已创建
3. ✅ `Assets/Plugins/Android/SAFPicker.androidlib/gradle/wrapper/gradle-wrapper.properties` - 已创建
4. ✅ `Assets/Plugins/Android/SAFPicker.androidlib/AndroidManifest.xml` - 已修改
5. ✅ `Assets/Plugins/Android/SAFPicker.androidlib/project.properties` - 已修改

---

## 💡 提示

如果你在本地 Unity 编辑器中测试，确保：
1. 删除 `Library` 文件夹（清理缓存）
2. 重新打开项目
3. 重新构建 APK

---

**创建时间：** 2026-03-24  
**状态：** ✅ 修复已准备好手动应用
