param([switch]$SkipInstall)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$frontend = Join-Path $root 'frontend'
$project = 'citysurfers-stage7-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$previousApi = $env:VITE_API_BASE_URL
$previousSimulation = $env:VITE_DEMO_SIMULATION_ENABLED
$previousPreview = $env:STAGE7_PREVIEW
function Invoke-Checked([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Exe failed with exit code $LASTEXITCODE" }
}
Push-Location $root
try {
    # Refuse occupied ports rather than resetting or testing somebody else's demo.
    foreach ($port in @(8080, 5173)) {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $port)
        try { $listener.Start() } finally { $listener.Stop() }
    }
    Invoke-Checked docker @('compose','-p',$project,'-f','docker-compose.demo.yml','up','--build','-d')
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try { $response = Invoke-WebRequest 'http://localhost:8080/health/ready' -TimeoutSec 3; if ($response.StatusCode -eq 200) { $ready = $true; break } } catch { }
        Start-Sleep -Seconds 1
    }
    if (!$ready) { throw 'Stage 6 API did not become ready.' }
    Set-Location $frontend
    $env:VITE_API_BASE_URL = 'http://localhost:8080'
    $env:VITE_DEMO_SIMULATION_ENABLED = 'true'
    $env:STAGE7_PREVIEW = 'true'
    if (!$SkipInstall) { Invoke-Checked npm.cmd @('ci') }
    Invoke-Checked npm.cmd @('run','build')
    Invoke-Checked npx.cmd @('playwright','install','chromium')
    # Playwright owns the preview process and tears it down, including on test failure.
    Invoke-Checked npm.cmd @('run','e2e')
} finally {
    Set-Location $root
    & docker compose -p $project -f docker-compose.demo.yml down --volumes --remove-orphans
    $env:VITE_API_BASE_URL = $previousApi
    $env:VITE_DEMO_SIMULATION_ENABLED = $previousSimulation
    $env:STAGE7_PREVIEW = $previousPreview
    Pop-Location
}
