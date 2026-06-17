$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $here

$dataDir = (Resolve-Path (Join-Path $here "..\..\Data")).Path
$port = if ($env:HUB_STUDIO_PORT) { $env:HUB_STUDIO_PORT } else { "8765" }
$url = "http://127.0.0.1:$port"

Write-Host "Data: $dataDir"

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
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c $pyLine" -WorkingDirectory $here -WindowStyle Minimized

    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 300
        if (Test-HubStudioUp) { break }
    }
}

if (Test-HubStudioUp) {
    Start-Process $url
} else {
    Write-Host "启动失败。运行: python server.py" -ForegroundColor Red
    exit 1
}
