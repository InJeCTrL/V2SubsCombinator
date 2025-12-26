# V2SubsCombinator

V2Ray/Clash 订阅合并服务，支持多订阅源或节点合并、自定义导出订阅、自定义订阅或节点前缀。

## 功能

- 合并多个 V2Ray/Clash 订阅源
- 支持 VMess、VLESS、Trojan、Shadowsocks 等协议
- 自定义订阅或节点前缀
- 用户认证和订阅管理
- 自定义导出订阅路由

## 开发基于

- Dotnet SDK 10
- Terraform
- MongoDB API
- Docker
- JWT
- Azure

## 本地开发

### 前提条件

- Dotnet 开发环境
- MongoDB 连接串

### 运行

```bash
> cp appsettings.json appsettings.Development.json
# !!! 修改 appsettings.Development.json 中的 ConnectionStrings、JWTSettings 配置项 !!!
> dotnet run
```

访问 http://localhost:5025

## 云服务部署 (Azure via Terraform)

### 前提条件

- Terraform CLI
- Azure CLI
- Azure 订阅

### 部署步骤

```bash
> az login
> cd infra
> cp terraform.tfvars.example terraform.tfvars
# !!! 修改 terraform.tfvars !!!
> terraform init
> terraform apply
```

### 部署资源

Azure免费层可以基本覆盖以下资源:

- Azure Container Apps
- Azure Cosmos DB
- Log Analytics Workspace
