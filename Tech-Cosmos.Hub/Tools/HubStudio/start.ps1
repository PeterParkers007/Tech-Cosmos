$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $here

$hubRoot = (Resolve-Path (Join-Path $here "..\..")).Path
$dataDir = Join-Path $hubRoot "Data"
$port = if ($env:HUB_STUDIO_PORT) { $env:HUB_STUDIO_PORT } else { 8765 }
$url = "http://127.0.0.1:$port"

Write-Host "本份 Hub: $hubRoot"
Write-Host "写入 Data: $dataDir"

function Get-RunningMeta {
    try {
        return Invoke-RestMethod -Uri "$url/api/meta" -TimeoutSec 1
    } catch {
        return $null
    }
}

function Stop-ListenerOnPort([int]$ListenPort) {
  $lines = netstat -ano | Select-String ":$ListenPort\s+.*LISTENING"
  foreach ($line in $lines) {
    $parts = ($line -replace '\s+', ' ').Trim().Split(' ')
    $procId = $parts[-1]
    if ($procId -match '^\d+$' -and [int]$procId -gt 0) {
      Write-Host "结束占用端口 $ListenPort 的进程 PID $procId ..."
      Stop-Process -Id ([int]$procId) -Force -ErrorAction SilentlyContinue
    }
  }
  Start-Sleep -Milliseconds 400
}

function Start-HubStudioServer {
    $python = $null
    foreach ($cmd in @("py -3", "python", "python3")) {
        try {
            $parts = $cmd -split " ", 2
            $exe = $parts[0]
            $args = if ($parts.Length -gt 1) { $parts[1] } else { "" }
            & $exe $args -c "import sys" 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) {
                $python = @{ Exe = $exe; Args = $args }
                break
            }
        } catch { }
    }

    if (-not $python) {
        Write-Host "未找到 Python 3。运行: python server.py" -ForegroundColor Red
        exit 1
    }

    $pyLine = if ($python.Args) { "$($python.Exe) $($python.Args) server.py" } else { "$($python.Exe) server.py" }
    Write-Host "启动: $pyLine"
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c $pyLine" -WorkingDirectory $here -WindowStyle Minimized

    for ($i = 0; $i -lt 25; $i++) {
        Start-Sleep -Milliseconds 300
        $meta = Get-RunningMeta
        if ($meta -and ($meta.dataDir -ieq $dataDir)) { return $meta }
    }
    return $null
}

$meta = Get-RunningMeta
if ($meta -and ($meta.dataDir -ine $dataDir)) {
    Write-Host ""
    Write-Host "检测到端口 $port 上的服务写的是另一份目录:" -ForegroundColor Yellow
    Write-Host "  正在运行: $($meta.dataDir)" -ForegroundColor Yellow
    Write-Host "  本目录应为: $dataDir" -ForegroundColor Yellow
    Write-Host "正在重启服务 ..." -ForegroundColor Yellow
    Stop-ListenerOnPort $port
    $meta = $null
}

if (-not $meta) {
    $meta = Start-HubStudioServer
}

if ($meta -and ($meta.dataDir -ieq $dataDir)) {
    Write-Host ""
    Write-Host "确认写入: $($meta.dataDir)" -ForegroundColor Green
    Start-Process $url
} else {
    Write-Host "启动失败。请先结束旧服务后运行: python server.py" -ForegroundColor Red
    exit 1
}
