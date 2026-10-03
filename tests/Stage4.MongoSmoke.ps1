param(
    [string]$MongoContainer = 'citysurfers-mongo',
    [string]$Database = 'citysurfers',
    [string]$ApiImage = 'citysurfers-api:stage4',
    [int]$Port = 18084
)

$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Use a simple database name.' }
$api = 'citysurfers-stage4-check-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
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

function Assert-Rival {
    param($Rival, [string]$UserId)
    $board = Request 'GET' '/api/leaderboards/month'
    Assert-Equal $Rival.period 'month' 'Rival period'
    Assert-Equal $Rival.periodStartUtc $board.periodStartUtc 'Month start'
    Assert-Equal $Rival.periodEndUtc $board.periodEndUtc 'Month end'
    Assert-Equal $Rival.currentUser.points $board.currentUser.points 'Leaderboard points reused'
    Assert-Equal $Rival.currentUser.rank $board.currentUser.rank 'Leaderboard rank reused'
    if ($board.currentUser.rank -eq 1) {
        if ($null -ne $Rival.rival) { throw 'Rank one must have no rival.' }
    } else {
        $row = @($board.aroundMe + $board.top | Where-Object { $_.rank -eq ($board.currentUser.rank - 1) })[0]
        if ($null -eq $row -or $null -eq $Rival.rival) { throw 'Direct competitor missing.' }
        Assert-Equal $Rival.rival.displayName $row.displayName 'Direct rival identity'
        Assert-Equal $Rival.rival.rank $row.rank 'Direct rival rank'
        Assert-Equal $Rival.rival.points $row.points 'Direct rival points'
        Assert-Equal $Rival.rival.pointsGap ([Math]::Max(0, $row.points - $board.currentUser.points)) 'Point gap'
        Assert-Equal $Rival.rival.pointsToPass ([Math]::Max(1, $row.points - $board.currentUser.points + 1)) 'Points to pass'
    }
    if ($UserId -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Unexpected demo user id.' }
    $from = ([DateTimeOffset]$Rival.periodStartUtc).ToString('O')
    $to = ([DateTimeOffset]$Rival.periodEndUtc).ToString('O')
    $code = @"
const d = db.getSiblingDB('$Database');
const runs = d.runs.find({userId:'$UserId', status:{`$in:['active','completed']},
  startedAtUtc:{`$gte:new Date('$from'),`$lt:new Date('$to')}}).toArray();
print(JSON.stringify({points:runs.reduce((sum,r)=>sum+r.seasonPointsEarned,0),collections:d.getCollectionNames().sort()}));
"@
    $actual = (Docker-Check @('exec', $MongoContainer, 'mongosh', '--quiet', '--eval', $code)) | ConvertFrom-Json
    Assert-Equal $Rival.currentUser.points $actual.points 'Monthly points derive from persisted Mongo runs'
    Assert-Equal ($actual.collections -join ',') 'runs,users' 'No new Mongo collections'
}

function Assert-MonthlyGoal {
    param($Rival)
    $goal = (Request 'GET' '/api/goals/next').goal
    if ($null -eq $Rival.rival) {
        if ($null -ne $goal) { throw 'Expected null goal without rival or run target.' }
    } else {
        Assert-Equal $goal.type 'rival_points' 'Monthly fallback type'
        Assert-Equal $goal.source 'monthly_leaderboard' 'Monthly fallback source'
        Assert-Equal $goal.targetDisplayName $Rival.rival.displayName 'Goal rival identity'
        Assert-Equal $goal.remainingPoints $Rival.rival.pointsToPass 'Goal points to pass'
        Assert-Equal $goal.currentRank $Rival.currentUser.rank 'Goal user rank'
        Assert-Equal $goal.targetRank $Rival.rival.rank 'Goal rival rank'
        if ($null -ne $goal.runId -or $null -ne $goal.remainingDistanceMeters -or $null -ne $goal.potentialPoints) {
            throw 'Monthly goal contains active-run fields.'
        }
    }
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
    $before = Request 'GET' '/api/rivals/current'
    Assert-Rival $before $login.id
    Assert-MonthlyGoal $before
    $run = Request 'POST' '/api/runs' -Expected 201
    $id = $run.id
    $first = (Request 'GET' '/api/goals/next').goal
    Assert-Equal $first.type 'run_overtake' 'Active run has priority'
    Assert-Equal $first.source 'active_run' 'Active source'
    Assert-Equal $first.runId $id 'Run identity'
    Assert-Equal $first.targetDisplayName $run.competition.currentTarget.opponent 'Competition target reused'
    Assert-Equal $first.remainingDistanceMeters $run.competition.currentTarget.distanceToOvertakeMeters 'Competition distance reused'
    Assert-Equal $first.potentialPoints $run.competition.currentTarget.potentialPoints 'Competition reward reused'
    Assert-Equal $first.currentRank $run.competition.rank 'Competition rank reused'
    Assert-Equal $first.targetRank ($first.currentRank - 1) 'Rank transition'
    $progress = Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 850; durationSeconds = 255 }
    $closer = (Request 'GET' '/api/goals/next').goal
    Assert-Equal $closer.targetDisplayName $first.targetDisplayName 'Same target before crossing'
    Assert-Equal $closer.remainingDistanceMeters $progress.competition.currentTarget.distanceToOvertakeMeters 'Updated snapshot distance'
    if ($closer.remainingDistanceMeters -ge $first.remainingDistanceMeters) { throw 'Distance did not decrease.' }
    $progress = Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 1200; durationSeconds = 360 }
    $next = (Request 'GET' '/api/goals/next').goal
    if ($next.targetDisplayName -eq $first.targetDisplayName) { throw 'Target did not advance.' }
    Assert-Equal $next.targetDisplayName $progress.competition.currentTarget.opponent 'Next competition target'
    Assert-Equal $next.remainingDistanceMeters $progress.competition.currentTarget.distanceToOvertakeMeters 'Next target distance'
    Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = 5500; durationSeconds = 1650 } | Out-Null
    $exhausted = Request 'GET' '/api/rivals/current'
    Assert-MonthlyGoal $exhausted
    $finish = Request 'POST' "/api/runs/$id/finish" @{ distanceMeters = 6800; durationSeconds = 2210 }
    $after = Request 'GET' '/api/rivals/current'
    Assert-Rival $after $login.id
    Assert-Equal ($after.currentUser.points - $before.currentUser.points) $finish.seasonPointsEarned 'Persisted point delta'
    if ($after.currentUser.rank -gt $before.currentUser.rank) { throw 'Positive points worsened monthly rank.' }
    Assert-MonthlyGoal $after
    $openApi = Request 'GET' '/openapi/v1.json'
    foreach ($path in @('/api/rivals/current','/api/goals/next')) {
        if ($null -eq $openApi.paths.$path.get.responses.'200') { throw "OpenAPI missing GET 200: $path" }
    }
    Docker-Check @('restart', $api) | Out-Null
    Wait-Api
    Request 'GET' '/health/ready' | Out-Null
    $reloaded = Request 'GET' '/api/rivals/current'
    Assert-Equal ($reloaded | ConvertTo-Json -Depth 20 -Compress) ($after | ConvertTo-Json -Depth 20 -Compress) 'Derived rival survives restart'
    Assert-Rival $reloaded $login.id
    Assert-MonthlyGoal $reloaded
    Write-Output "Stage 4 real Mongo flow passed: idle/active goals, distance, target transitions, fallback, persisted scores and restart. Completed run: $id"
} finally {
    $client.Dispose()
    if ($started) { Docker-Check @('stop', $api) | Out-Null }
}
