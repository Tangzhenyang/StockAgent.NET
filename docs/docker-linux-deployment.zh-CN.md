# StockAgent.NET Linux Docker 部署说明

本文档说明如何在 Linux 服务器上使用 Docker 部署 StockAgent.NET。

## 容器组成

默认 `docker-compose.yml` 会启动 4 个容器：

| 服务 | 容器作用 | 默认端口 |
| --- | --- | --- |
| `web` | React Web UI，Nginx 托管，并反向代理 `/api` 到后端 | `80` |
| `api` | ASP.NET Core Web API、用户系统、任务编排、PDF 导出、多 Agent 分析 | `5000` |
| `postgres` | PostgreSQL 16 + pgvector | `5432` |
| `datasource` | FastAPI 数据源网关，用于行情、财务、公告、证据抓取 | `8000` |

如果你已经在服务器上单独部署了 PostgreSQL，可以使用 `docker-compose.external-pg.yml`，只启动 `web`、`api`、`datasource`，不启动内置 `postgres` 容器。

## 服务器准备

在 Linux 服务器安装 Docker 和 Docker Compose 插件：

```bash
sudo apt update
sudo apt install -y ca-certificates curl gnupg
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
```

重新登录 SSH 后确认：

```bash
docker version
docker compose version
```

## 获取代码

```bash
git clone https://github.com/Tangzhenyang/StockAgent.NET.git
cd StockAgent.NET
```

## 配置环境变量

复制示例配置：

```bash
cp .env.example .env
```

编辑 `.env`：

```bash
nano .env
```

至少修改这些值：

```env
POSTGRES_PASSWORD=换成强密码
PUBLIC_WEB_ORIGIN=http://你的服务器IP
DATA_SOURCE_API_KEY=换成数据源服务访问密钥
```

如果你使用已经部署好的外部 PostgreSQL，还需要修改：

```env
EXTERNAL_POSTGRES_HOST=你的PG服务器IP或域名
EXTERNAL_POSTGRES_PORT=5432
EXTERNAL_POSTGRES_DB=stockagent
EXTERNAL_POSTGRES_USER=admin
EXTERNAL_POSTGRES_PASSWORD=你的PG密码
```

如果你的服务器开放域名，`PUBLIC_WEB_ORIGIN` 可写成：

```env
PUBLIC_WEB_ORIGIN=https://your-domain.com
```

## 启动服务：使用内置 PostgreSQL

如果希望由 Docker Compose 一起启动 PostgreSQL、API、Web、数据源服务，执行：

```bash
docker compose up -d --build
```

该模式会启动：

- `postgres`
- `api`
- `web`
- `datasource`

## 启动服务：使用已有 PostgreSQL

如果你已经在 Linux 服务器上部署好了 PostgreSQL，并且数据库已开启 `pgvector`，推荐使用外部 PG override 文件：

```bash
docker compose -f docker-compose.yml -f docker-compose.external-pg.yml up -d --build api web datasource
```

该命令只启动：

- `api`
- `web`
- `datasource`

不会启动 compose 内置的 `postgres` 容器。

API 容器会使用 `.env` 中的这些变量连接外部 PostgreSQL：

```env
EXTERNAL_POSTGRES_HOST=你的PG服务器IP或域名
EXTERNAL_POSTGRES_PORT=5432
EXTERNAL_POSTGRES_DB=stockagent
EXTERNAL_POSTGRES_USER=admin
EXTERNAL_POSTGRES_PASSWORD=你的PG密码
```

如果 `stockagent` 数据库不存在，需要先在 PostgreSQL 里创建：

```bash
createdb -h 你的PG服务器IP -p 5432 -U admin stockagent
```

如果服务器没有安装 `createdb`，也可以进入 PostgreSQL 容器或使用任意 PostgreSQL 客户端执行：

```sql
create database stockagent;
```

查看状态：

```bash
docker compose ps
```

查看日志：

```bash
docker compose logs -f api
docker compose logs -f web
docker compose logs -f datasource
```

API 容器启动时会根据：

```env
Database__ApplyMigrationsOnStartup=true
```

自动执行 EF Core 数据库迁移，首次启动会创建完整表结构。

## 访问系统

浏览器打开：

```text
http://你的服务器IP
```

如果修改了 `WEB_PORT`，例如 `WEB_PORT=8080`，则访问：

```text
http://你的服务器IP:8080
```

## 首次使用配置

1. 打开 Web UI。
2. 注册一个用户。
3. 登录后进入配置页面。
4. 配置大模型：
   - Provider
   - Base URL
   - Model
   - API Key
5. 配置数据源：
   - 行情/财务 Base URL：`http://datasource:8000`
   - 行情/财务 API Key：填写 `.env` 中的 `DATA_SOURCE_API_KEY`
   - 公告/证据 Base URL：`http://datasource:8000`
   - 公告/证据 API Key：填写 `.env` 中的 `DATA_SOURCE_API_KEY`
6. 保存配置后，用 A 股或港股代码发起研究。

注意：数据源 Base URL 填 `http://datasource:8000`，这是 Docker Compose 内部网络地址，由 API 容器访问，不是浏览器直接访问的地址。

## 常用运维命令

停止服务：

```bash
docker compose down
```

重启服务：

```bash
docker compose restart
```

更新代码后重新部署：

```bash
git pull
docker compose up -d --build
```

如果使用外部 PostgreSQL，更新后执行：

```bash
git pull
docker compose -f docker-compose.yml -f docker-compose.external-pg.yml up -d --build api web datasource
```

查看数据库数据卷：

```bash
docker volume ls | grep stockagent
```

备份 PostgreSQL：

```bash
docker compose exec postgres pg_dump -U stockagent stockagent > stockagent_backup.sql
```

恢复 PostgreSQL：

```bash
cat stockagent_backup.sql | docker compose exec -T postgres psql -U stockagent stockagent
```

## 外部 PostgreSQL 注意事项

使用外部 PostgreSQL 时，不要修改 `docker-compose.yml` 里的连接串，也不要把真实密码写进仓库文件。只需要在服务器 `.env` 里填写 `EXTERNAL_POSTGRES_*` 变量，然后使用：

```bash
docker compose -f docker-compose.yml -f docker-compose.external-pg.yml up -d --build api web datasource
```

生产密码只放在服务器 `.env` 或服务器密钥管理系统里，不要提交到 GitHub。
