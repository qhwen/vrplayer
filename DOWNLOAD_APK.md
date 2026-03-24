# 下载 APK 构建产物

## 🎉 构建已完成

代码已成功推送到 GitHub，并且 CI/CD 工作流已经完成构建。

---

## 📦 下载 APK

### 方式 1: 直接访问 Actions 页面（推荐）

**点击以下链接直接访问构建页面：**

```
https://github.com/qhwen/vrplayer/actions
```

**步骤：**
1. 点击上方链接或在浏览器中打开
2. 找到最新的 "Build Unity VR Player" 工作流（应该标记为 ✓ 成功）
3. 点击该工作流运行
4. 滚动到页面底部的 "Artifacts" 部分
5. 点击下载 `VRVideoPlayer-Android-APK`

---

### 方式 2: 直接访问最新构建

**使用以下链接直接访问最新构建：**

```
https://github.com/qhwen/vrplayer/actions/workflows/build.yml
```

**步骤：**
1. 点击最新的成功构建（绿色 ✓ 标记）
2. 在页面底部找到 "Artifacts"
3. 下载 `VRVideoPlayer-Android-APK`

---

## 📱 安装 APK

### 1. 解压下载的文件

下载的文件是 ZIP 压缩包，包含：
- APK 文件
- 可能的构建信息文件

### 2. 卸载旧版本（重要！）

```bash
adb uninstall com.vrplayer.app
```

或在手机上：
- 设置 → 应用 → VR Video Player → 卸载

### 3. 安装新版本

**方式 A: 使用 adb**
```bash
adb install VRVideoPlayer.apk
```

**方式 B: 直接在手机上安装**
- 将 APK 文件传输到手机
- 点击 APK 文件安装
- 如果提示"未知来源"，请在设置中允许

---

## 🔧 构建信息

**仓库：** https://github.com/qhwen/vrplayer  
**工作流：** Build Unity VR Player  
**触发方式：** 推送到 master 分支  
**构建平台：** Android  
**Unity 版本：** 2022.3.40f1  

---

## 📋 本次构建包含的修复

✅ **Android 15 兼容性修复**
- 添加 AndroidManifest.xml 权限声明
- 支持 READ_MEDIA_VIDEO (Android 13+)
- 支持 READ_MEDIA_VISUAL_USER_SELECTED (Android 14+)
- 优化权限请求流程

✅ **文档完善**
- Android 15 修复指南
- 开发指南
- 修复总结报告

---

## 🧪 测试清单

安装后请测试：

### ✅ 基本功能
- [ ] 应用正常启动
- [ ] UI 正常显示
- [ ] "Select Videos" 按钮可用

### ✅ Android 15 权限
- [ ] 点击 "Select Videos" 能打开系统文件选择器
- [ ] 选择视频文件成功
- [ ] 视频出现在列表中
- [ ] 点击视频能正常播放

### ✅ 权限请求（可选）
- [ ] 点击 "Scan Settings" 请求权限
- [ ] 权限对话框正常显示
- [ ] 授权后能扫描 Movies 目录

---

## 🐛 如果遇到问题

### 问题 1: 无法打开文件选择器

**解决：**
- 检查是否授予了应用权限
- 尝试卸载并重新安装
- 查看 [Android 15 修复指南](./unity_vr_player/ANDROID_15_FIX.md)

### 问题 2: 视频列表为空

**解决：**
- 点击 "Refresh" 按钮刷新
- 使用 "Select Videos" 手动选择视频
- 检查视频文件格式（支持 .mp4, .mkv, .mov）

### 问题 3: 无法播放视频

**解决：**
- 检查视频文件是否损坏
- 查看日志：`adb logcat -s Unity`
- 确认视频格式支持

---

## 📊 构建详情

**构建时间：** 查看 Actions 页面获取准确时间  
**构建日志：** 可在 Actions 页面下载 `Unity-Build-Log`  
**APK 大小：** 查看 Actions 页面获取准确大小  

---

## 🔗 快速链接

| 链接 | 说明 |
|------|------|
| [Actions 页面](https://github.com/qhwen/vrplayer/actions) | 下载 APK |
| [修复指南](./unity_vr_player/ANDROID_15_FIX.md) | Android 15 问题解决 |
| [开发文档](./unity_vr_player/DEVELOPMENT_GUIDE.md) | 开发指南 |
| [修复总结](./unity_vr_player/FIX_SUMMARY.md) | 本次修复详情 |

---

**提示：** 如果需要自动化下载，可以安装 GitHub CLI (`gh`) 然后使用：
```bash
gh run download --repo qhwen/vrplayer --name VRVideoPlayer-Android-APK
```
