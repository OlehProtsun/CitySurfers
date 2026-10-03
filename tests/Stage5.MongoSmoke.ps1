param(
    [string]$MongoContainer = 'citysurfers-mongo',
    [string]$Database = 'citysurfers_stage5_smoke',
    [string]$ApiImage = 'citysurfers-api:stage5',
    [int]$Port = 18085
)

$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^citysurfers_stage5_smoke[a-zA-Z0-9_-]*$') {
    throw 'Use a dedicated citysurfers_stage5_smoke database name. Existing databases are never modified.'
}
$api = 'citysurfers-stage5-check-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$base = "http://127.0.0.1:$Port"
$origin = 'https://frontend.example'
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.UseProxy = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(5)
$started = $false
$databaseCreated = $false

function Docker-Check {
    param([string[]]$Arguments)
    $output = & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed: $($Arguments[0])" }
    return $output
}

function Mongo {
    param([string]$Code)
    $script = "const d = db.getSiblingDB('$Database'); " + $Code
    return (Docker-Check @('exec', $MongoContainer, 'mongosh', '--quiet', '--eval', $script)) | ConvertFrom-Json
}

function Assert-Equal {
    param($Actual, $Expected, [string]$Message)
    if ($Actual -ne $Expected) { throw "$Message (expected $Expected, received $Actual)" }
}

function Request {
    param([string]$Method, [string]$Path, [object]$Body = $null, [int]$Expected = 200,
        [hashtable]$Headers = @{}, [string]$AllowOrigin = '', [switch]$Preflight)
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $base + $Path)
    foreach ($key in $Headers.Keys) { $request.Headers.Add($key, [string]$Headers[$key]) }
    if ($null -ne $Body) {
        $request.Content = [System.Net.Http.StringContent]::new(($Body | ConvertTo-Json -Compress),
            [Text.Encoding]::UTF8, 'application/json')
    }
    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            Assert-Equal ([int]$response.StatusCode) $Expected "$Method $Path status"
            if ($Headers.ContainsKey('Origin')) {
                $actual = if ($response.Headers.Contains('Access-Control-Allow-Origin')) {
                    $response.Headers.GetValues('Access-Control-Allow-Origin') -join ','
                } else { '' }
                Assert-Equal $actual $AllowOrigin 'CORS allow-origin'
                if ($response.Headers.Contains('Access-Control-Allow-Credentials')) { throw 'CORS enables credentials.' }
            }
            if ($Preflight) {
                Assert-Equal ($response.Headers.GetValues('Access-Control-Allow-Methods') -join ',') 'PATCH' 'Preflight method'
                Assert-Equal ($response.Headers.GetValues('Access-Control-Allow-Headers') -join ',') 'content-type' 'Preflight header'
            }
            $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if ($text.StartsWith('{')) { return $text | ConvertFrom-Json }
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}

function Start-Api {
    param([bool]$Reset)
    if ($script:started) {
        Docker-Check @('stop', $api) | Out-Null
        $script:started = $false
    }
    Docker-Check @('run', '-d', '--rm', '--name', $api, '-p', "127.0.0.1:${Port}:8080",
        '-e', "MongoDb__ConnectionString=mongodb://${script:ip}:27017", '-e', "MongoDb__DatabaseName=$Database",
        '-e', 'ASPNETCORE_ENVIRONMENT=Development', '-e', 'DemoData__SeedOnStartup=true',
        '-e', "DemoData__ResetRunsOnStartup=$($Reset.ToString().ToLowerInvariant())",
        '-e', "Cors__AllowedOrigins__0=$origin", $ApiImage) | Out-Null
    $script:started = $true
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try { Request 'GET' '/health' | Out-Null; Request 'GET' '/health/ready' | Out-Null; return } catch {}
        Start-Sleep -Milliseconds 500
    }
    throw 'API did not become ready.'
}

function Assert-CleanHome {
    $homeState = Request 'GET' '/api/home'
    Assert-Equal $homeState.today.rank 41 'Clean today rank'
    Assert-Equal $homeState.today.points 0 'Clean points'
    Assert-Equal $homeState.activeRun $null 'Idle active run'
    Assert-Equal $homeState.nextGoal.type 'rival_points' 'Idle goal'
    Assert-Equal $homeState.nextGoal.remainingPoints 9 'Reachable fresh rival'
    Assert-Equal $homeState.nextGoal.currentRank 41 'Fresh monthly rank'
    Assert-Equal $homeState.nextGoal.targetRank 40 'Fresh rival rank'
    Assert-Equal @((Request 'GET' '/api/runs/history').items).Count 0 'Clean demo history'
}

