# 🌌 技术星图

> 本星图展示技术宇宙中所有**已发布或可安装**的核心模块。它们可独立使用，通过 **Hub** 浏览安装，通过 **项目 Adapter** 协同工作。

**集成入口**：[Tech-Cosmos Hub](./Tech-Cosmos.Hub/README.md) · `Tech-Cosmos → Hub`

---

## 🧭 集成中枢

*   **[Tech-Cosmos Hub](https://github.com/PeterParkers007/Tech-Cosmos.git?path=/Tech-Cosmos.Hub)** — **本仓库内置**
    - **核心价值**：Unity 编辑器内浏览全部框架、Git/本地导入 UPM、条件化胶水生成、一键项目结构脚手架
    - **设计哲学**：Hub **不引用**任何卫星包 asmdef，保持纯净安装器角色
    - **安装**：`https://github.com/PeterParkers007/Tech-Cosmos.git?path=/Tech-Cosmos.Hub`

---

## 🔗 解耦契约层（生态扩展）

> 以下包实现「卫星框架零互引」：只含契约与原语，跨包协作在项目 `Assets/_Game/Adapters/` 完成。随完整 Framework 集成工作区提供，独立 Git 仓库陆续发布。

| 包 | 职责 |
|----|------|
| **Core** | `Float3`、`EntityHandle`、`Blackboard` — 引擎无关原语 |
| **Event Bus** | 类型安全事件总线 |
| **Input System (Intent)** | `InputIntent` 意图流 → Handler |
| **Targeting System** | `TargetRequest` → `TargetResult` |
| **Combat System** | `DamageRequest` / 治疗契约 |

---

## 🛠️ 工具链与基础设施

### 基础设施
*   **[SerializationKit](https://github.com/PeterParkers007/Tech-Cosmos.Infra.Serialization.git)** — 配置序列化与 DataPipeline
*   **[LoggingSystem](https://github.com/PeterParkers007/Tech-Cosmos.Infra.LoggingSystem.git)** — 分级结构化日志

### 运行时工具
*   **[PoolSystem](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.PoolSystem.git)** — 对象池，消除 GC 压力
*   **[UpdateManager](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.Update.git)** — 集中式 Update 调度
*   **[AnimationLerper](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.Animation.git)** — UI 动画插值
*   **[ToolBox](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.ToolBox.git)** — 单例等杂项工具
*   **[FactionForge](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.FactionForge.git)** — 阵营关系矩阵编辑器
*   **[Init Analyzer](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.InitializationDependencyAnalyzer.git)** — 模块初始化依赖分析

### 编辑器工具
*   **[MSE (Magic String Eliminator)](https://github.com/PeterParkers007/Tech-Cosmos.Tool.MSE.git)** — 魔法字符串枚举生成

### 内容管道
*   **[AssetBundleBuilder](https://github.com/PeterParkers007/Tech-Cosmos.Pipeline.AssetBundleBuilder.git)** — AB 智能构建与依赖分析

---

## 🏛️ 框架层

*   **[SkillSystem](https://github.com/PeterParkers007/Tech-Cosmos.Framework.SkillSystem.git)** — 六层技能架构 + **内置 GBF Buff** + **Graph 编辑器**（v2.3+）
    - ⚠️ 独立 **[BuffSystem](https://github.com/PeterParkers007/Tech-Cosmos.Framework.BuffSystem.git)** 已**并入** SkillSystem，请只安装 SkillSystem
*   **[Entity-OS (ECS)](https://github.com/PeterParkers007/Tech-Cosmos.Framework.ECS.git)** — 高性能实体组件系统
*   **[Resource Manager](https://github.com/PeterParkers007/Tech-Cosmos.Framework.ResourceManager.git)** — Resources ↔ Addressables 可切换

---

## 🧱 组件层

*   **[Unit-Core](https://github.com/PeterParkers007/Tech-Cosmos.Component.UnitCore.git)** — 工业级单位核心，能力工厂 + 事件驱动
*   **[CommandSystem](https://github.com/PeterParkers007/Tech-Cosmos.Component.CommandSystem.git)** — 可组合命令队列与优先级调度
*   **[Universal Selection](https://github.com/PeterParkers007/Tech-Cosmos.Component.UniversalSelection.git)** — 框选、多选、类型安全选择
*   **[AI-OS](https://github.com/PeterParkers007/TechCosmos.Component.AI-OS.git)** — 效用理论 AI 决策框架

---

## 💻 操作系统层

### 输入（双轨并存）
| 包 | 场景 |
|----|------|
| **[Input-OS](https://github.com/PeterParkers007/Tech-Cosmos.OS.InputSystem.git)** | MOBA/RTS：`RegisterCommand(KeyCode, Action)` 命令映射 |
| **Framework Input (Intent)** | 新栈：`InputIntent` → Targeting → Skill（见解耦契约层） |

---

## 🧩 功能模块

*   **[Mouse Hover](https://github.com/PeterParkers007/TechCosmos.TMoudle.MouseHover.git)** — 空间网格高性能悬停检测
*   **[Mapping Manager](https://github.com/PeterParkers007/TechCosmos.TMoudle.MappingManager.git)** — 类型安全双向映射

---

## 🎯 规划中（尚未独立发布）

以下模块在路线图中，仓库链接将在发布时更新：

| 模块 | 说明 |
|------|------|
| **RTS-OS** | 即时战略领域内核（编队、资源、战争迷雾） |
| **WASD-OS** | 3D/2D 角色移动与摄像机方案 |
| **RTS-MOBA-Control-System** | 旗舰级 MOBA/RTS 控制验证 |
| **ARPG-Combat-Demo** | 完整 ARPG 战斗循环演示 |
| **ConfigEditor** | 可视化配置编辑器 |

---

## 📦 推荐集成架构

```
Unity 项目
├── Packages/manifest.json     ← Hub 写入 Git 依赖
├── Assets/Framework/          ← 可选：本地嵌入全部包（monorepo 工作流）
├── Assets/_Game/
│   ├── Adapters/              ← 唯一跨包胶水（手写或 Hub 生成）
│   └── Generated/Hub/         ← Hub 条件生成的 .g.cs
└── Tech-Cosmos → Hub          ← 浏览 / 导入 / 脚手架
```

---

*技术宇宙持续扩张中… 最后更新：2026年6月*
