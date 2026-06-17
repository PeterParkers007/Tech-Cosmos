$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $here

$port = if ($env:HUB_STUDIO_PORT) { $env:HUB_STUDIO_PORT } else { "8765" }
$url = "http://127.0.0.1:$port"

function Test-HubStudioUp {
    try {
        $r = Invoke-WebRequest -Uri "$url/api/meta" -UseBasicParsing -TimeoutSec 1
        return $r.StatusCode -eq 200
    } catch {
        return $false
    }
}

if (-not (Test-HubStudioUp)) {
    $python = $null
    foreach ($cmd in @("py -3", "python", "python3")) {
        try {
            & $cmd -c "import sys; print(sys.version)" 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) { $python = $cmd; break }
        } catch { }
    }

    if (-not $python) {
        Write-Host "未找到 Python。请安装 Python 3 后重试，或在 Unity 菜单 Tech-Cosmos -> Hub Studio 启动。" -ForegroundColor Red
        exit 1
    }

    Write-Host "启动 Hub Studio 服务 ($python server.py) ..."
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c $python server.py" -WorkingDirectory $here -WindowStyle Minimized

    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 300
        if (Test-HubStudioUp) { break }
    }
}

if (Test-HubStudioUp) {
    Start-Process $url
    Write-Host "已打开 $url"
} else {
    Write-Host "服务启动失败。请手动运行: python server.py" -ForegroundColor Red
    exit 1
}
