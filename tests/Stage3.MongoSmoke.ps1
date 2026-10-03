param(
    [string]$MongoContainer = 'citysurfers-mongo',
    [string]$Database = 'citysurfers',
    [string]$ApiImage = 'citysurfers-api:stage3',
    [int]$Port = 18083
)

$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Use a simple database name.' }
$api = 'citysurfers-stage3-check-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
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
    param([string]$Method, [string]$Path, [object]$Body = $null, [int]$Expected = 200)
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $base + $Path)
    if ($null -ne $Body) {
        $request.Content = [System.Net.Http.StringContent]::new(($Body | ConvertTo-Json -Compress),
            [Text.Encoding]::UTF8, 'application/json')
    }
    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            if ([int]$response.StatusCode -ne $Expected) { throw "$Method $Path returned $([int]$response.StatusCode), expected $Expected" }
            $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if ($text.StartsWith('{')) { return $text | ConvertFrom-Json }
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}

function Assert-Equal {
    param($Actual, $Expected, [string]$Message)
    if ($Actual -ne $Expected) { throw "$Message (expected $Expected, received $Actual)" }
}

function Wait-Api {
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try { Request 'GET' '/health' | Out-Null; return } catch {}
        Start-Sleep -Milliseconds 500
    }
    throw 'API did not start.'
}

