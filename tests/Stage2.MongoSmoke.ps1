param(
    [string]$MongoContainer = 'citysurfers-mongo',
    [string]$Database = 'citysurfers',
    [string]$ConnectionString = '',
    [string]$ApiImage = 'citysurfers-api:stage2',
    [int]$Port = 18082
)

$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Use a simple database name for the smoke check.' }
$api = 'citysurfers-stage2-check-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$base = "http://127.0.0.1:$Port"
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.UseProxy = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(5)
$started = $false

function Docker-Check {
    param([string[]]$Arguments)
    $output = & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed: $($Arguments[0])" }
    return $output
}

function Request {
    param([string]$Method, [string]$Path, [object]$Body = $null)
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $base + $Path)
    if ($null -ne $Body) {
        $request.Content = [System.Net.Http.StringContent]::new(($Body | ConvertTo-Json -Compress),
            [Text.Encoding]::UTF8, 'application/json')
    }
    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            return @{ Status = [int]$response.StatusCode; Text = $text;
                Json = $(if ($text.StartsWith('{')) { $text | ConvertFrom-Json } else { $null }) }
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}

function Assert-Equal {
    param($Actual, $Expected, [string]$Message)
    if ($Actual -ne $Expected) { throw "$Message (expected $Expected, received $Actual)" }
}

function Request-Concurrently {
    param([string]$Method, [string]$Path, [object]$Body = $null)
    $pending = @(for ($i = 0; $i -lt 8; $i++) {
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $base + $Path)
        if ($null -ne $Body) {
            $request.Content = [System.Net.Http.StringContent]::new(($Body | ConvertTo-Json -Compress),
                [Text.Encoding]::UTF8, 'application/json')
        }
        @{ Request = $request; Task = $client.SendAsync($request) }
    })
    foreach ($item in $pending) {
        try {
            $response = $item.Task.GetAwaiter().GetResult()
            try {
                $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                @{ Status = [int]$response.StatusCode; Text = $text; Json = ($text | ConvertFrom-Json) }
            } finally { $response.Dispose() }
        } finally { $item.Request.Dispose() }
    }
}

function Wait-Api {
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $running = Docker-Check @('ps', '--filter', "name=$api", '--format', '{{.State}}')
        if ($running -ne 'running') { throw 'Temporary API container exited before readiness.' }
        try { if ((Request 'GET' '/health').Status -eq 200) { return } } catch {}
        Start-Sleep -Milliseconds 500
    }
    throw 'API did not start.'
}

function Assert-Document {
    param([string]$Id, [string]$Status, [int]$Count, [int]$Points, [double]$Distance, [double]$Duration)
    $code = "const r=db.getSiblingDB('$Database').runs.findOne({_id:'$Id'}); " +
        "if(!r || r.status!='$Status' || r.overtakes.length!=$Count || r.seasonPointsEarned!=$Points " +
        "|| r.distanceMeters!=$Distance || r.durationSeconds!=$Duration " +
        "|| r.currentRank!=41-$Count || new Set(r.overtakes.map(x=>x.opponentKey)).size!=$Count) " +
        "throw new Error('Persisted run mismatch'); print('Document verified: $Status, $Count overtakes, $Points points');"
    Docker-Check @('exec', $MongoContainer, 'mongosh', '--quiet', '--eval', $code)
}

