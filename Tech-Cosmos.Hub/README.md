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

快捷菜单：`Tech-Cosmos → Hub → Generate Project Structure (Standard)`

## 功能

### 1. 技术框架

- 左侧：按分类浏览全部框架（catalog 含 Git URL）
- 右侧：README 预览、状态徽章、导入/移除
- **导入**：本地有 `Assets/Framework/<包>` 时用 `file:`；否则写入 catalog 中的 **gitUrl**

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
