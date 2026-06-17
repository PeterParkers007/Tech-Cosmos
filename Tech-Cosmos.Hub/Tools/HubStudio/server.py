#!/usr/bin/env python3
"""Hub Studio — 编辑本仓库 Data/ 下的 JSON 与模板文件。"""

from __future__ import annotations

import json
import os
import subprocess
import sys
from http.server import HTTPServer, SimpleHTTPRequestHandler
from urllib.parse import unquote, urlparse

PORT = int(os.environ.get("HUB_STUDIO_PORT", "8765"))
STUDIO_ROOT = os.path.dirname(os.path.abspath(__file__))


def resolve_hub_root() -> str:
    """本包根目录 = 本脚本所在 Tools/HubStudio 的上两级。"""
    if os.environ.get("HUB_ROOT"):
        return os.path.abspath(os.environ["HUB_ROOT"])
    return os.path.abspath(os.path.join(STUDIO_ROOT, "..", ".."))


HUB_ROOT = resolve_hub_root()
DATA_DIR = os.path.join(HUB_ROOT, "Data")
TEMPLATES_DIR = os.path.join(DATA_DIR, "Templates")

DATA_FILES = {
    "catalog": os.path.join(DATA_DIR, "package-catalog.json"),
    "recipes": os.path.join(DATA_DIR, "glue-recipes.json"),
    "structure": os.path.join(DATA_DIR, "project-structure.json"),
}


def json_response(handler: SimpleHTTPRequestHandler, status: int, payload: dict) -> None:
    body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    handler.send_response(status)
    handler.send_header("Content-Type", "application/json; charset=utf-8")
    handler.send_header("Content-Length", str(len(body)))
    handler.send_header("Access-Control-Allow-Origin", "*")
    handler.end_headers()
    handler.wfile.write(body)