function Preserved-Data {
    Mongo @'
print(JSON.stringify({
  users:EJSON.stringify(d.users.find().sort({_id:1}).toArray()),
  foreignRuns:EJSON.stringify(d.runs.find({userId:'foreign-stage5'}).sort({_id:1}).toArray()),
  userIndexes:EJSON.stringify(d.users.getIndexes()),
  runIndexes:EJSON.stringify(d.runs.getIndexes()),
  collections:d.getCollectionNames().sort().join(',')
}));
'@
}

try {
    $exists = Mongo "print(JSON.stringify(db.getSiblingDB('admin').runCommand({listDatabases:1,nameOnly:true}).databases.some(x => x.name === '$Database')));"
    if ($exists) { throw 'Smoke database already exists; choose another dedicated name. No data was modified.' }
    $databaseCreated = $true
    # Seed an existing demo with a non-default id, plus unrelated users and active/completed runs.
    Mongo @'
const date = new Date('2026-10-01T10:00:00Z');
d.users.insertMany([
  {_id:'persisted-demo-stage5',username:'demo',displayName:'Preserved Demo',demoPassword:'1234',createdAtUtc:date},
  {_id:'foreign-stage5',username:'foreign',displayName:'Foreign Runner',demoPassword:'fictional',createdAtUtc:date}
]);
for (const userId of ['persisted-demo-stage5','foreign-stage5']) {
  for (const status of ['active','completed']) {
    d.runs.insertOne({_id:userId+'-'+status,userId,status,startedAtUtc:date,updatedAtUtc:date,
      finishedAtUtc:status==='completed'?date:null,distanceMeters:100,durationSeconds:30,
      averagePaceSecondsPerKm:300,startingRank:41,currentRank:41,seasonPointsEarned:0,overtakes:[]});
  }
}
print(JSON.stringify(true));
'@ | Out-Null
    $ip = Docker-Check @('inspect', $MongoContainer, '--format', '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}')
    if ($ip -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Mongo container IP unavailable.' }
    Start-Api $true
    $login = Request 'POST' '/api/auth/login' @{ username = 'demo'; password = '1234' }
    Assert-Equal $login.id 'persisted-demo-stage5' 'Resolved persisted demo id'
    Assert-Equal $login.displayName 'Preserved Demo' 'Demo user preserved'
    $preserved = Preserved-Data
    Assert-Equal $preserved.collections 'runs,users' 'Only existing collections'
    $initial = Mongo "print(JSON.stringify({demo:d.runs.countDocuments({userId:'persisted-demo-stage5'}),foreign:d.runs.countDocuments({userId:'foreign-stage5'}),users:d.users.countDocuments({})}));"
    Assert-Equal $initial.demo 0 'Both active and completed demo runs deleted'
    Assert-Equal $initial.foreign 2 'Unrelated active/completed runs preserved'
    Assert-Equal $initial.users 2 'Users preserved'
    Assert-CleanHome
    Request 'GET' '/api/home' -Headers @{ Origin = $origin } -AllowOrigin $origin | Out-Null
    Request 'GET' '/api/home' -Headers @{ Origin = 'https://untrusted.example' } | Out-Null
    Request 'OPTIONS' '/api/runs/run-id/progress' -Expected 204 -Headers @{
        Origin = $origin; 'Access-Control-Request-Method' = 'PATCH'; 'Access-Control-Request-Headers' = 'content-type'
    } -AllowOrigin $origin -Preflight | Out-Null
    $run = Request 'POST' '/api/runs' -Expected 201
    $id = $run.id
    $homeState = Request 'GET' '/api/home'
    Assert-Equal $homeState.activeRun.id $id 'Home active run id'
    Assert-Equal $homeState.activeRun.startedAtUtc $run.startedAtUtc 'Home start time'
    Assert-Equal $homeState.nextGoal.type 'run_overtake' 'Run goal priority'
    Assert-Equal $homeState.nextGoal.currentRank 41 'Initial run rank'
    Assert-Equal $homeState.nextGoal.targetRank 40 'Initial target rank'
    $steps = @(
        @{ distance = 1200; points = 16; rank = 40 },
        @{ distance = 2500; points = 30; rank = 39 },
        @{ distance = 4000; points = 41; rank = 38 },
        @{ distance = 5500; points = 59; rank = 37 }
    )
    foreach ($step in $steps) {
        $progress = Request 'PATCH' "/api/runs/$id/progress" @{
            distanceMeters = $step.distance; durationSeconds = $step.distance * 0.3
        }
        Assert-Equal $progress.competition.seasonPointsEarned $step.points 'Run point progression'
        Assert-Equal $progress.competition.rank $step.rank 'Run rank progression'
        Assert-Equal @($progress.events).Count 1 'One overtake per threshold'
        foreach ($period in @('today', 'month')) {
            $board = Request 'GET' "/api/leaderboards/$period"
            Assert-Equal $board.currentUser.points $step.points "$period points"
            Assert-Equal $board.currentUser.rank $step.rank "$period rank"
        }
        $homeState = Request 'GET' '/api/home'
        Assert-Equal $homeState.activeRun.distanceMeters $step.distance 'Home stored distance'
        Assert-Equal $homeState.activeRun.durationSeconds ($step.distance * 0.3) 'Home stored duration'
        Assert-Equal $homeState.activeRun.averagePaceSecondsPerKm 300 'Home stored pace'
        Assert-Equal $homeState.today.rank $step.rank 'Home rank progression'
        if ($step.rank -gt 37) { Assert-Equal $homeState.nextGoal.type 'run_overtake' 'Next target exists' }
    }
    $finish = Request 'POST' "/api/runs/$id/finish" @{ distanceMeters = 6800; durationSeconds = 2210 }
    Assert-Equal $finish.seasonPointsEarned 59 'Finished points'
    Assert-Equal $finish.rankBefore 41 'Starting run rank'
    Assert-Equal $finish.rankAfter 37 'Final run rank'
    Assert-Equal $finish.overtakesCount 4 'Four unique overtakes'
    foreach ($period in @('today', 'month')) {
        $board = Request 'GET' "/api/leaderboards/$period"
        Assert-Equal $board.currentUser.rank 37 "$period finished rank"
        Assert-Equal $board.currentUser.points 59 "$period finished points counted once"
    }
    $rival = Request 'GET' '/api/rivals/current'
    Assert-Equal $rival.currentUser.rank 37 'Monthly showcase rank'
    Assert-Equal $rival.rival.rank 36 'Next rival rank'
    Assert-Equal $rival.rival.points 64 'Next rival points'
    Assert-Equal $rival.rival.pointsGap 5 'Rival gap'
    Assert-Equal $rival.rival.pointsToPass 6 'Points to pass'
    $homeState = Request 'GET' '/api/home'
    Assert-Equal $homeState.activeRun $null 'Finished Home idle'
    Assert-Equal $homeState.nextGoal.type 'rival_points' 'Finished Home fallback'
    Assert-Equal $homeState.nextGoal.remainingPoints 6 'Finished Home reachable goal'
    $history = Request 'GET' '/api/runs/history'
    Assert-Equal @($history.items).Count 1 'Completed demo history count'
    Assert-Equal $history.items[0].id $id 'Completed history id'
    $progress = Request 'GET' '/api/progress'
    Assert-Equal $progress.lifetime.completedRuns 1 'Completed progress count'
    Assert-Equal $progress.lifetime.totalPointsEarned 59 'Progress points'
    Assert-Equal $progress.lifetime.totalDistanceMeters 6800 'Progress distance'
    Assert-Equal $progress.lifetime.totalDurationSeconds 2210 'Progress duration'
    $openApi = Request 'GET' '/openapi/v1.json'
    foreach ($path in @('/api/auth/login','/api/runs','/api/runs/active','/api/runs/{runId}',
        '/api/runs/{runId}/progress','/api/runs/{runId}/finish','/api/runs/history','/api/progress',
        '/api/leaderboards/today','/api/leaderboards/month','/api/rivals/current','/api/goals/next','/api/map/activity')) {
        if ($null -eq $openApi.paths.$path) { throw "OpenAPI missing existing path: $path" }
    }
    Assert-Equal $openApi.paths.'/api/home'.get.responses.'200'.content.'application/json'.schema.'$ref' '#/components/schemas/HomeResponse' 'Home OpenAPI schema'
    Start-Api $false
    Assert-Equal ((Request 'GET' '/api/home') | ConvertTo-Json -Depth 20 -Compress) ($homeState | ConvertTo-Json -Depth 20 -Compress) 'Home survives restart with reset off'
    Assert-Equal (Request 'GET' '/api/runs/history').items[0].id $id 'Run persists with reset off'
    Start-Api $true
    Assert-CleanHome
    $afterReset = Request 'POST' '/api/auth/login' @{ username = 'demo'; password = '1234' }
    Assert-Equal $afterReset.id $login.id 'Demo user survives reset'
    Assert-Equal (Preserved-Data | ConvertTo-Json -Depth 20 -Compress) ($preserved | ConvertTo-Json -Depth 20 -Compress) 'Users, foreign runs, indexes and collections preserved'
    Write-Output 'Stage 5 real Mongo smoke passed: Home, CORS, 41->37 ladder, 59 points, 6-point rival, persistence, targeted reset and preserved indexes/data.'
} finally {
    $client.Dispose()
    try {
        if ($started) { Docker-Check @('stop', $api) | Out-Null }
    } finally {
        if ($databaseCreated) { Mongo 'd.dropDatabase(); print(JSON.stringify(true));' | Out-Null }
    }
}
