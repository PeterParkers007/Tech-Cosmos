# Tech-Cosmos Hub

**唯一集成入口**：浏览全部技术框架 → 按需导入（Git / 本地）→ 条件生成胶水 → **一键生成项目结构**。

属于 [Tech-Cosmos](https://github.com/PeterParkers007/Tech-Cosmos) 主仓库子目录，亦可作为 [独立 UPM 仓库](https://github.com/PeterParkers007/Tech-Cosmos.Hub) 安装。**不引用**任何卫星框架 asmdef。

## 安装

### Git URL — 独立仓库（推荐同步源）

```
https://github.com/PeterParkers007/Tech-Cosmos.Hub.git
```

### Git URL — 主仓库子路径

```
https://github.com/PeterParkers007/Tech-Cosmos.git?path=/Tech-Cosmos.Hub
```

或在 `Packages/manifest.json`：

```json
"com.techcosmos.hub": "https://github.com/PeterParkers007/Tech-Cosmos.Hub.git"
```

### 本地嵌入

```json
"com.techcosmos.hub": "file:../Assets/Framework/Tech-Cosmos.Hub"
```

## 打开

| 菜单 | 说明 |
|------|------|
| `Tech-Cosmos → Hub` | Unity 集成面板（框架 / 胶水 / 项目结构） |
| `Tech-Cosmos → Hub Studio` | 浏览器编辑 `Data/`（需本机 Python 3） |
| `Tech-Cosmos → 诊断 Hub 安装` | 检查 Catalog / USS 加载状态 |

## 维护本仓库数据（Hub Studio）

**方式一（推荐）**：Unity 菜单 `Tech-Cosmos → Hub Studio`（Windows 自动调用 `start.ps1`）

**方式二**：命令行

```bash
cd Tools/HubStudio && python server.py
# 或 Windows: .\start.ps1
```

Hub Studio 提供四个编辑页签：

1. **技术框架** — `package-catalog.json`（含 `dependsOn` 依赖链）
2. **胶水代码** — `glue-recipes.json`（含 `doc` / `outputFile` / `dependsOnRecipes`）
3. **项目结构** — `project-structure.json`
4. **模板文件** — `Data/Templates/*.txt`

保存后：

```bash
git add Data/
git commit -m "hub: update catalog/recipes"
git push origin main
```

详见 [Tools/HubStudio/README.md](Tools/HubStudio/README.md)。

## 功能

### 1. 技术框架

- 左侧：**导入模式**切换（Git UPM / Assets 嵌入）
- **多选** + **依赖解析**：自动补齐 `dependsOn` 链
- **Git UPM**：写入 `Packages/manifest.json`
- **Assets 嵌入**：复制/克隆到 `Assets/Package-TechCosmos/<包名>/`
- README 预览（Markdown 渲染、shields 徽章、表格）

### 2. 胶水生成

- Recipe 条件检查 → 生成到 `Assets/_Game/Generated/Hub/*.g.cs`
- 与手写 `Adapters/` **二选一**，避免类名冲突

### 3. 项目结构

- 预设：精简 / 标准 / 完整
- 可写 `.gitkeep`、README、asmdef 样板

## v1.2.0 更新摘要

- Hub Studio 完整纳入包内（Python 服务 + Web UI + 启动脚本）
- Unity 菜单一键启动 Hub Studio
- 包依赖解析（`dependsOn`）与批量导入
- README Markdown 渲染与 UI 交互优化
- 所有 Unity 资源附带完整 `.meta`

## 架构文档

- [技术星图](../GALAXY.md)（主仓库）
- [总架构](../ARCHITECTURE.md)（主仓库）

## 注意

- Framework 包放 `Assets/Framework/`（只读）或 UPM Git 依赖
- 业务代码放 `Assets/_Game/`
- 解耦契约层（Core/EventBus/Input/Targeting/Combat）随完整集成工作区提供
