# {{PROJECT_NAME}}

> 本目录由 **Tech-Cosmos Hub → 项目结构** 生成。  
> **Framework 包** 位于 `Assets/Framework/`（只读，勿改业务逻辑）。

---

## 目录说明

| 路径 | 用途 |
|------|------|
| `Adapters/` | **唯一**跨框架集成层（Input → Target → Skill → Combat） |
| `Scripts/` | 游戏业务代码（Unit、Mechanism、Condition、System） |
| `Resources/` | ScriptableObject、配置、可 `Resources.Load` 的资产 |
| `Art/` | 美术资源（模型、贴图、材质、特效、UI 图） |
| `Audio/` | 音频（BGM、SFX） |
| `Prefabs/` | 预制体 |
| `Scenes/` | 场景 |
| `Generated/` | 代码生成输出（Hub 胶水、SkillSystem Generate ALL） |
| `Editor/` | 项目 Editor 扩展脚本 |

---

## 解耦原则

1. **Framework 包之间互不认识** — 集成只在 `Adapters/`  
2. **技能系统只管释放** — 按键/选目标在 Adapter 填 `SkillContext`  
3. **`Generated/` 建议 gitignore** — 可重新生成

---

## 快速开始

1. `Tech-Cosmos → Hub` → **胶水生成** → 生成 Adapter 样板（或与手写 Adapters 二选一）  
2. `Tech-Cosmos → SkillSystem → Generator → Generate ALL Classes`  
3. 在 `Scenes/` 搭建场景，Play

---

**Tech-Cosmos** — 框架解耦，业务在 `_Game`。
