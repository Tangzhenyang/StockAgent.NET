# 用户系统、个人配置、历史报告与进度样式设计

## 背景

当前 StockAgent.NET 已经具备股票研究任务提交、后台流水线、报告生成、证据查看和 PDF 导出能力，但仍然是单用户原型：

- 后端没有认证和授权，所有 API 默认公开。
- 研究任务没有用户归属字段，任何调用方都能看到所有任务。
- `AppSetting` 目前是全局 JSON 配置，不能支持“每个用户自行配置”。
- 前端只有静态设置摘要，没有可编辑的大模型 API、证据上限、数据源等配置页面。
- 研究进度条使用类似按钮的块状样式，容易让用户误以为这些阶段可以点击。

本设计目标是在保留当前技术栈和业务结构的基础上，升级为可自部署的多用户 Web 应用。

## 目标

1. 新增内置用户系统，支持注册、登录、退出、查看当前登录用户。
2. 登录状态需要持久保存，用户刷新页面或重新打开浏览器后不需要每次重新登录。
3. 每个用户可以独立配置大模型 API Key、Base URL、模型名称、报告语言、证据上限和数据源相关配置。
4. 用户配置保存到 PostgreSQL 数据库，其中 API Key 加密后保存。
5. 用户只能查看和操作自己的研究任务、报告、证据和 PDF 导出记录。
6. 新增历史记录入口，用户可以查看已完成研究任务并打开历史报告。
7. 修改研究阶段样式，明确表达为状态进度而不是按钮。
8. 新增和修改的代码继续补充完整注释，保持当前项目中中英文说明并存的风格。

## 非目标

- 首版不接入第三方登录，例如 GitHub、Microsoft、Google。
- 首版不做管理员后台、用户封禁、角色权限或组织空间。
- 首版不支持用户读取历史 API Key 明文，只允许保存或替换。
- 首版不实现复杂计费、调用额度和审计报表。
- 首版不把数据源扩展成完整插件市场，只保留可配置结构和未来扩展点。

## 推荐方案

采用 ASP.NET Core Identity + HttpOnly Cookie 作为认证基础。

该方案适合当前自部署场景：后端管理登录态，浏览器通过安全 Cookie 自动携带会话，前端不需要把 token 存入 `localStorage`。刷新页面、重新打开浏览器后，只要 Cookie 未过期，用户仍然保持登录。由于 Cookie 是 HttpOnly，前端脚本不能直接读取登录凭据，安全边界更清晰。

## 技术栈

- 后端：.NET 10、ASP.NET Core Minimal API、ASP.NET Core Identity、Entity Framework Core、Npgsql、PostgreSQL、Semantic Kernel。
- 前端：React、TypeScript、Vite、TanStack Query、React Router。
- 数据库：PostgreSQL，继续兼容已启用 pgvector 的部署环境。
- 迁移：EF Core Migrations，替代当前开发期的 `EnsureCreated` 表结构初始化方式。
- 密钥保护：ASP.NET Core Data Protection 或等价的后端加密服务，用于加密数据库中的用户 API Key。

## 后端设计

### 用户模型

新增 `ApplicationUser`，继承 ASP.NET Core Identity 的用户模型。

每个用户至少包含：

- 用户 ID。
- 用户名或邮箱。
- 密码哈希。
- 创建时间。
- 更新时间。

所有与用户相关的实体都通过 `UserId` 绑定当前用户。

### 认证接口

新增 `/api/auth` 分组：

- `POST /api/auth/register`：注册用户。
- `POST /api/auth/login`：登录用户并写入持久 Cookie。
- `POST /api/auth/logout`：退出登录并清除 Cookie。
- `GET /api/auth/me`：返回当前登录用户信息。

登录接口默认启用持久登录 Cookie。首版可以固定 30 天有效期，后续再增加“记住我”开关。

### 授权规则

除健康检查、登录、注册外，业务 API 默认要求登录。

需要受保护的 API 包括：

- 研究任务。
- 报告读取。
- 证据读取。
- PDF 导出。
- 用户配置。
- 数据源健康和模型连接测试。

接口内部必须使用当前登录用户 ID 过滤数据，不能只依赖前端传参。

### 数据模型调整

`ResearchTask` 增加：

- `UserId`：当前任务所属用户。

与任务关联的数据仍然通过 `ResearchTaskId` 关联，包括：

- `ResearchReport`。
- `EvidenceCard`。
- `DocumentSource`。
- `DocumentChunk`。
- `ResearchStep`。
- `PdfExport`。
- `ModelInvocation`。

