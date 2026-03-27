# Git 代理配置脚本

适用于 Windows 系统，解决 Git 无法连接 GitHub 的代理问题

## 简体中文版本

### 前置条件

确保您已经拥有以下信息：
- 代理服务器地址：`192.168.8.112`
- 代理服务器端口：`10808`
- Git 仓库路径

---

## 快速配置步骤

### 方法 1：使用 PowerShell 配置（推荐）

1. **打开 PowerShell（以管理员身份运行）**

2. **复制并运行以下命令**：

```powershell
# 设置 Git 代理
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808

# 禁用 SSL 验证（防止证书错误）
git config --global http.sslVerify false
git config --global http.sslBackend schannel

# 设置代理连接超时（可选）
git config --global http.lowSpeedLimit 0
git config --global http.postBuffer 524288000

# 验证配置
Write-Host "`n=== 当前代理配置 ===`n" -ForegroundColor Green
Write-Host "HTTP Proxy: $(git config --global --get http.proxy)" -ForegroundColor Yellow
Write-Host "HTTPS Proxy: $(git config --global --get https.proxy)" -ForegroundColor Yellow
Write-Host "SSL Verify: $(git config --global --get http.sslVerify)" -ForegroundColor Yellow
Write-Host "`n=== 配置完成！ ===`n" -ForegroundColor Green
```

### 方法 2：使用 Git Bash 配置

1. **打开 Git Bash**

2. **复制并运行以下命令**：

```bash
# 设置 Git 代理
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808

# 禁用 SSL 验证
git config --global http.sslVerify false

# 验证配置
echo "=== 当前代理配置 ==="
git config --global --get http.proxy
git config --global --get https.proxy
git config --global --get http.sslVerify
echo "=== 配置完成！ ==="
```

---

## 推送代码步骤

配置完代理后，按照以下步骤推送代码：

### 步骤 1：进入项目目录

```powershell
cd C:\code\vrplayer
```

### 步骤 2：添加文件到暂存区

```powershell
git add .
```

### 步骤 3：提交更改

```powershell
git commit -m "Update project files"
```

### 步骤 4：推送到 GitHub

```powershell
git push origin master
```

---

## 清除代理配置（如果需要）

如果不需要使用代理，可以运行以下命令清除配置：

### PowerShell

```powershell
# 清除代理配置
git config --global --unset http.proxy
git config --global --unset https.proxy

# 恢复 SSL 验证
git config --global http.sslVerify true

# 验证清除
Write-Host "`n=== 代理已清除 ===`n" -ForegroundColor Green
```

### Git Bash

```bash
# 清除代理配置
git config --global --unset http.proxy
git config --global --unset https.proxy

# 恢复 SSL 验证
git config --global http.sslVerify true

echo "=== 代理已清除 ==="
```

---

## 常见问题排查

### 问题 1：连接超时

**症状**：
```
fatal: unable to access 'https://github.com/...': Connection timed out
```

**解决方案**：

1. **检查代理服务器是否运行**
2. **增加 Git 超时时间**：

```powershell
git config --global http.lowSpeedLimit 0
git config --global http.postBuffer 1048576000
```

### 问题 2：SSL 证书错误

**症状**：
```
fatal: unable to access 'https://github.com/...': SSL certificate problem: unable to get local issuer certificate
```

**解决方案**：

```powershell
# 禁用 SSL 验证
git config --global http.sslVerify false
git config --global http.sslBackend schannel
```

### 问题 3：代理认证失败

**症状**：
```
fatal: unable to access 'https://github.com/...': Proxy authentication required
```

**解决方案**：

如果代理需要用户名和密码：

```powershell
# 格式：http://username:password@proxy:port
git config --global http.proxy http://username:password@192.168.8.112:10808
git config --global https.proxy http://username:password@192.168.8.112:10808
```

---

## 验证代理是否工作

运行以下命令测试连接：

```powershell
# 测试连接 GitHub
git ls-remote https://github.com/torvalds/linux.git

# 如果看到输出，说明连接成功
# 如果看到错误，说明代理配置有问题
```

---

## 永久配置文件位置

### Windows 用户级别配置

Git 用户配置文件位于：
```
C:\Users\<用户名>\.gitconfig
```

### 全局配置文件

系统级别配置文件位于：
```
C:\ProgramData\Git\config
```

---

## 📝 快速参考卡片

```powershell
# 配置代理（复制这段）
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808
git config --global http.sslVerify false

# 推送代码
git add .
git commit -m "your message"
git push origin master

# 清除代理（复制这段）
git config --global --unset http.proxy
git config --global --unset https.proxy
git config --global http.sslVerify true
```

---

## 获取帮助

如果遇到问题：

1. **检查 Git 版本**：
   ```powershell
   git --version
   ```

2. **查看当前配置**：
   ```powershell
   git config --global --list
   ```

3. **查看详细日志**：
   ```powershell
   GIT_CURL_VERBOSE=1 git push origin master
   ```

---

## ✅ 配置完成检查清单

完成配置后，请确保：

- [ ] 代理服务器地址正确：`192.168.8.112:10808`
- [ ] 代理服务器正在运行
- [ ] SSL 验证已禁用（如果需要）
- [ ] Git 配置已更新
- [ ] 可以连接到 GitHub
- [ ] 可以成功推送代码

---

**创建时间**: 2025-03-25  
**版本**: v1.0  
**状态**: ✅ 完成

---

**注意**：如果您的代理需要认证，请将 `192.168.8.112:10808` 替换为 `username:password@192.168.8.112:10808`
