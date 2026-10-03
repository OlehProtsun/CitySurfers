param(
    [string]$ApiImage = 'citysurfers-api:stage6',
    [int]$Port = 18086
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/Stage6.SmokeHelpers.ps1"
$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 12)
$network = "citysurfers-stage6-$suffix"
$mongo = "citysurfers-stage6-mongo-$suffix"
$api = "citysurfers-stage6-api-$suffix"
$base = "http://127.0.0.1:$Port"
$origin = 'https://frontend.example'
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.UseProxy = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(10)
$networkCreated = $false

function Docker-Check {
    param([string[]]$Arguments)
    $output = & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed: $($Arguments[0])" }
    return $output
}

function Remove-SmokeContainer {
    param([string]$Name)
    & docker container inspect $Name 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { Docker-Check @('rm', '-f', '-v', $Name) | Out-Null }
}

try {
    Docker-Check @('build', '-f', "$PSScriptRoot/../Dockerfile", '-t', $ApiImage, "$PSScriptRoot/..") | Out-Null
    $user = Docker-Check @('image', 'inspect', $ApiImage, '--format', '{{.Config.User}}')
    if ([string]::IsNullOrWhiteSpace($user) -or $user -in @('0', 'root')) { throw 'Docker runtime must be non-root.' }
    Docker-Check @('network', 'create', $network) | Out-Null
    $networkCreated = $true
    Docker-Check @('run', '-d', '--name', $mongo, '--network', $network, '--network-alias', 'mongo', 'mongo:8.0') | Out-Null
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        & docker exec $mongo mongosh --quiet --eval 'quit(db.adminCommand({ping:1}).ok ? 0 : 1)' 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready) { throw 'Isolated Mongo did not become ready.' }
    Docker-Check @('run', '-d', '--name', $api, '--network', $network, '-p', "127.0.0.1:${Port}:8080",
        '-e', 'ASPNETCORE_ENVIRONMENT=Production', '-e', 'ASPNETCORE_HTTP_PORTS=8080',
        '-e', 'MongoDb__ConnectionString=mongodb://mongo:27017', '-e', 'MongoDb__DatabaseName=citysurfers_stage6_smoke',
        '-e', 'DemoData__SeedOnStartup=true', '-e', 'DemoData__ResetRunsOnStartup=true',
        '-e', "Cors__AllowedOrigins__0=$origin", $ApiImage) | Out-Null
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try { Request 'GET' '/health/ready'; $ready = $true; break } catch {}
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready) { throw 'Production API did not become ready.' }
    Request 'GET' '/openapi/v1.json' -Expected 404
    Test-ReadContracts
    Test-Cors $origin
    Request 'GET' '/api/home' -Headers @{ Origin = 'https://untrusted.example' } | Out-Null
    Test-FullDemo
    $collections = Docker-Check @('exec', $mongo, 'mongosh', '--quiet', '--eval',
        "print(db.getSiblingDB('citysurfers_stage6_smoke').getCollectionNames().sort().join(','))")
    Assert-Equal $collections 'runs,users' 'Only existing Mongo collections'
    Write-Output 'PASS Stage 6 Production release smoke, OpenAPI disabled, non-root runtime, unchanged collections.'
} finally {
    $client.Dispose()
    try {
        Remove-SmokeContainer $api
    } finally {
        try {
            Remove-SmokeContainer $mongo
        } finally {
            if ($networkCreated) { Docker-Check @('network', 'rm', $network) | Out-Null }
        }
    }
}
