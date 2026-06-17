# 🚀 快速启动指南

欢迎来到技术宇宙！本指南帮助不同背景的访客找到最高效的探索路径。

## ⚡ 最快上手：安装 Hub

1. 打开 Unity 项目 → **Package Manager** → **+** → **Add package from git URL**
2. 粘贴：
   ```
   https://github.com/PeterParkers007/Tech-Cosmos.git?path=/Tech-Cosmos.Hub
   ```
3. 菜单 **Tech-Cosmos → Hub** 打开集成中枢
4. 在「技术框架」页选择需要的包 → **导入**（支持 Git URL 或本地 `Assets/Framework` 目录）
5. 可选：「胶水生成」页按条件生成 Adapter 样板；「项目结构」页一键创建 `_Game` 目录

详见 [Tech-Cosmos.Hub/README.md](./Tech-Cosmos.Hub/README.md)

---

## 🎯 面向不同访客的专属路径

### 技术总监 / 架构师 / 招聘者
1. [构建者宣言](./MANIFESTO.md) — 5 分钟了解工程价值观
2. [总架构图](./ARCHITECTURE.md) — 零耦合 + Adapter 集成模型
3. [技术星图](./GALAXY.md) — 全部卫星仓库与职责
4. 深入 **SkillSystem**、**Unit-Core**、**ECS** 独立仓库阅读 README 与代码

### 开发者 / 技术合作者
1. 安装 [Hub](./Tech-Cosmos.Hub/README.md)，按需导入包
2. [技术星图](./GALAXY.md) 按需求选型：
   - **性能** → PoolSystem, UpdateManager
   - **架构** → Unit-Core, CommandSystem, Core 契约层
   - **战斗技能** → SkillSystem（含 Buff）+ Targeting + Combat Adapter
   - **AI** → AI-OS
   - **输入** → Input-OS（命令映射）或 Framework Input（Intent 流）
3. 在 `Assets/_Game/Adapters/` 完成跨包集成（或 Hub 生成胶水）

### 同行 / 共建者
1. [构建者宣言](./MANIFESTO.md) + [时间线](./TIMELINE.md)
2. [贡献指南](./CONTRIBUTING.md)

---

## 🗺️ 推荐探索顺序

### 30 分钟
1. [构建者宣言](./MANIFESTO.md) *(5 分钟)*
2. [总架构图](./ARCHITECTURE.md) *(10 分钟)*
3. [技术星图](./GALAXY.md) 核心价值部分 *(15 分钟)*

### 2 小时
1. 宣言 + 架构 + 星图完整阅读 *(70 分钟)*
2. 安装 Hub，导入 PoolSystem + Unit-Core 试用 *(30 分钟)*
3. [发展路线图](./ROADMAP.md) *(20 分钟)*

---

## 🛠️ 环境准备

- **Unity**: 2022.3 LTS 或更高
- **.NET**: 4.x+
- **平台**: Windows / macOS / Linux

## 📦 两种包管理方式

| 方式 | 适用场景 |
|------|----------|
| **Hub + Git URL** | 按需安装，manifest 由 Hub 管理（推荐） |
| **本地 monorepo** | 全部包放在 `Assets/Framework/`，manifest 使用 `file:` 路径 |

---

*探索愉快！疑问请通过 [联系我们](./LINKS.md) 发起讨论。*