读取这些关联数据时，需要先确认任务属于当前用户，或在查询中通过任务归属过滤。

### 用户配置模型

建议新增 `UserSetting`，而不是复用全局 `AppSetting`。

字段包括：

- `Id`：设置记录 ID。
- `UserId`：所属用户。
- `SettingKey`：设置键，例如 `model`, `research`, `dataSources`。
- `SettingValueJson`：配置 JSON。
- `UpdatedAt`：更新时间。

可以继续保留 `AppSetting` 用于未来系统级全局设置。

### 模型配置

模型配置 JSON 首版包含：

- `provider`：模型提供商，例如 `OpenAICompatible`、`OpenAI`、`AzureOpenAI`。
- `baseUrl`：兼容 OpenAI 的 API 地址。
- `model`：模型名称。
- `encryptedApiKey`：加密后的 API Key，仅后端使用。
- `apiKeyConfigured`：派生状态，返回给前端时只表示是否已配置。
- `updatedAt`：配置更新时间。

前端保存模型配置时：

- 如果传入新 API Key，后端加密并替换旧密钥。
- 如果 API Key 字段为空，后端保留旧密钥。
- 后端响应不返回 API Key 明文，也不返回加密密文。

### 研究配置

研究配置 JSON 首版包含：

- `defaultLanguage`：默认报告语言，首版默认 `zh-CN`。
- `maxEvidenceCards`：证据上限。
- `maxDocumentChunks`：文档分块上限。
- `maxRetrievedChunks`：进入模型上下文的检索片段上限。
- `retainRawDocuments`：是否保存原始文档全文，首版可默认 `false` 以节省磁盘。

研究流水线创建任务时读取当前用户配置：

- `Language` 默认来自用户配置。
- 证据筛选上限来自用户配置，而不是固定 `30`。

### 用户配置接口

新增 `/api/user-settings` 分组：

- `GET /api/user-settings`：返回当前用户的配置摘要。
- `PUT /api/user-settings/model`：保存模型配置。
- `PUT /api/user-settings/research`：保存研究配置。
- `POST /api/user-settings/model/test`：使用当前配置测试模型连接。

所有接口都只读写当前登录用户的配置。

### 历史记录接口

研究任务列表接口改为只返回当前用户任务。

新增或扩展查询参数：

- `GET /api/research-tasks?status=completed`：返回当前用户已完成任务。
- `GET /api/research-tasks/{id}`：只允许读取自己的任务。
- `GET /api/research-tasks/{id}/report`：只允许读取自己的报告。
- `GET /api/research-tasks/{id}/evidence`：只允许读取自己的证据。

历史记录页面使用这些接口展示已完成研究报告。

### 数据库迁移

当前项目在开发环境使用 `EnsureCreated`。新增 Identity 表和用户归属字段后，应切换到 EF Core Migrations。

迁移内容包括：

- Identity 用户表。
- `UserSettings` 表。
- `ResearchTasks.UserId` 字段和索引。
- 必要的外键关系。
- 现有单用户数据的迁移策略。

如果当前库中已有旧任务，首版可以在迁移后要求重新创建测试数据，或提供一次性默认用户归属迁移脚本。由于当前项目仍处于早期阶段，推荐优先保证新结构干净可靠。

## 前端设计

### 应用路由

新增路由结构：

- `/login`：登录页。
- `/register`：注册页。
- `/`：研究工作台。
- `/history`：历史报告。
- `/settings`：用户设置。

前端启动时调用 `/api/auth/me`：

- 如果已登录，进入应用主界面。
- 如果未登录，跳转到登录页。

所有请求使用 `credentials: 'include'`，让浏览器自动携带 Cookie。

### 登录体验

登录成功后进入研究工作台。

刷新页面时，前端重新调用 `/api/auth/me` 恢复当前用户，不要求用户重新输入账号密码。

页面导航显示：

- 当前用户名。
- 工作台入口。
- 历史记录入口。
- 设置入口。
- 退出登录按钮。

### 设置页面

设置页面分为三个区域：

1. 模型配置。
2. 研究配置。
3. 数据源配置。

模型配置区域包含：

- 提供商选择。
- Base URL。
- 模型名称。
- API Key 密码输入框。
- 已配置状态。
- 保存按钮。
- 测试连接按钮。

研究配置区域包含：

- 报告语言。
- 证据上限。
- 文档分块上限。
- 检索片段上限。
- 是否保留原始文档。

