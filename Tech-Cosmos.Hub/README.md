# Tech-Cosmos Hub

**唯一集成入口**：浏览全部技术框架 → 按需导入（Git / 本地）→ 条件生成胶水 → **一键生成项目结构**。

属于 [Tech-Cosmos](https://github.com/PeterParkers007/Tech-Cosmos) 主仓库子目录，**不引用**任何卫星框架 asmdef。

## 安装

### Git URL（推荐）

Unity **Package Manager** → **+** → **Add package from git URL**：

```
https://github.com/PeterParkers007/Tech-Cosmos.git?path=/Tech-Cosmos.Hub
```

或在 `Packages/manifest.json`：

```json
"com.techcosmos.hub": "https://github.com/PeterParkers007/Tech-Cosmos.git?path=/Tech-Cosmos.Hub"
```

### 本地嵌入

```json
"com.techcosmos.hub": "file:../Assets/Framework/Tech-Cosmos.Hub"
```

## 打开

- **Hub 面板**：`Tech-Cosmos → Hub`
- **Hub Studio（Web 配置）**：`Tech-Cosmos → Hub Studio (Web)` — 编辑框架列表、胶水 Recipe、项目结构预设

项目结构、胶水生成等功能均在 Hub 面板内操作；扩展 catalog / recipes 等 JSON 请用 Hub Studio。

## 功能

### 1. 技术框架

- 左侧：**导入模式**切换（Git UPM / Assets 嵌入）
- **多选**：勾选框架 → **导入选中** / **全选可见** / **清除**
- **Git UPM**：写入 `Packages/manifest.json`（Git URL 或 `file:`）
- **Assets 嵌入**：复制/克隆到 `Assets/Package-TechCosmos/<包名>/`（自动创建目录）

### 2. 胶水生成

- Recipe 条件检查 → 生成到 `Assets/_Game/Generated/Hub/*.g.cs`
- 与手写 `Adapters/` **二选一**，避免类名冲突

### 3. 项目结构

- 预设：精简 / 标准 / 完整
- 可写 `.gitkeep`、README、asmdef 样板

### 4. Hub Studio（Web）

- 路径：`Tools/HubStudio/`
- 编辑 `package-catalog.json`、`glue-recipes.json`、`project-structure.json` 与胶水模板
- 支持新建框架、Recipe、结构预设与模板文件

## 架构文档

- [技术星图](../GALAXY.md)
- [总架构](../ARCHITECTURE.md)
- [快速启动](../GETTING-STARTED.md)

## 注意

- Framework 包放 `Assets/Framework/`（只读）或 UPM Git 依赖
- 业务代码放 `Assets/_Game/`
- 解耦契约层（Core/EventBus/Input/Targeting/Combat）随完整集成工作区提供
