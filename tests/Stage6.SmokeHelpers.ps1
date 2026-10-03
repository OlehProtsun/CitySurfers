Set-StrictMode -Version Latest

function Assert-Equal {
    param($Actual, $Expected, [string]$Message)
    if ($Actual -ne $Expected) { throw "$Message contract mismatch." }
}

function Assert-Fields {
    param($Value, [string[]]$Fields)
    foreach ($field in $Fields) {
        if ($null -eq $Value -or $null -eq $Value.PSObject.Properties[$field]) {
            throw "Missing JSON field: $field"
        }
    }
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
            Assert-Equal ([int]$response.StatusCode) $Expected "$Method $Path status (expected $Expected)"
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
            if ($Expected -eq 200 -or $Expected -eq 201) {
                if ($Path.StartsWith('/api/')) {
                    Assert-Equal $response.Content.Headers.ContentType.MediaType 'application/json' "$Method $Path content-type"
                    return $response.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
                }
            }
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}

function Test-ReadContracts {
    Request 'GET' '/health'
    Request 'GET' '/health/ready'
    $login = Request 'POST' '/api/auth/login' @{ username = 'demo'; password = '1234' }
    Assert-Fields $login @('id', 'username', 'displayName')
    Assert-Equal $login.username 'demo' 'Demo login'
    if ([string]::IsNullOrWhiteSpace($login.id)) { throw 'Demo id missing.' }
    Request 'POST' '/api/auth/login' @{ username = 'demo'; password = 'invalid-demo-password' } -Expected 401
    $homeState = Request 'GET' '/api/home'
    Assert-Fields $homeState @('today', 'activeRun', 'nextGoal')
    Assert-Fields $homeState.today @('rank', 'points')
    foreach ($period in @('today', 'month')) {
        $board = Request 'GET' "/api/leaderboards/$period"
        Assert-Fields $board @('period', 'periodStartUtc', 'periodEndUtc', 'currentUser', 'top', 'aroundMe')
        Assert-Fields $board.currentUser @('rank', 'points')
        Assert-Equal $board.period $period 'Leaderboard period'
        if ($board.top -isnot [array] -or $board.aroundMe -isnot [array]) { throw 'Leaderboard arrays missing.' }
    }
    $map = Request 'GET' '/api/map/activity?period=today'
    Assert-Fields $map @('period', 'generatedAtUtc', 'zones')
    Assert-Equal $map.period 'today' 'Map period'
    if ($map.zones -isnot [array]) { throw 'Map zones array missing.' }
    foreach ($zone in $map.zones) {
        Assert-Fields $zone @('id', 'name', 'latitude', 'longitude', 'activeRunners', 'runs', 'averagePaceSecondsPerKm', 'activityLevel')
    }
    $personal = Request 'GET' '/api/progress'
    Assert-Fields $personal @('lifetime', 'currentWeek', 'currentMonth', 'previousMonth', 'comparison')
    Assert-Fields $personal.lifetime @('completedRuns', 'totalDistanceMeters', 'totalDurationSeconds', 'totalPointsEarned')
    $history = Request 'GET' '/api/runs/history'
    Assert-Fields $history @('items')
    if ($history.items -isnot [array]) { throw 'History items array missing.' }
    Assert-Fields (Request 'GET' '/api/rivals/current') @('period', 'currentUser', 'rival')
    Assert-Fields (Request 'GET' '/api/goals/next') @('goal')
    Write-Output 'PASS health, readiness, login, Home and dedicated read contracts.'
}

function Test-Cors {
    param([string]$Origin)
    Request 'GET' '/api/home' -Headers @{ Origin = $Origin } -AllowOrigin $Origin | Out-Null
    Request 'OPTIONS' '/api/runs/smoke-id/progress' -Expected 204 -Headers @{
        Origin = $Origin; 'Access-Control-Request-Method' = 'PATCH'; 'Access-Control-Request-Headers' = 'content-type'
    } -AllowOrigin $Origin -Preflight
    Write-Output 'PASS configured CORS origin and PATCH preflight.'
}