数据源配置区域首版可以先显示当前数据源状态和预留配置项。

### 历史记录页面

历史记录页面展示当前用户已完成任务：

- 股票代码。
- 市场。
- 状态。
- 创建时间。
- 更新时间。
- 报告语言。
- 查看报告入口。
- PDF 导出入口。

点击历史项后，打开对应报告和证据。

### 进度样式

`TaskTimeline` 保留为非交互状态组件，但样式改为 Stepper：

- 使用圆点表示阶段。
- 使用连接线表示阶段顺序。
- 已完成阶段使用完成色。
- 当前阶段使用强调色。
- 未开始阶段使用灰色。
- 失败阶段显示错误色。
- 不使用按钮边框、hover 指针或点击态。

语义上继续使用有序列表，并为当前阶段增加可访问性标记，例如 `aria-current="step"`。

## 安全设计

- 密码只保存 Identity 的密码哈希。
- API Key 加密后保存到数据库。
- API Key 不返回给前端明文。
- 登录 Cookie 使用 HttpOnly。
- 生产环境启用 Secure Cookie。
- Cookie SameSite 配置根据部署方式决定；如果前后端同域反代，优先使用更严格的 SameSite。
- 所有用户数据查询都必须绑定当前用户 ID。
- 所有写入配置接口都必须校验输入长度和必填字段。
- 测试连接接口只返回成功或失败摘要，不返回敏感请求细节。

## 错误处理

- 未登录访问受保护 API 返回 401。
- 访问其他用户数据返回 404，避免暴露资源存在性。
- 配置缺失时，创建研究任务前返回清晰错误，提示先完成模型配置。
- API Key 解密失败时，要求用户重新保存 API Key。
- 模型连接测试失败时，返回可读错误摘要。
- 历史报告不存在时，返回 404。

## 测试策略

后端测试：

- 注册、登录、退出、获取当前用户。
- 未登录访问受保护接口返回 401。
- 用户只能看到自己的任务。
- 用户不能读取其他用户报告、证据和 PDF。
- 用户设置保存和读取。
- API Key 保存后不以明文返回。
- 证据上限从用户配置生效。

前端测试：

- 未登录显示登录页。
- 登录后显示工作台。
- 刷新后通过 `/api/auth/me` 恢复登录态。
- 设置页可以保存配置并显示已配置状态。
- 历史页只展示完成任务。
- 进度条不表现为可点击按钮。

手动验证：

- PostgreSQL 迁移成功。
- 注册新用户后可保存模型配置。
- 创建研究任务后历史记录可查看报告。
- 退出登录后无法访问业务 API。
- 重新打开浏览器后仍保持登录。

## 实施顺序

1. 引入 ASP.NET Core Identity 并配置 Cookie 认证。
2. 新增用户实体、用户配置实体和 EF Core 迁移。
3. 新增认证 API。
4. 新增用户配置 API 和 API Key 加密服务。
5. 为研究任务绑定 `UserId`，并给报告、证据、PDF 接口增加用户隔离。
6. 调整研究流水线读取当前用户研究配置。
7. 前端新增认证 API 封装和登录态恢复逻辑。
8. 前端新增登录、注册、设置、历史页面。
9. 改造工作台导航和请求凭据。
10. 修改 `TaskTimeline` 样式为 Stepper。
11. 补充后端和前端测试。
12. 在 PostgreSQL 环境执行迁移并完成端到端验证。

## 验收标准

- 新用户可以注册并登录。
- 登录状态在刷新页面后仍然保留。
- 用户可以保存自己的模型配置和研究配置。
- API Key 保存到数据库，但前端无法读取明文。
- 用户创建的研究任务只属于自己。
- 历史记录只显示当前用户已完成报告。
- 用户不能通过 URL 访问其他用户的任务、报告、证据或 PDF。
- 进度条视觉上明确是状态，不再像按钮。
- 新增和修改的代码包含完整、清晰的注释。
- 后端测试和前端构建通过。

## 上线前部署事项

当前设计已按“内置账号密码登录 + 持久 Cookie + 用户配置入库 + API Key 加密存储”的方向收敛。

以下事项不阻塞本次本地开发和测试，但如果后续要上线公网，需要在部署前进一步确认：

- 前后端是否同域部署。
- Cookie 的域名和 HTTPS 策略。
- Data Protection 密钥持久化位置。
- 是否开放公开注册，还是只允许管理员创建用户。
