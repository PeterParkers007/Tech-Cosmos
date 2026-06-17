#!/usr/bin/env python3
"""Tech-Cosmos Hub Studio — 在 Hub 仓库根目录编辑 Data/，保存后直接 git commit。"""

from __future__ import annotations

import json
import os
import subprocess
import sys
from http.server import HTTPServer, SimpleHTTPRequestHandler
from urllib.parse import unquote, urlparse

PORT = int(os.environ.get("HUB_STUDIO_PORT", "8765"))
STUDIO_ROOT = os.path.dirname(os.path.abspath(__file__))
HUB_ROOT = os.path.abspath(
    os.environ.get("HUB_ROOT") or os.path.join(STUDIO_ROOT, "..", "..")
)
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


def is_git_repo() -> bool:
    return os.path.isdir(os.path.join(HUB_ROOT, ".git"))


def git_porcelain(paths: list[str] | None = None) -> list[str]:
    if not is_git_repo():
        return []
    args = ["git", "status", "--porcelain"]
    if paths:
        args.extend(paths)
    try:
        result = subprocess.run(
            args,
            cwd=HUB_ROOT,
            capture_output=True,
            text=True,
            timeout=8,
            check=False,
        )
    except (OSError, subprocess.TimeoutExpired):
        return []
    if result.returncode != 0:
        return []
    return [line for line in result.stdout.splitlines() if line.strip()]


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
            json_response(self, 200, {
                "hubRoot": HUB_ROOT,
                "dataDir": DATA_DIR,
                "templatesDir": TEMPLATES_DIR,
                "port": PORT,
                "isGitRepo": is_git_repo(),
            })
            return

        if path == "/api/git-status":
            changed = git_porcelain(["Data/"])
            json_response(self, 200, {
                "isGitRepo": is_git_repo(),
                "changed": changed,
                "hubRoot": HUB_ROOT,
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
        print("请在 Tech-Cosmos.Hub 仓库内运行，或设置环境变量 HUB_ROOT。", file=sys.stderr)
        return 1

    server = HTTPServer(("127.0.0.1", PORT), HubStudioHandler)
    print(f"Hub Studio  http://127.0.0.1:{PORT}")
    print(f"Hub 仓库    {HUB_ROOT}")
    if is_git_repo():
        print("Git 仓库已识别 — 保存后可 git add Data/ && git commit")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