try {
    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        $ip = Docker-Check @('inspect', $MongoContainer, '--format', '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}')
        if ($ip -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Supply ConnectionString for this Mongo container network.' }
        $ConnectionString = "mongodb://${ip}:27017"
    }
    Docker-Check @('run', '-d', '--rm', '--name', $api, '-p', "127.0.0.1:${Port}:8080",
        '-e', "MongoDb__ConnectionString=$ConnectionString", '-e', "MongoDb__DatabaseName=$Database",
        '-e', 'ASPNETCORE_ENVIRONMENT=Production', $ApiImage) | Out-Null
    $started = $true
    Wait-Api
    Assert-Equal (Request 'GET' '/health/ready').Status 200 'Mongo readiness'
    $login = Request 'POST' '/api/auth/login' @{ username = 'demo'; password = '1234' }
    Assert-Equal $login.Status 200 'Demo login'
    Assert-Equal $login.Json.username 'demo' 'Demo identity'
    Assert-Equal (Request 'POST' '/api/auth/login' @{ username = 'demo'; password = 'wrong' }).Status 401 'Invalid login'
    Assert-Equal (Request 'GET' '/api/runs/active').Status 404 'Precondition: demo user must have no existing active run'
    $starts = @(Request-Concurrently 'POST' '/api/runs')
    Assert-Equal @($starts | Where-Object Status -eq 201).Count 1 'Concurrent start winner'
    Assert-Equal @($starts | Where-Object Status -eq 409).Count 7 'Concurrent start conflicts'
    $start = $starts | Where-Object Status -eq 201
    Assert-Equal $start.Status 201 'Start run'
    $id = $start.Json.id
    if ($id -notmatch '^[a-f0-9]{32}$') { throw 'Unexpected run id.' }
    Assert-Equal $start.Json.competition.currentTarget.opponent 'Runner_92' 'Initial target'
    Assert-Equal (Request 'POST' '/api/runs').Status 409 'Second start'
    Assert-Document $id 'active' 0 0 0 0

    $below = Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 1010; durationSeconds = 320 }
    Assert-Equal $below.Status 200 'Progress below threshold'
    Assert-Equal $below.Json.events.Count 0 'No premature overtake'
    Assert-Equal $below.Json.competition.currentTarget.distanceToOvertakeMeters 190 'Target distance'
    if ([Math]::Abs($below.Json.averagePaceSecondsPerKm - 316.83168316831683) -gt 0.00001) { throw 'Pace mismatch.' }
    Assert-Equal (Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 1000; durationSeconds = 320 }).Status 400 'Decreasing distance'
    Assert-Equal (Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 1010; durationSeconds = 319 }).Status 400 'Decreasing duration'

    $first = Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 1200; durationSeconds = 360 }
    Assert-Equal $first.Status 200 'First threshold'
    Assert-Equal $first.Json.events.Count 1 'First overtake count'
    Assert-Equal $first.Json.events[0].type 'OVERTAKE' 'Event type'
    Assert-Equal $first.Json.events[0].opponent 'Runner_92' 'First opponent'
    Assert-Equal $first.Json.competition.currentTarget.opponent 'Marta' 'Next opponent'
    Assert-Equal (Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 1200; durationSeconds = 360 }).Json.events.Count 0 'Repeated progress'
    Assert-Document $id 'active' 1 16 1200 360
    $updates = @(Request-Concurrently 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 2800; durationSeconds = 840 })
    $newEvents = @($updates | Where-Object Status -eq 200 | ForEach-Object { $_.Json.events })
    Assert-Equal $newEvents.Count 1 'Concurrent progress awards only once'
    if (@($updates | Where-Object { $_.Status -notin @(200, 409) }).Count -gt 0) { throw 'Concurrent progress failed.' }
    $second = $updates | Where-Object { $_.Status -eq 200 -and $_.Json.events.Count -eq 1 } | Select-Object -First 1
    Assert-Equal $second.Status 200 'Second threshold'
    Assert-Equal $second.Json.events.Count 1 'Second overtake count'
    Assert-Equal $second.Json.events[0].opponent 'Marta' 'Second opponent'
    Assert-Document $id 'active' 2 30 2800 840

    Docker-Check @('restart', $api) | Out-Null
    Wait-Api
    $resumed = Request 'GET' '/api/runs/active'
    Assert-Equal $resumed.Status 200 'Resume after restart'
    Assert-Equal $resumed.Json.id $id 'Persisted active id'
    Assert-Equal $resumed.Json.distanceMeters 2800 'Persisted active distance'
    Assert-Equal $resumed.Json.competition.seasonPointsEarned 30 'Persisted active rewards'
    Assert-Equal (Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 2800; durationSeconds = 840 }).Json.events.Count 0 'No duplicate after restart'

    $finishes = @(Request-Concurrently 'POST' "/api/runs/$id/finish" @{ distanceMeters = 6800; durationSeconds = 2210 })
    Assert-Equal @($finishes | Where-Object Status -eq 200).Count 1 'Concurrent finish winner'
    Assert-Equal @($finishes | Where-Object Status -eq 409).Count 7 'Concurrent finish conflicts'
    $finish = $finishes | Where-Object Status -eq 200
    Assert-Equal $finish.Status 200 'Finish run'
    Assert-Equal $finish.Json.overtakesCount 4 'Finish evaluates multiple remaining targets'
    Assert-Equal $finish.Json.rankBefore 41 'Starting rank'
    Assert-Equal $finish.Json.rankAfter 37 'Final rank'
    Assert-Equal $finish.Json.seasonPointsEarned 59 'Final points'
    Assert-Equal $finish.Json.averagePaceSecondsPerKm 325 'Final pace'
    if ($null -ne $finish.Json.nextTarget -or $null -eq $finish.Json.finishedAtUtc) { throw 'Incomplete summary.' }
    Assert-Equal (Request 'POST' "/api/runs/$id/finish" @{ distanceMeters = 6800; durationSeconds = 2210 }).Status 409 'Repeated finish'
    Assert-Document $id 'completed' 4 59 6800 2210

    Docker-Check @('restart', $api) | Out-Null
    Wait-Api
    $loaded = Request 'GET' "/api/runs/$id"
    Assert-Equal $loaded.Status 200 'Completed run after restart'
    Assert-Equal $loaded.Text $finish.Text 'Summary survives restart unchanged'
    Assert-Equal (Request 'GET' '/api/runs/active').Status 404 'No active run after completion'
    Assert-Document $id 'completed' 4 59 6800 2210
    Write-Output "Stage 2 real Mongo flow passed. Completed demo run: $id"
} finally {
    $client.Dispose()
    if ($started -and (Docker-Check @('ps', '--filter', "name=$api", '--format', '{{.State}}')) -eq 'running') {
        Docker-Check @('stop', $api) | Out-Null
    }
}