def read_json(path: str) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def write_json(path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")


def git_toplevel() -> str | None:
    try:
        result = subprocess.run(
            ["git", "rev-parse", "--show-toplevel"],
            cwd=HUB_ROOT,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=8,
            check=False,
        )
    except (OSError, subprocess.TimeoutExpired):
        return None
    if result.returncode != 0 or not result.stdout:
        return None
    path = result.stdout.strip()
    return os.path.abspath(path) if path else None


def is_git_repo() -> bool:
    return git_toplevel() is not None


def git_remote_url() -> str | None:
    top = git_toplevel()
    if not top:
        return None
    try:
        result = subprocess.run(
            ["git", "remote", "get-url", "origin"],
            cwd=top,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=8,
            check=False,
        )
    except (OSError, subprocess.TimeoutExpired):
        return None
    if result.returncode != 0:
        return None
    url = result.stdout.strip()
    return url or None


def hub_path_in_git() -> str | None:
    """Hub 包目录相对于 Git 仓库根的路径，空字符串表示 Hub 即仓库根。"""
    top = git_toplevel()
    if not top:
        return None
    hub = os.path.normcase(os.path.normpath(HUB_ROOT))
    top_n = os.path.normcase(os.path.normpath(top))
    if hub == top_n:
        return ""
    if hub.startswith(top_n + os.sep):
        return os.path.relpath(HUB_ROOT, top).replace("\\", "/")
    return None


def git_data_prefix() -> str:
    """git add / status 时使用的 Data 路径前缀（相对 Git 仓库根）。"""
    rel = hub_path_in_git()
    if rel is None:
        return "Data/"
    return f"{rel}/Data/" if rel else "Data/"


def build_warnings() -> list[str]:
    if git_toplevel() is None:
        return ["未检测到 Git 仓库；保存仍有效，但无法用 git 跟踪变更。"]
    return []


def git_porcelain() -> list[str]:
    top = git_toplevel()
    if not top:
        return []
    data_prefix = git_data_prefix()
    try:
        result = subprocess.run(
            ["git", "status", "--porcelain", data_prefix],
            cwd=top,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=8,
            check=False,
        )
    except (OSError, subprocess.TimeoutExpired):
        return []
    if result.returncode != 0:
        return []
    return [line for line in result.stdout.splitlines() if line.strip()]


def build_meta() -> dict:
    top = git_toplevel()
    rel = hub_path_in_git()
    return {
        "hubRoot": HUB_ROOT,
        "studioRoot": STUDIO_ROOT,
        "gitTopLevel": top,
        "hubPathInGit": rel if rel is not None else None,
        "gitDataPrefix": git_data_prefix(),
        "gitRemote": git_remote_url(),
        "dataDir": DATA_DIR,
        "templatesDir": TEMPLATES_DIR,
        "port": PORT,
        "isGitRepo": top is not None,
        "warnings": build_warnings(),
    }


class HubStudioHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=STUDIO_ROOT, **kwargs)

    def log_message(self, format: str, *args) -> None:
        sys.stderr.write("[HubStudio] %s - %s\n" % (self.address_string(), format % args))

    def end_headers(self) -> None:
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, PUT, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        super().end_headers()

    def do_OPTIONS(self) -> None:
        self.send_response(204)
        self.end_headers()

    def do_GET(self) -> None:
        parsed = urlparse(self.path)
        path = parsed.path

        if path == "/api/meta":
            json_response(self, 200, build_meta())
            return

        if path == "/api/git-status":
            meta = build_meta()
            json_response(self, 200, {
                "isGitRepo": meta["isGitRepo"],
                "gitTopLevel": meta["gitTopLevel"],
                "hubRoot": meta["hubRoot"],
                "gitDataPrefix": meta["gitDataPrefix"],
                "changed": git_porcelain(),
                "warnings": meta["warnings"],
            })
            return

        for key, file_path in DATA_FILES.items():
            if path == f"/api/data/{key}":
                if not os.path.isfile(file_path):
                    json_response(self, 404, {"error": f"missing file: {file_path}"})
                    return
                json_response(self, 200, read_json(file_path))
                return

        if path == "/api/templates":
            items = []
            if os.path.isdir(TEMPLATES_DIR):
                for name in sorted(os.listdir(TEMPLATES_DIR)):
                    if not name.endswith(".txt"):
                        continue
                    full = os.path.join(TEMPLATES_DIR, name)
                    items.append({
                        "name": name[:-4],
                        "fileName": name,
                        "size": os.path.getsize(full),
                    })
            json_response(self, 200, {"templates": items})
            return

        if path.startswith("/api/templates/"):
            name = unquote(path[len("/api/templates/"):])
            if not name or "/" in name or "\\" in name or ".." in name:
                json_response(self, 400, {"error": "invalid template name"})
                return
            file_path = os.path.join(TEMPLATES_DIR, name + ".txt")
            if not os.path.isfile(file_path):
                json_response(self, 404, {"error": "template not found"})
                return
            with open(file_path, "r", encoding="utf-8") as f:
                content = f.read()
            json_response(self, 200, {"name": name, "content": content})
            return

        super().do_GET()

    def do_PUT(self) -> None:
        parsed = urlparse(self.path)
        path = parsed.path
        length = int(self.headers.get("Content-Length", "0"))
        raw = self.rfile.read(length) if length else b"{}"

        try:
            payload = json.loads(raw.decode("utf-8"))
        except json.JSONDecodeError:
            json_response(self, 400, {"error": "invalid JSON body"})
            return

        for key, file_path in DATA_FILES.items():
            if path == f"/api/data/{key}":
                if not isinstance(payload, dict):
                    json_response(self, 400, {"error": "expected JSON object"})
                    return
                try:
                    write_json(file_path, payload)
                except OSError as exc:
                    json_response(self, 500, {"error": str(exc)})
                    return
                json_response(self, 200, {"ok": True, "path": file_path})
                return

        if path.startswith("/api/templates/"):
            name = unquote(path[len("/api/templates/"):])
            if not name or "/" in name or "\\" in name or ".." in name:
                json_response(self, 400, {"error": "invalid template name"})
                return
            content = payload.get("content")
            if not isinstance(content, str):
                json_response(self, 400, {"error": "content must be a string"})
                return
            file_path = os.path.join(TEMPLATES_DIR, name + ".txt")
            try:
                os.makedirs(TEMPLATES_DIR, exist_ok=True)
                with open(file_path, "w", encoding="utf-8", newline="\n") as f:
                    f.write(content)
            except OSError as exc:
                json_response(self, 500, {"error": str(exc)})
                return
            json_response(self, 200, {"ok": True, "path": file_path})
            return

        json_response(self, 404, {"error": "not found"})


def main() -> int:
    if not os.path.isdir(DATA_DIR):
        print(f"ERROR: Hub Data directory not found: {DATA_DIR}", file=sys.stderr)
        print("请在 Tech-Cosmos.Hub 包内运行（Tools/HubStudio 的上两级应含 Data/）。", file=sys.stderr)
        return 1

    server = HTTPServer(("127.0.0.1", PORT), HubStudioHandler)
    print(f"Hub Studio  http://127.0.0.1:{PORT}")
    print(f"Data        {DATA_DIR}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
