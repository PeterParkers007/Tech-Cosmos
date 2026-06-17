# Hub Studio

用浏览器编辑**本仓库** `Data/` 里的配置，保存后直接 `git commit`。

与 Unity、运行时 Hub 窗口无关。

## 用法

```bash
cd Tools/HubStudio
python server.py
# 打开 http://127.0.0.1:8765
```

Windows：`start.ps1` 或 `start.bat`

## 改什么

| 文件 | 内容 |
|------|------|
| `Data/package-catalog.json` | 技术框架列表 |
| `Data/glue-recipes.json` | 胶水 Recipe |
| `Data/project-structure.json` | 项目结构预设 |
| `Data/Templates/*.txt` | 胶水模板 |

## 路径

保存目录固定为：

```
Tools/HubStudio/../../Data/
```

即**你当前这份仓库**里的 `Data/`，没有别的逻辑。

## 提交流程

1. 编辑 → **保存全部**
2. `git add Data/`（若在 monorepo 子目录里，页脚会显示实际前缀，如 `Tech-Cosmos.Hub/Data/`）
3. `git commit` → `push`

## 可选环境变量

| 变量 | 说明 |
|------|------|
| `HUB_STUDIO_PORT` | 端口，默认 8765 |
| `HUB_ROOT` | 仅调试时覆盖包根路径 |
