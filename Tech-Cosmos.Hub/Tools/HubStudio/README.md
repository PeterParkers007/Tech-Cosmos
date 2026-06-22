# Hub Studio

用浏览器编辑**本仓库** `Data/` 里的配置，保存后直接 `git commit`。

与 Unity 运行时 Hub 窗口无关（但可从 Unity 菜单启动）。

## 用法

**Unity（Windows 推荐）**：`Tech-Cosmos → Hub Studio`

**命令行**：

```bash
cd Tools/HubStudio
python server.py
# 打开 http://127.0.0.1:8765
```

Windows：`start.ps1` 或 `start.bat`（自动检测端口冲突并打开浏览器）

## 功能

| 页签 | API | 编辑内容 |
|------|-----|----------|
| 技术框架 | `/api/data/catalog` | 包 id、folder、gitUrl、**dependsOn** |
| 胶水代码 | `/api/data/recipes` | template、requires、**dependsOnRecipes**、**doc**、**outputFile** |
| 项目结构 | `/api/data/structure` | presets、folders、extraRoots |
| 模板文件 | `GET/PUT/DELETE /api/templates/{name}` | `Data/Templates/*.txt` 原文 |

跨页校验（`validate.js`）：重复 id、Recipe 引用缺失模板、catalog 与 requires 交叉检查。

页脚显示 **保存到此目录**、`git status` 未提交项。

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

保存目录固定为 `Tools/HubStudio/../../Data/`（由 **server.py 文件所在位置** 决定）。

## 保存到了别的盘？

8765 端口上可能还跑着**另一份** Hub Studio（例如之前在 D 盘启动过）。`start.ps1` 现在会自动检测并重启；若仍不对：

1. 页顶查看 **「保存到此目录」** 是否为当前这份仓库
2. 任务管理器结束所有 `python.exe`（或 `netstat -ano | findstr 8765` 后结束对应 PID）
3. 在本目录重新 `.\start.ps1`

## 可选环境变量

| 变量 | 说明 |
|------|------|
| `HUB_STUDIO_PORT` | 端口，默认 8765 |
