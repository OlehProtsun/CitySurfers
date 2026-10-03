param(
    [Parameter(Mandatory)][string]$BaseUrl,
    [string]$FrontendOrigin,
    [switch]$FullDemo
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/Stage6.SmokeHelpers.ps1"
$uri = $null
if (-not [Uri]::TryCreate($BaseUrl, [UriKind]::Absolute, [ref]$uri) -or
    $uri.Scheme -notin @('http', 'https') -or $uri.UserInfo -or $uri.Query -or $uri.Fragment) {
    throw 'BaseUrl must be an absolute HTTP(S) URL without credentials, query or fragment.'
}
$base = $BaseUrl.TrimEnd('/')
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(15)
try {
    Test-ReadContracts
    if ($FrontendOrigin) { Test-Cors $FrontendOrigin }
    if ($FullDemo) { Test-FullDemo }
    Write-Output 'PASS Stage 6 remote smoke.'
} catch {
    # Do not print server bodies, request headers or exception details that may contain secrets.
    Write-Error 'Remote smoke failed: HTTP, connectivity or contract check. See the last completed step.' -ErrorAction Continue
    exit 1
} finally {
    $client.Dispose()
}
