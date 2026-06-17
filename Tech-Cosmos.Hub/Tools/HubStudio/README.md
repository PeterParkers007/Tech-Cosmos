# Hub Studio

在 **Tech-Cosmos.Hub 仓库**里维护 Hub 包本身用的 Web 工具，与 Unity 无关。

编辑 `Data/` 下的配置与胶水模板 → 保存到磁盘 → 在仓库根目录 `git commit` → `push`。

## 谁在用

- **维护 Hub 包的人**：在 `Tech-Cosmos.Hub`（或 monorepo 里该子目录）克隆/检出后本地运行
- **使用 Hub 的 Unity 项目**：只通过 UPM 拉取已发布的包，不需要也不应运行 Studio

## 快速开始

```powershell
# 在 Hub 仓库根目录
cd Tools/HubStudio
./start.ps1
```

或：

```bash
cd Tools/HubStudio
python server.py
# 浏览器打开 http://127.0.0.1:8765
```

Windows 也可双击 `start.bat`。

## 工作流

```
克隆 Hub 仓库
    → 运行 Hub Studio
    → 编辑框架 / 胶水 / 项目结构 / 模板
    → 「保存全部」（直接写入 Data/）
    → git add Data/
    → git commit -m "hub: 描述你的扩展"
    → git push origin main
```

Unity 侧用户更新 `com.techcosmos.hub` 依赖后即可看到新 catalog / recipes，无需在 Unity 里跑 Studio。

## 编辑内容

| 文件 | 用途 |
|------|------|
| `Data/package-catalog.json` | 技术框架列表 |
| `Data/glue-recipes.json` | 胶水 Recipe |
| `Data/project-structure.json` | 项目结构预设 |
| `Data/Templates/*.txt` | 胶水代码模板 |

## 环境变量

| 变量 | 说明 |
|------|------|
| `HUB_STUDIO_PORT` | 端口，默认 `8765` |
| `HUB_ROOT` | Hub 包根目录；默认自动解析为 `Tools/HubStudio` 上两级 |

在 monorepo 中若 Hub 不在默认相对路径，可设置：

```powershell
$env:HUB_ROOT = "D:\path\to\Tech-Cosmos.Hub"
cd Tools/HubStudio
python server.py
```

## 依赖

- Python 3（标准库即可，无 pip 依赖）
- 本机浏览器

## 胶水 Recipe 可选字段

| 字段 | 说明 |
|------|------|
| `template` | `Data/Templates/{template}.txt` |
| `outputFile` | 自定义输出 `.g.cs` 文件名 |
| `doc` | Hub 胶水详情页说明（Markdown 纯文本） |
| `needsHeroType` | 生成时是否需要 Hero 类型占位符 |
