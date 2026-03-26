# GitHub Actions 工作流修复指南

## 🔧 修复的问题

我已经修复了 `build.yml` 文件中的以下问题：

### 1. 语法错误
- ✅ 修复了 YAML 缩进问题
- ✅ 修复了错误的引号使用
- ✅ 修复了格式不一致的问题

### 2. 工作流配置
- ✅ 添加了正确的触发器（push、pull_request、workflow_dispatch）
- ✅ 添加了并发控制，避免重复构建
- ✅ 修复了 Unity License 的配置

### 3. 构建步骤优化
- ✅ 简化了构建流程
- ✅ 使用正确的 Unity Builder Action
- ✅ 添加了测试任务

---

## 📝 必需的 GitHub Secrets 配置

在运行 GitHub Actions 之前，您需要在 GitHub 仓库中配置以下 Secrets：

### 步骤 1：配置 Secrets

前往：`GitHub 仓库 > Settings > Secrets and variables > Actions`

#### 需要配置的 Secrets：

| Secret 名称 | 说明 | 如何获取 |
|------------|------|---------|
| `UNITY_LICENSE` | Unity 许可证密钥 | 从 Unity 授权网站获取 |
| `ANDROID_KEYSTORE_BASE64` | Android 签名密钥的 Base64 编码 | 见下方说明 |
| `ANDROID_KEYALIAS_NAME` | Android 密钥别名 | 您在创建密钥时指定的别名 |
| `ANDROID_KEYALIAS_PASS` | Android 密钥密码 | 您在创建密钥时指定的密码 |

---

## 🔑 生成 Android 签名密钥

### 步骤 1：创建 KeyStore

```bash
keytool -genkey -v -keystore my-release-key.keystore \
  -alias my-key-alias -keyalg RSA -keysize 2048 -validity 10000
```

### 步骤 2：转换为 Base64

#### 在 Linux/macOS 上：
```bash
base64 -i my-release-key.keystore | tr -d '\n' > keystore-base64.txt
```

#### 在 Windows 上：
```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("my-release-key.keystore")) | Out-File keystore-base64.txt
```

### 步骤 3：复制 Base64 内容

打开 `keystore-base64.txt` 文件，复制其中的内容，然后粘贴到 GitHub Secrets 的 `ANDROID_KEYSTORE_BASE64` 中。

---

## 🔐 获取 Unity License

### 步骤 1：访问 Unity 授权网站

前往：https://license.unity3d.com/manual

### 步骤 2：获取 Personal License

1. 登录您的 Unity 账户
2. 选择 "Get a free Personal License"
3. 激活许可证
4. 下载 `.ulf` 文件

### 步骤 3：转换为 Base64

#### 在 Linux/macOS 上：
```bash
base64 -i Unity_v2022.x.ulf | tr -d '\n' > unity-license-base64.txt
```

#### 在 Windows 上：
```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("Unity_v2022.x.ulf")) | Out-File unity-license-base64.txt
```

### 步骤 4：添加到 GitHub Secrets

复制 `unity-license-base64.txt` 的内容，粘贴到 GitHub Secrets 的 `UNITY_LICENSE` 中。

---

## 🚀 使用修复后的工作流

### 触发构建

#### 自动触发
工作流会在以下情况下自动触发：
- ✅ Push 到 `master` 分支
- ✅ 创建 Pull Request 到 `master` 分支

#### 手动触发
1. 前往 GitHub 仓库的 `Actions` 标签页
2. 选择 `Build Unity VR Player` 工作流
3. 点击 `Run workflow` 按钮
4. 选择分支并点击 `Run workflow`

---

## 📊 工作流任务

### 1. Build Android APK

**功能**：
- ✅ 构建可安装的 Android APK
- ✅ 使用签名密钥对 APK 进行签名
- ✅ 上传构建产物

**输出**：
- 构建完成后，可以在 Actions 页面下载 APK 文件
- APK 会保存 7 天

### 2. Run Unit Tests

**功能**：
- ✅ 运行所有单元测试
- ✅ 生成测试报告
- ✅ 计算代码覆盖率

**输出**：
- 测试结果会自动上传
- 可以在 Actions 页面查看详细的测试报告

---

## 🔍 查看构建日志

### 查看工作流运行状态

1. 前往 GitHub 仓库
2. 点击 `Actions` 标签页
3. 选择最近的工作流运行
4. 点击工作流名称查看详细信息

### 查看构建步骤

每个任务都包含以下步骤：
1. **Checkout Repository** - 检出代码
2. **Cache Unity Library** - 缓存 Unity 库
3. **Install Unity** - 安装 Unity 引擎
4. **Build/Run Tests** - 构建或运行测试
5. **Upload Artifact** - 上传构建产物

### 下载构建产物

1. 在工作流运行页面，滚动到页面底部
2. 找到 `Artifacts` 部分
3. 点击产物名称下载

---

## ❌ 常见问题排查

### 问题 1：Unity License 错误

**错误信息**：
```
Error: License not found
```

**解决方案**：
1. 确保 `UNITY_LICENSE` Secret 已正确配置
2. 确认 License 对应的 Unity 版本（2022.3.40f1）
3. 重新获取 License 并更新 Secret

### 问题 2：Android 签名错误

**错误信息**：
```
Error: Keystore not found
```

**解决方案**：
1. 确保 `ANDROID_KEYSTORE_BASE64` 已正确配置
2. 确认 Base64 编码是正确的
3. 检查密钥别名和密码是否正确

### 问题 3：构建超时

**错误信息**：
```
Error: Build timed out
```

**解决方案**：
1. 增加 `timeout-minutes` 值
2. 检查项目是否有性能问题
3. 考虑优化 Unity 项目

### 问题 4：测试失败

**错误信息**：
```
Error: Tests failed
```

**解决方案**：
1. 查看测试日志了解失败的测试
2. 修复代码中的问题
3. 提交修复并重新运行工作流

---

## 📚 相关文档

- [Unity Builder Action 文档](https://github.com/game-ci/unity-builder)
- [Unity Test Runner Action 文档](https://github.com/game-ci/unity-test-runner)
- [Unity 安装程序文档](https://github.com/game-ci/unity-installer-action)
- [Unity 授权指南](https://docs.unity3d.com/Manual/ManagingYourUnityLicense.html)

---

## ✅ 检查清单

在首次运行工作流前，请确保：

- [ ] 所有 GitHub Secrets 已配置
- [ ] Unity License 对应正确的 Unity 版本（2022.3.40f1）
- [ ] Android 密钥已正确生成并转换为 Base64
- [ ] 仓库有足够的 Actions 分钟数
- [ ] Unity 项目可以在本地成功构建

---

## 🎯 下一步

1. **配置 Secrets** - 按照上述说明配置所有必需的 Secrets
2. **测试工作流** - 手动触发工作流进行测试
3. **查看结果** - 检查构建日志和产物
4. **优化配置** - 根据需要调整工作流配置

---

**创建时间**: 2025-03-25
**版本**: v1.0
**状态**: ✅ 已修复
