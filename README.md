# SeventyTwo.Sample

基于 .NET 10 的分层 Web API 示例项目，展示商品、订单、库存和钱包等业务场景，以及 PostgreSQL 持久化、Redis 缓存和 CAP 事件消息的集成方式。

## 项目结构

```text
src/
├── SeventyTwo.Sample.Domain          # 领域模型、聚合根和仓储接口
├── SeventyTwo.Sample.Common          # 公共代码项目
├── SeventyTwo.Sample.Application     # 应用服务和输入输出模型
├── SeventyTwo.Sample.Infrastructure  # 数据访问、消息订阅和基础设施实现
├── SeventyTwo.Sample.DataSetup       # 数据库结构及初始数据初始化服务
└── SeventyTwo.Sample.WebApi          # HTTP 接口、认证和应用启动入口
tests/
├── SeventyTwo.Sample.Domain.Tests        # 领域测试
└── SeventyTwo.Sample.ArchitectureTests  # 架构与映射配置测试
```

## 技术栈

- .NET 10 / ASP.NET Core
- PostgreSQL / SqlSugar
- Redis
- RabbitMQ / DotNetCore.CAP
- Autofac
- Mapster
- xUnit

## 运行环境

启动项目前需要准备：

- .NET 10 SDK
- PostgreSQL
- Redis
- RabbitMQ

## 初始化数据库

创建 PostgreSQL 数据库 `SeventyTwo.Sample`，根据本地环境修改 `src/SeventyTwo.Sample.DataSetup/Program.cs` 中的连接字符串、初始机构和管理员信息，然后运行初始化服务：

```powershell
dotnet run --project src/SeventyTwo.Sample.DataSetup
```

初始化服务通过 SqlSugar Code First 创建表和索引，并写入初始机构、超级管理员、机构成员、页面权限及用户权限。初始化账号和密码由 `Program.cs` 中的 `userName` 和 `initialPassword` 常量配置。

## 配置

复制示例配置：

```powershell
Copy-Item src/SeventyTwo.Sample.WebApi/appsettings.sample.json src/SeventyTwo.Sample.WebApi/appsettings.json
```

根据本地环境修改 `appsettings.json` 中的以下配置：

- `CorsConfiguration`：配置允许跨域访问的来源、请求头和 HTTP 方法
- `JwtConfiguration`：配置签发者、接收者、至少 32 字节的签名密钥，以及 Base64 编码的 64 字节加密密钥
- `ConnectionStrings:PostgreSQL`：PostgreSQL 连接字符串
- `CapConfiguration`：CAP 使用的 PostgreSQL 和 RabbitMQ 配置
- `CapDashboardAuthenticationConfiguration`：CAP Dashboard 登录凭据
- `CacheConfiguration`：Redis 连接配置

可使用以下命令生成随机 JWT 密钥：

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)) # SigningKey
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64)) # EncryptionKey
```

`appsettings.json` 已加入 `.gitignore`，不会提交本地凭据。

## 启动项目

```powershell
dotnet run --project src/SeventyTwo.Sample.WebApi
```

默认监听地址为 <http://localhost:5272>。

## 认证

除显式允许匿名访问的端点外，业务 API 均需要 JWT。先调用 `POST /api/users/Login` 获取访问令牌，后续请求通过以下请求头携带令牌：

```http
Authorization: Bearer <access-token>
```

刷新令牌保存在名为 `refresh_token` 的 HttpOnly Cookie 中，可通过 `POST /api/users/RefreshToken` 刷新访问令牌。CAP Dashboard 使用独立的 HTTP Basic Authentication，凭据由 `CapDashboardAuthenticationConfiguration` 配置。

## 示例功能

项目提供以下 HTTP API：

- `/api/products`：商品增删改查及分页
- `/api/orders`：随机订单生成及多种分页查询
- `/api/inventories/changes`：库存变更
- `/api/wallets/changes`：钱包余额变更
- `/api/users`：用户登录、令牌刷新、退出登录及当前用户信息
- `/api/permissions`：查询当前用户的权限
- `/api/cap-sample/publish`：CAP 消息发布示例

启动后还可访问以下静态示例页面：

- <http://localhost:5272/random-orders.html>
- <http://localhost:5272/page1.html>
- <http://localhost:5272/page2.html>
- <http://localhost:5272/page3.html>
- <http://localhost:5272/cap-sample.html>

CAP Dashboard 默认路径为 <http://localhost:5272/cap>，使用配置文件中的 Dashboard 凭据登录。

其他基础设施端点：

- <http://localhost:5272/health>：健康检查
- <http://localhost:5272/openapi/v1.yaml>：OpenAPI 文档，仅在开发环境中提供

## 测试

```powershell
dotnet test
```

## 开源协议

本项目基于 [MIT License](LICENSE) 开源。
