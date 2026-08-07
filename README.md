# SeventyTwo.Sample

基于 .NET 10 的分层 Web API 示例项目，展示商品、订单、库存和钱包等业务场景，以及 PostgreSQL 持久化、Redis 缓存和 CAP 事件消息的集成方式。

## 项目结构

```text
src/
├── SeventyTwo.Sample.Domain          # 领域模型、聚合根和仓储接口
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

创建 PostgreSQL 数据库 `SeventyTwo.Sample`，在 `src/SeventyTwo.Sample.DataSetup/Program.cs` 中填写连接字符串，然后运行初始化服务：

```powershell
dotnet run --project src/SeventyTwo.Sample.DataSetup
```

初始化服务通过 SqlSugar Code First 创建表和索引，并写入测试机构及超级管理员。

## 配置

复制示例配置：

```powershell
Copy-Item src/SeventyTwo.Sample.WebApi/appsettings.sample.json src/SeventyTwo.Sample.WebApi/appsettings.json
```

根据本地环境修改 `appsettings.json` 中的以下配置：

- `ConnectionStrings:PostgreSQL`：PostgreSQL 连接字符串
- `CapConfiguration`：CAP 使用的 PostgreSQL 和 RabbitMQ 配置
- `CapDashboardAuthenticationConfiguration`：CAP Dashboard 登录凭据
- `CacheConfiguration`：Redis 连接配置

`appsettings.json` 已加入 `.gitignore`，不会提交本地凭据。

## 启动项目

```powershell
dotnet run --project src/SeventyTwo.Sample.WebApi
```

默认监听地址为 <http://localhost:5272>。

## 示例功能

项目提供以下 HTTP API：

- `/api/products`：商品增删改查及分页
- `/api/orders`：随机订单生成及多种分页查询
- `/api/inventories/changes`：库存变更
- `/api/wallets/changes`：钱包余额变更
- `/api/cap-sample/publish`：CAP 消息发布示例

启动后还可访问以下静态示例页面：

- <http://localhost:5272/random-orders.html>
- <http://localhost:5272/page1.html>
- <http://localhost:5272/page2.html>
- <http://localhost:5272/page3.html>
- <http://localhost:5272/cap-sample.html>

CAP Dashboard 默认路径为 <http://localhost:5272/cap>，使用配置文件中的 Dashboard 凭据登录。

## 测试

```powershell
dotnet test
```
