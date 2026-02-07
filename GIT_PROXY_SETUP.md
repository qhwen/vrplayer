# Git 代理配置指南

## 问题
- ✅ 代码已提交到本地仓库
- ❌ 推送到 GitHub 时网络可能不稳定
- 🌐 需要通过代理：`http://192.168.8.112:10808`

---

## 方案 1：全局代理配置（推荐）

### 方法 A：使用 Git 命令（最简单）
```bash
# 配置 HTTP 代理
git config --global http.proxy http://192.168.8.112:10808

# 配置 HTTPS 代理
git config --global https.proxy http://192.168.8.112:10808

# 验证配置
git config --global --list | grep -i proxy
```

### 方法 B：使用环境变量（更灵活）
```bash
# 配置代理（支持 Git 命令行）
export GIT_PROXY_COMMAND="curl -x http://192.168.8.112:10808"

# 使用代理推送
export GIT_HTTP_PROXY="http://192.168.8.112:10808"
git push -u origin master
```

### 方法 C：使用 core.gitconfig（永久配置）
```bash
# 编辑 core.gitconfig
nano ~/.gitconfig
# 或使用 vim ~/.gitconfig
```

在文件中添加：
```ini
[http]
    proxy = http://192.168.8.112:10808

[https]
    proxy = http://192.168.8.112:10808
```

保存后：
```bash
# 验证
git config --list
```

---

## 方案 2：仅推送时使用代理（推荐）

### 配置 Git 不使用特定域名的代理
```bash
# 配置特定域名绕过代理（可选）
git config --global http."http://github.com".proxy ""

# 推送命令（使用代理）
GIT_HTTP_PROXY="http://192.168.8.112:10808" git push -u origin master
```

---

## 方案 3：配置 Git 忽略 SSL 错误（推荐）

### 代理可能导致的 SSL 问题
```bash
# 禁用 SSL 验证（解决代理 SSL 错误）
git config --global http.sslVerify false
git config --global http.proxySSLVerify false
```

---

## 方案 4：使用 socks 代理（如果需要）

### 如果你的代理是 socks5
```bash
# 配置 socks 代理
git config --global core.gitproxy "socks5://192.168.8.112:10808"
```

---

## 方案 5：调试代理问题

### 查看代理是否工作
```bash
# 测试连接
curl -x http://192.168.8.112:10808 https://www.github.com

# 查看 Git 是否使用代理
GIT_CURL_VERBOSE=1 git push -u origin master
```

---

## 方案 6：临时禁用代理（如果代理有问题）

### 推送时不使用代理
```bash
# 临时禁用代理（仅此命令）
GIT_NO_PROXY="yes" git push -u origin master

# 或修改 URL 暂时绕过
git remote set-url origin https://github.com/qhwen/vrplayer.git
git push -u origin master
```

---

## 🚀 快速解决方案（推荐使用）

### 完整配置脚本

```bash
#!/bin/bash
# Git 代理配置脚本
echo "🚀 配置 Git 使用代理..."

# 方法 1：全局代理（推荐）
git config --global http.proxy http://192.168.8.112:10808
git config --global https.proxy http://192.168.8.112:10808

# 方法 2：仅推送 GitHub 时使用代理（更安全）
# 配置特定域名使用代理
git config --global http."github.com".proxy ""

# 方法 3：禁用 SSL 验证（代理常见问题）
git config --global http.sslVerify false
git config --global http.proxySSLVerify false

# 验证配置
echo "✓ 当前代理配置："
git config --global --list | grep -i proxy

# 推送代码
echo ""
echo "📤 正在推送到 GitHub（使用代理）..."
git push -u origin master

# 检查结果
if [ $? -eq 0 ]; then
    echo "✅ 推送成功！"
    exit 0
else
    echo "❌ 推送失败，请检查代理配置"
    echo ""
    echo "📋 查看日志：GIT_CURL_VERBOSE=1 git push -u origin master"
    exit 1
fi
```

---

## 📋 故障排除

### 问题 1：连接被拒绝
**错误：** `fatal: unable to access ... Permission denied (publickey)`
**原因：** 代理认证失败或配置错误
**解决：** 检查代理地址和端口

### 问题 2：SSL 错误
**错误：** `SSL certificate problem: unable to get local issuer certificate`
**原因：** 代理的 SSL 证书问题
**解决：** 禁用 SSL 验证（方法 5）

### 问题 3：超时
**错误：** `fatal: operation timed out`
**原因：** 网络连接不稳定
**解决：** 重试推送，或增加超时时间

---

## ✅ 推荐步骤

### 步骤 1：验证代理可用性
```bash
curl -x http://192.168.8.112:10808 https://www.google.com
```

### 步骤 2：配置 Git 代理
```bash
# 使用方法 1 或方法 2
git config --global http.proxy http://192.168.8.112:10808
```

### 步骤 3：测试推送
```bash
# 使用环境变量方式（方法 B）
export GIT_HTTP_PROXY="http://192.168.8.112:10808"
git push -u origin master
```

---

## 🎯 最终方案

**推荐配置：**
```bash
# 1. 配置 HTTP 代理
git config --global http.proxy http://192.168.8.112:10808

# 2. 配置 HTTPS 代理
git config --global https.proxy http://192.168.8.112:10808

# 3. 推送（自动使用代理）
git push -u origin master
```

---

*配置脚本版本：1.0*
*创建日期：2026-02-07*
