# 🌌 技术星图

> 本星图展示了技术宇宙中的所有核心模块。它们被设计为可独立使用，也可协同工作。

## 🛠️ 工具链 - 技术宇宙的基石

### 基础设施
*   **[SerializationKit](https://github.com/PeterParkers007/Tech-Cosmos.Infra.Serialization.git)** - **序列化工具集**
    - **核心价值**：统一处理JSON、XML等格式的序列化与反序列化，为内容管道提供纯净、高效的数据基石
    - **设计哲学**：严格遵循单一职责原则，是所有需要数据持久化模块的唯一真理源

*   **[LoggingSystem](https://github.com/PeterParkers007/Tech-Cosmos.Infra.LoggingSystem.git)** - **日志系统**
    - **核心价值**：提供分级、结构化、跨平台的日志能力，是系统调试的望远镜

*   **[ToolBox](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.ToolBox.git)** - **杂项工具箱**
    - **核心价值**：专门提供一些提升效率的杂项工具

### 运行时框架
*   **[PoolSystem](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.PoolSystem.git)** - **对象池系统**
    - **核心价值**：将对象的生与死转化为内存的呼吸，彻底消除GC压力
 
*   **[UpdateManager](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.Update.git)** - **更新调度器**
    - **核心价值**：取代杂乱的MonoBehaviour.Update，实现性能可控的帧调度

*   **[AnimationLerper](https://github.com/PeterParkers007/Tech-Cosmos.Runtime.Animation.git)** - **动画插值引擎**
    - **核心价值**：通过配置与回调，将动画逻辑从代码中彻底解耦

### 内容管道
*   **[ConfigEditor](https://github.com/yourname/ConfigEditor)** - **配置编辑器**
    - **核心价值**：让策划人员在友好界面中调校游戏平衡，产出由SerializationKit处理的标准化数据文件
    - **技术依赖**：基于 `SerializationKit`

*   **[AssetBundleBuilder](https://github.com/PeterParkers007/Tech-Cosmos.Pipeline.AssetBundleBuilder.git)** - **资源打包工具**
    - **核心价值**：自动化构建、命名、依赖管理，将混乱的艺术资源转化为可发布的资产包

## 🧱 组件层 - 可复用的业务模块

### 实体组件
*   **[Unit-Core](https://github.com/PeterParkers007/Tech-Cosmos.Component.UnitCore.git)** - **游戏实体基石框架**
    - **特性**：事件驱动、状态机、数据配置化
    - **价值**：实现数据、表现、逻辑的彻底分离

### 能力组件  
*   **[CommandSystem](https://github.com/PeterParkers007/Tech-Cosmos.Component.CommandSystem.git)** - **优雅命令模式实现**
    - **特性**：模块化、可组合、支持状态追踪与优先级调度的命令执行框架
    - **价值**：将复杂行为封装为可复用、可取消、可排队的命令对象，显著提升游戏逻辑的可维护性和可扩展性

## 💻 操作系统层 - 游戏世界的通用内核

### 通用操作系统
*   **[Entity-OS](https://github.com/PeterParkers007/Tech-Cosmos.Framework.ECS.git)** - **实体操作系统**
    - **核心价值**：在Unit-Core之上，提供大规模实体的生命周期、查询、分组与阵营管理

*   **[Input-OS](https://github.com/PeterParkers007/Tech-Cosmos.OS.InputSystem.git)** - **输入操作系统**  
    - **核心价值**：将输入检测与命令执行解耦，实现可配置、类型安全的输入-动作映射系统，支持动态注册与参数化命令执行

*   **[AI-OS](https://github.com/yourname/AI-OS)** - **人工智能操作系统**
    - **核心价值**：提供通用的AI决策框架与调度器


### 领域操作系统
*   **[RTS-OS](链接)** - **即时战略操作系统**
    - **核心价值**：为RTS游戏提供编队、资源、战争迷雾等标准化内核
    - **技术栈**：基于 `Entity-OS` + `Input-OS` + `AI-OS`

*   **[WASD-OS](链接)** - **移动控制操作系统**
    - **核心价值**：为3D/2D游戏提供角色移动、摄像机、物理等完整移动方案
    - **技术栈**：基于 `Entity-OS` + `Input-OS`

## 🎯 应用系统层 - 旗舰级玩法系统

*   **[RTS-MOBA-Control-System](https://github.com/yourname/RTS-MOBA-Control-System)** - **即时战略/MOBA控制系统**
    - **核心价值**：本技术栈复杂应用能力的终极证明
    - **技术栈**：`Entity-OS` + `Input-OS` + `AI-OS` + `CommandSystem` + `AnimationLerper`
    - **特性**：智能编队、高级路径寻找、协同攻击AI


## 🚀 演示项目

*   **[ARPG-Combat-Demo](https://github.com/yourname/ARPG-Combat-Demo)** - **完整ARPG战斗循环**
    - **展示**：Unit集成、技能系统、装备系统
    - **价值**：数据驱动游戏设计的完整实践

*   **[RTS-Core-Loop](https://github.com/yourname/RTS-Core-Loop)** - **即时战略核心机制**  
    - **展示**：大规模单位控制、编队AI、资源管理
    - **价值**：验证命令系统在复杂控制中的应用

---

*技术宇宙持续扩张中... 最后更新：2025年11月8日*