function Assert-MongoScores {
    param($Today, $Month, $Progress, [string]$UserId)
    if ($UserId -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Unexpected demo user id.' }
    $todayStart = ([DateTimeOffset]$Today.periodStartUtc).ToString('O')
    $todayEnd = ([DateTimeOffset]$Today.periodEndUtc).ToString('O')
    $monthStart = ([DateTimeOffset]$Month.periodStartUtc).ToString('O')
    $monthEnd = ([DateTimeOffset]$Month.periodEndUtc).ToString('O')
    $code = @"
const d = db.getSiblingDB('$Database');
const runs = d.runs.find({userId:'$UserId', status:{`$in:['active','completed']}}).toArray();
const completed = runs.filter(r => r.status === 'completed');
const points = (from,to) => runs.filter(r => r.startedAtUtc >= new Date(from) && r.startedAtUtc < new Date(to))
  .reduce((sum,r) => sum + r.seasonPointsEarned,0);
print(JSON.stringify({today:points('$todayStart','$todayEnd'), month:points('$monthStart','$monthEnd'),
  completedRuns:completed.length, totalPoints:completed.reduce((sum,r) => sum+r.seasonPointsEarned,0),
  totalDistance:completed.reduce((sum,r) => sum+r.distanceMeters,0),
  totalDuration:completed.reduce((sum,r) => sum+r.durationSeconds,0),
  collections:d.getCollectionNames().sort()}));
"@
    $actual = (Docker-Check @('exec', $MongoContainer, 'mongosh', '--quiet', '--eval', $code)) | ConvertFrom-Json
    Assert-Equal $Today.currentUser.points $actual.today 'Today score derives from Mongo runs'
    Assert-Equal $Month.currentUser.points $actual.month 'Month score derives from Mongo runs'
    Assert-Equal $Progress.lifetime.completedRuns $actual.completedRuns 'Mongo completed run count'
    Assert-Equal $Progress.lifetime.totalPointsEarned $actual.totalPoints 'Mongo lifetime points'
    Assert-Equal $Progress.lifetime.totalDistanceMeters $actual.totalDistance 'Mongo lifetime distance'
    Assert-Equal $Progress.lifetime.totalDurationSeconds $actual.totalDuration 'Mongo lifetime duration'
    Assert-Equal ($actual.collections -join ',') 'runs,users' 'No additional collections'
}

try {
    $ip = Docker-Check @('inspect', $MongoContainer, '--format', '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}')
    if ($ip -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Mongo container IP unavailable.' }
    Docker-Check @('run', '-d', '--rm', '--name', $api, '-p', "127.0.0.1:${Port}:8080",
        '-e', "MongoDb__ConnectionString=mongodb://${ip}:27017", '-e', "MongoDb__DatabaseName=$Database",
        '-e', 'ASPNETCORE_ENVIRONMENT=Development', $ApiImage) | Out-Null
    $started = $true
    Wait-Api
    Request 'GET' '/health/ready' | Out-Null
    $login = Request 'POST' '/api/auth/login' @{ username = 'demo'; password = '1234' }
    Request 'GET' '/api/runs/active' -Expected 404 | Out-Null
    $baselineToday = Request 'GET' '/api/leaderboards/today'
    $baselineMonth = Request 'GET' '/api/leaderboards/month'
    $baselineProgress = Request 'GET' '/api/progress'
    Assert-MongoScores $baselineToday $baselineMonth $baselineProgress $login.id
    $run = Request 'POST' '/api/runs' -Expected 201
    $id = $run.id
    $active = Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 2800; durationSeconds = 840 }
    Assert-Equal $active.competition.seasonPointsEarned 30 'Active run points'
    $today = Request 'GET' '/api/leaderboards/today'
    $month = Request 'GET' '/api/leaderboards/month'
    $progress = Request 'GET' '/api/progress'
    Assert-Equal ($today.currentUser.points - $baselineToday.currentUser.points) 30 'Active today points included once'
    Assert-Equal ($month.currentUser.points - $baselineMonth.currentUser.points) 30 'Active month points included once'
    Assert-Equal $progress.lifetime.totalPointsEarned $baselineProgress.lifetime.totalPointsEarned 'Active excluded from progress'
    Assert-MongoScores $today $month $progress $login.id
    $finish = Request 'POST' "/api/runs/$id/finish" @{ distanceMeters = 6800; durationSeconds = 2210 }
    Assert-Equal $finish.seasonPointsEarned 59 'Completed run points'
    $today = Request 'GET' '/api/leaderboards/today'
    $month = Request 'GET' '/api/leaderboards/month'
    $progress = Request 'GET' '/api/progress'
    $history = Request 'GET' '/api/runs/history'
    Assert-Equal ($today.currentUser.points - $baselineToday.currentUser.points) 59 'Completed today points counted once'
    Assert-Equal ($month.currentUser.points - $baselineMonth.currentUser.points) 59 'Completed month points counted once'
    Assert-Equal ($progress.lifetime.completedRuns - $baselineProgress.lifetime.completedRuns) 1 'Progress completed count delta'
    Assert-Equal ($progress.lifetime.totalDistanceMeters - $baselineProgress.lifetime.totalDistanceMeters) 6800 'Progress distance delta'
    Assert-Equal ($progress.lifetime.totalDurationSeconds - $baselineProgress.lifetime.totalDurationSeconds) 2210 'Progress duration delta'
    Assert-Equal ($progress.lifetime.totalPointsEarned - $baselineProgress.lifetime.totalPointsEarned) 59 'Progress points delta'
    Assert-Equal $history.items[0].id $id 'Newest run in Mongo history'
    Assert-Equal $history.items[0].pointsEarned 59 'History points'
    Assert-Equal $history.items[0].overtakesCount 4 'History overtakes'
    Assert-Equal $history.items[0].averagePaceSecondsPerKm 325 'History pace'
    Assert-MongoScores $today $month $progress $login.id
    Request 'GET' '/api/runs/history?limit=0' -Expected 400 | Out-Null
    Request 'GET' '/api/map/activity?period=invalid' -Expected 400 | Out-Null
    $openApi = Request 'GET' '/openapi/v1.json'
    foreach ($path in @('/api/runs/history','/api/progress','/api/leaderboards/today','/api/leaderboards/month','/api/map/activity')) {
        if ($null -eq $openApi.paths.$path) { throw "OpenAPI missing $path" }
    }
    Docker-Check @('restart', $api) | Out-Null
    Wait-Api
    foreach ($check in @(
        @{ Path = '/api/runs/history'; Value = $history },
        @{ Path = '/api/progress'; Value = $progress },
        @{ Path = '/api/leaderboards/today'; Value = $today },
        @{ Path = '/api/leaderboards/month'; Value = $month }
    )) {
        $loaded = Request 'GET' $check.Path
        Assert-Equal ($loaded | ConvertTo-Json -Depth 20 -Compress) ($check.Value | ConvertTo-Json -Depth 20 -Compress) "Restart persistence: $($check.Path)"
    }
    foreach ($period in @('live','today','month')) {
        $map = Request 'GET' "/api/map/activity?period=$period"
        Assert-Equal $map.period $period 'Map period'
        Assert-Equal $map.zones.Count 5 'Aggregate Krakow zones'
        foreach ($zone in $map.zones) {
            Assert-Equal (($zone.PSObject.Properties.Name | Sort-Object) -join ',') 'activeRunners,activityLevel,averagePaceSecondsPerKm,id,latitude,longitude,name,runs' 'Aggregate zone fields only'
        }
    }
    Write-Output "Stage 3 real Mongo flow passed: scores derive from runs, history/progress/leaderboards survive restart. Completed run: $id"
} finally {
    $client.Dispose()
    if ($started) { Docker-Check @('stop', $api) | Out-Null }
}
