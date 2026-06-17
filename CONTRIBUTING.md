# 贡献指南

感谢关注 Tech-Cosmos！本仓库包含**文档导航**与 **Hub 编辑器包**（`Tech-Cosmos.Hub/`）。

## 可以贡献什么

| 类型 | 位置 | 说明 |
|------|------|------|
| 文档修正 | 根目录 `*.md` | 星图链接、架构描述、错别字 |
| Hub 功能 | `Tech-Cosmos.Hub/Editor/` | UI、catalog、胶水模板 |
| 卫星包代码 | **各独立仓库** | 见 [GALAXY.md](./GALAXY.md) |

## 提交流程

1. Fork [Tech-Cosmos](https://github.com/PeterParkers007/Tech-Cosmos)
2. 创建分支：`docs/xxx` 或 `hub/xxx`
3. 提交 PR，说明改动动机
4. 卫星包功能请在对应仓库 PR，勿混入本仓库

## Hub 开发说明

- 程序集：`TechCosmos.Hub.Editor`（仅 Editor）
- **禁止** Hub 引用 SkillSystem / ECS 等卫星 asmdef
- 包清单：`Tech-Cosmos.Hub/Data/package-catalog.json`
- 本地测试：将 `Tech-Cosmos.Hub` 以 `file:` 路径加入 Unity 项目 manifest

## 文档约定

- 中文为主，关键技术词保留英文
- 新卫星包发布时同步更新 `GALAXY.md` 与 Hub catalog
- BuffSystem 已并入 SkillSystem，勿再单独列入星图

## 行为准则

尊重、建设性讨论。Issue 请提供复现步骤与环境（Unity 版本、包版本）。

---

*你的思想可以成为技术宇宙的一部分。*
