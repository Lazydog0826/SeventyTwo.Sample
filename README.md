# SeventyTwo.Sample

基于 .NET 10 的分层 Web API 示例项目，包含用户、权限、机构、数据字典、商品、订单、库存和钱包等业务场景，并集成 PostgreSQL、Redis、RabbitMQ 与 CAP。

## 项目结构

```text
src/
├── SeventyTwo.Sample.Common          # 公共消息键等共享代码
├── SeventyTwo.Sample.Domain          # 领域模型、聚合根和仓储接口
├── SeventyTwo.Sample.Application     # 应用服务和输入输出模型
├── SeventyTwo.Sample.Infrastructure  # 持久化、缓存、认证和消息实现
├── SeventyTwo.Sample.DataSetup       # 数据库建表及初始数据初始化程序
└── SeventyTwo.Sample.WebApi          # HTTP 接口、认证授权和应用入口
tests/
├── SeventyTwo.Sample.Domain.Tests        # 领域单元测试
└── SeventyTwo.Sample.ArchitectureTests  # 架构、认证、接口和映射测试
```

## 技术栈

- .NET 10 / ASP.NET Core
- PostgreSQL / SqlSugar
- Redis
- RabbitMQ / DotNetCore.CAP
- Autofac
- Mapster
- Serilog
- xUnit

## 运行环境

启动项目前需要准备：

- .NET 10 SDK
- PostgreSQL
- Redis
- RabbitMQ

## 初始化数据库

创建 PostgreSQL 数据库 `SeventyTwo.Sample`，然后在 `src/SeventyTwo.Sample.DataSetup` 下创建 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=SeventyTwo.Sample;Username=postgres;Password=your_password"
  }
}
```

规范：初始化程序仅用于全新空库且只能执行一次；它不是可重复执行的数据迁移程序。

运行初始化程序：

```powershell
dotnet run --project src/SeventyTwo.Sample.DataSetup
```

初始化程序通过 SqlSugar Code First 创建业务表，并写入超级管理员和权限管理、机构管理、数据字典、用户管理所需的初始权限。默认管理员账号为 `superadmin`，初始密码为 `123456`；相关初始信息定义在 `src/SeventyTwo.Sample.DataSetup/Program.cs` 中。

## 配置 Web API

复制示例配置：

```powershell
Copy-Item src/SeventyTwo.Sample.WebApi/appsettings.sample.json src/SeventyTwo.Sample.WebApi/appsettings.json
```

根据本地环境修改以下配置：

- `CorsConfiguration`：允许跨域访问的来源、请求头和 HTTP 方法
- `JwtConfiguration`：令牌签发者、接收者、签名密钥、加密密钥及访问令牌和刷新令牌有效期
- `ConnectionStrings:PostgreSQL`：业务数据库及 CAP 消息存储使用的 PostgreSQL 连接字符串
- `CapConfiguration`：RabbitMQ 主机、账号、密码和虚拟主机
- `CapDashboardAuthenticationConfiguration`：CAP Dashboard 的 Basic Authentication 凭据
- `CacheConfiguration`：Redis 连接、键命名空间和默认数据库

`JwtConfiguration:SigningKey` 应至少包含 32 字节，`EncryptionKey` 必须是 Base64 编码的 64 字节密钥。可使用 PowerShell 生成：

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)) # SigningKey
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64)) # EncryptionKey
```

各项目的 `appsettings.json` 均已加入 `.gitignore`，不会提交本地凭据。

## 启动项目

```powershell
dotnet run --project src/SeventyTwo.Sample.WebApi
```

开发环境默认监听 <http://localhost:5272>。

也可以构建并运行 Docker 镜像；容器内服务端口为 `8080`：

```powershell
docker build -t seventytwo-sample .
docker run --rm -p 8080:8080 seventytwo-sample
```

规范：容器的 HTTP `8080` 端口仅用于受信任内网；生产环境必须经 TLS 反向代理访问，且不得将使用 Basic Authentication 的 `/cap` 直接暴露到公网。

生产部署时需通过配置文件、环境变量或挂载文件提供 Web API 配置。

## 认证与授权

除显式允许匿名访问的端点外，业务 API 均需要 JWT。调用 `POST /api/users/Login` 获取访问令牌，后续请求通过请求头携带令牌：

```http
Authorization: Bearer <access-token>
```

刷新令牌保存在名为 `refresh_token` 的 HttpOnly、Secure Cookie 中，可通过 `POST /api/users/RefreshToken` 刷新访问令牌，通过 `POST /api/users/Logout` 注销当前会话。

用户、权限、机构和数据字典管理接口还会根据权限编码进行授权。超级管理员不受普通权限分配限制。CAP Dashboard 使用独立的 HTTP Basic Authentication。

## 示例功能

主要 HTTP API：

- `/api/users`：登录、令牌刷新、用户管理和用户授权
- `/api/permissions`：权限树及权限管理
- `/api/organizations`：机构管理
- `/api/dataDictionaries`：数据字典和字典项管理
- `/api/products`：商品增删改查及分页查询
- `/api/orders`：随机订单生成及多种分页查询
- `/api/inventories/changes`：库存变更
- `/api/wallets/changes`：钱包余额变更

静态示例页面：

- <http://localhost:5272/random-orders.html>
- <http://localhost:5272/page1.html>
- <http://localhost:5272/page2.html>
- <http://localhost:5272/page3.html>

基础设施端点：

- <http://localhost:5272/health>：健康检查，允许匿名访问
- <http://localhost:5272/openapi/v1.yaml>：OpenAPI 3.1 文档，仅在开发环境提供
- <http://localhost:5272/cap>：CAP Dashboard，使用配置的 Dashboard 凭据登录

应用日志输出到控制台；Error 及以上级别日志同时按天写入 `logs/log-*.txt`，最多保留 30 个文件。

## 测试

```powershell
dotnet test
```

## 开源协议

本项目基于 [MIT License](LICENSE) 开源。
