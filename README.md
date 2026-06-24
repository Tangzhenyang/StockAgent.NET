# StockAgent.NET

StockAgent.NET 是一个面向 A 股与港股的深度研究 Agent 应用，包含：

- ASP.NET Core Web API 后端
- React Web UI 前端
- Semantic Kernel 多 Agent 研究编排
- PostgreSQL / pgvector 数据库
- FastAPI 数据源网关
- 用户系统、个人 API Key 配置、历史报告、PDF 导出

## 快速部署

Linux Docker 部署请查看：

[docs/docker-linux-deployment.zh-CN.md](docs/docker-linux-deployment.zh-CN.md)

## 本地开发

```bash
dotnet build
dotnet test
cd src/StockAgent.Web
npm install
npm run dev
```

生产环境不要把真实数据库密码、大模型 API Key 或数据源 API Key 提交到仓库；请通过 `.env`、Docker 环境变量或服务器密钥管理系统配置。
