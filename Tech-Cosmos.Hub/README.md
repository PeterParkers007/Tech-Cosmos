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

`Tech-Cosmos → Hub`

项目结构、胶水生成等功能均在 Hub 面板内操作。

## 维护 Hub 包（与 Unity 无关）

在 **Tech-Cosmos.Hub 仓库**中扩展框架列表、胶水 Recipe、项目结构：

```powershell
cd Tools/HubStudio
./start.ps1
```

编辑并保存后，于仓库根目录提交：

```bash
git add Data/
git commit -m "hub: 描述扩展"
git push origin main
```

详见 [Tools/HubStudio/README.md](Tools/HubStudio/README.md)。

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

## 架构文档

- [技术星图](../GALAXY.md)
- [总架构](../ARCHITECTURE.md)
- [快速启动](../GETTING-STARTED.md)

## 注意

- Framework 包放 `Assets/Framework/`（只读）或 UPM Git 依赖
- 业务代码放 `Assets/_Game/`
- 解耦契约层（Core/EventBus/Input/Targeting/Combat）随完整集成工作区提供
