# Git 代理配置指南

## 📋 问题说明

您需要通过代理服务器推送 Git 代码到远程仓库。

## 🔧 解决方案

### 步骤 1：配置 Git 代理

根据您的代理地址 `192.168.8.112:10808`，执行以下命令：

```bash
# Windows PowerShell
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808

# Linux/macOS
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808
```

### 步骤 2：验证代理配置

查看当前代理配置：

```bash
git config --global --get http.proxy
git config --global --get https.proxy
```

应该显示：
```
http://192.168.8.112:10808
http://192.168.8.112:10808
```

### 步骤 3：测试连接

```bash
# 测试是否能访问 GitHub
curl --proxy http://192.168.8.112:10808 https://github.com

# 或者直接测试 git 连接
git ls-remote https://github.com/username/repo.git
```

### 步骤 4：推送代码

```bash
# 添加文件
git add .

# 提交
git commit -m "Add project files"

# 推送（使用代理）
git push origin master
```

---

## 🔒 安全配置（可选）

### 方法 1：仅对特定域名使用代理

```bash
# 仅对 GitHub 使用代理
git config --global http.https://github.com.proxy http://192.168.8.112:10808
```

### 方法 2：禁用 SSL 验证（如果遇到 SSL 错误）

```bash
git config --global http.sslVerify false
```

---

## ❌ 常见问题

### 问题 1：连接超时

**错误信息**：
```
fatal: unable to access 'https://github.com/...': Operation timed out
```

**解决方案**：
1. 检查代理服务器是否正常运行
2. 检查代理地址和端口是否正确
3. 尝试增加超时时间：
```bash
git config --global http.lowSpeedLimit 0
git config --global http.postBuffer 524288000
```

### 问题 2：SSL 证书错误

**错误信息**：
```
SSL certificate problem: unable to get local issuer certificate
```

**解决方案**：
```bash
# 禁用 SSL 验证
git config --global http.sslVerify false

# 或者添加 CA 证书路径
git config --global http.sslCAInfo /path/to/cacert.pem
```

### 问题 3：代理认证失败

**错误信息**：
```
fatal: unable to access 'https://github.com/...': Proxy authentication required
```

**解决方案**：

如果代理需要用户名和密码：

```bash
# 格式：http://username:password@proxy:port
git config --global http.proxy http://username:password@192.168.8.112:10808
git config --global https.proxy http://username:password@192.168.8.112:10808
```

---

## 🗑️ 取消代理配置

如果之后不需要使用代理，可以取消：

```bash
# 取消代理配置
git config --global --unset http.proxy
git config --global --unset https.proxy

# 重新启用 SSL 验证
git config --global --unset http.sslVerify
```

---

## 📝 完整配置脚本

### Windows PowerShell 脚本

```powershell
# 配置代理
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808

# 禁用 SSL 验证（如果需要）
git config --global http.sslVerify false

# 验证配置
Write-Host "HTTP Proxy:" (git config --global --get http.proxy)
Write-Host "HTTPS Proxy:" (git config --global --get https.proxy)
Write-Host "SSL Verify:" (git config --global --get http.sslVerify)

Write-Host "Git 代理配置完成！"
```

### Linux/macOS Shell 脚本

```bash
#!/bin/bash

# 配置代理
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808

# 禁用 SSL 验证（如果需要）
git config --global http.sslVerify false

# 验证配置
echo "HTTP Proxy: $(git config --global --get http.proxy)"
echo "HTTPS Proxy: $(git config --global --get https.proxy)"
echo "SSL Verify: $(git config --global --get http.sslVerify)"

echo "Git 代理配置完成！"
```

---

## 🎯 推荐做法

### 1. 仅对 GitHub 使用代理

```bash
# 取消全局代理
git config --global --unset http.proxy
git config --global --unset https.proxy

# 仅对 GitHub 使用代理
git config --global http.https://github.com.proxy http://192.168.8.112:10808
```

### 2. 使用环境变量

```bash
# Windows PowerShell
$env:HTTP_PROXY="http://192.168.8.112:10808"
$env:HTTPS_PROXY="http://192.168.8.112:10808"
git push origin master

# Linux/macOS
export HTTP_PROXY="http://192.168.8.112:10808"
export HTTPS_PROXY="http://192.168.8.112:10808"
git push origin master
```

### 3. 临时使用代理（仅当前命令）

```bash
# 使用代理执行单条命令
git -c http.proxy=http://192.168.8.112:10808 \
    -c https.proxy=http://192.168.8.112:10808 \
    push origin master
```

---

## 📚 参考资源

- [Git 官方文档 - 代理配置](https://git-scm.com/book/zh/v2/Git-%E5%B7%A5%E5%B7%A5-%E8%87%AA%E5%AE%9A%E4%B9%89-Git#_代理)
- [Git 代理配置详解](https://git-scm.com/docs/git-config)

---

## ✅ 检查清单

在推送代码前，确保：

- [ ] 代理服务器地址正确：`192.168.8.112:10808`
- [ ] 代理服务器正常运行
- [ ] Git 代理已配置
- [ ] 可以访问 GitHub
- [ ] Git 凭证已配置（如果需要）

---

## 🚀 快速开始

### 一键配置并推送（Windows PowerShell）

```powershell
# 1. 配置代理
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808

# 2. 添加所有文件
git add .

# 3. 提交
git commit -m "Update project"

# 4. 推送
git push origin master
```

### 一键配置并推送（Linux/macOS）

```bash
# 1. 配置代理
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808

# 2. 添加所有文件
git add .

# 3. 提交
git commit -m "Update project"

# 4. 推送
git push origin master
```

---

**创建时间**: 2025-03-25  
**版本**: v1.0  
**状态**: ✅ 完成