function Test-FullDemo {
    Write-Output 'FullDemo modifies the shared demo user run state; a clean demo is required.'
    $homeState = Request 'GET' '/api/home'
    Assert-Equal $homeState.today.rank 41 'Clean Home rank'
    Assert-Equal $homeState.today.points 0 'Clean Home points'
    Assert-Equal $homeState.activeRun $null 'Clean Home idle'
    Assert-Equal @((Request 'GET' '/api/runs/history').items).Count 0 'Clean history'
    $run = Request 'POST' '/api/runs' -Expected 201
    Assert-Fields $run @('id', 'distanceMeters', 'durationSeconds', 'competition', 'events')
    $id = $run.id
    if ([string]::IsNullOrWhiteSpace($id)) { throw 'Run id missing.' }
    Assert-Equal (Request 'GET' '/api/runs/active').id $id 'Active id'
    foreach ($step in @(@(1200,360,16,40), @(2500,750,30,39), @(4000,1200,41,38), @(5500,1650,59,37))) {
        $progress = Request 'PATCH' "/api/runs/$id/progress" @{ distanceMeters = $step[0]; durationSeconds = $step[1] }
        Assert-Equal $progress.id $id 'Stable run id'
        Assert-Equal $progress.distanceMeters $step[0] 'Progress distance'
        Assert-Equal $progress.durationSeconds $step[1] 'Progress duration'
        Assert-Equal $progress.competition.seasonPointsEarned $step[2] 'Progress points'
        Assert-Equal $progress.competition.rank $step[3] 'Progress rank'
        Assert-Equal @($progress.events).Count 1 'One new overtake'
        Assert-Equal $progress.events[0].type 'OVERTAKE' 'Overtake event'
        foreach ($period in @('today', 'month')) {
            $board = Request 'GET' "/api/leaderboards/$period"
            Assert-Equal $board.currentUser.points $step[2] "$period points"
            Assert-Equal $board.currentUser.rank $step[3] "$period rank"
        }
        Write-Output "PASS overtake: $($step[2]) points, rank $($step[3])."
    }
    $finish = Request 'POST' "/api/runs/$id/finish" @{ distanceMeters = 6800; durationSeconds = 2210 }
    Assert-Equal $finish.runId $id 'Summary run id'
    Assert-Equal $finish.seasonPointsEarned 59 'Finished points'
    Assert-Equal $finish.rankBefore 41 'Starting rank'
    Assert-Equal $finish.rankAfter 37 'Finished rank'
    Assert-Equal $finish.overtakesCount 4 'Unique overtakes'
    Assert-Equal (Request 'GET' "/api/runs/$id").runId $id 'Completed detail'
    $homeState = Request 'GET' '/api/home'
    Assert-Equal $homeState.activeRun $null 'Finished Home idle'
    Assert-Equal $homeState.today.points 59 'Home points counted once'
    Assert-Equal $homeState.today.rank 37 'Home final rank'
    Assert-Equal $homeState.nextGoal.type 'rival_points' 'Home rival fallback'
    Assert-Equal $homeState.nextGoal.remainingPoints 6 'Home points to pass'
    $rival = Request 'GET' '/api/rivals/current'
    Assert-Equal $rival.rival.rank 36 'Next rival rank'
    Assert-Equal $rival.rival.points 64 'Rival points'
    Assert-Equal $rival.rival.pointsGap 5 'Rival gap'
    Assert-Equal $rival.rival.pointsToPass 6 'Rival points to pass'
    Assert-Equal (Request 'GET' '/api/goals/next').goal.remainingPoints 6 'Next goal'
    $history = Request 'GET' '/api/runs/history'
    Assert-Equal @($history.items).Count 1 'History count'
    Assert-Equal $history.items[0].id $id 'History id'
    $personal = Request 'GET' '/api/progress'
    Assert-Equal $personal.lifetime.completedRuns 1 'Progress completed count'
    Assert-Equal $personal.lifetime.totalPointsEarned 59 'Progress points'
    Assert-Equal $personal.lifetime.totalDistanceMeters 6800 'Progress distance'
    Assert-Equal $personal.lifetime.totalDurationSeconds 2210 'Progress duration'
    Write-Output 'PASS finish, 59 points, rank 37, six-point rival, history and progress.'
}
