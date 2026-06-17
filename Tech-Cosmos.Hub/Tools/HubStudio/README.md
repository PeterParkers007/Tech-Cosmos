# Hub Studio

Web 端配置工具，用于扩展 **Tech-Cosmos Hub** 的三类数据：

| 数据文件 | 用途 |
|---------|------|
| `Data/package-catalog.json` | 技术框架列表（包 ID、目录、分类、Git URL） |
| `Data/glue-recipes.json` | 胶水 Recipe（依赖、模板、输出路径、文档） |
| `Data/project-structure.json` | 项目结构预设（folders、extraRoots） |
| `Data/Templates/*.txt` | 胶水代码生成模板 |

Unity Hub 面板读取上述 JSON；**Hub Studio 负责编辑它们**。

## 启动

### 方式一：Unity 菜单（推荐）

`Tech-Cosmos → Hub Studio (Web)`

自动启动本地服务并打开浏览器（需本机已安装 **Python 3**）。

### 方式二：PowerShell

```powershell
cd Tools/HubStudio
./start.ps1
```

### 方式三：手动

```bash
cd Tools/HubStudio
python server.py
# 浏览器打开 http://127.0.0.1:8765
```

## 使用流程

1. 在对应标签页选择或 **+ 新建** 条目
2. 编辑字段后点 **应用修改**
3. 右侧 **校验** 面板查看错误/警告
4. 点 **保存全部** 写回 `Data/` 目录
5. 回到 Unity Hub，点 **刷新** 加载新配置

## 胶水 Recipe 扩展字段

| 字段 | 说明 |
|------|------|
| `template` | 对应 `Data/Templates/{template}.txt` |
| `outputFile` | 可选，自定义输出 `.g.cs` 文件名 |
| `doc` | 可选，Hub 胶水详情页的 Markdown 说明 |
| `needsHeroType` | 是否在 Hub 显示 Hero 类型配置 |

## 离线模式

若未启动 `server.py`，可点击顶部 **离线** 状态导入 JSON 文件预览/编辑，但无法直接写回磁盘（需手动保存导出的文件）。

## 端口

默认 `8765`，可通过环境变量 `HUB_STUDIO_PORT` 修改。
