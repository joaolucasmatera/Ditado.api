param([string]$Service = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root '.env.docker'
Push-Location $root
try {
    if (-not (Test-Path $envFile)) {
        if ([string]::IsNullOrWhiteSpace($Service)) { docker logs -f --tail 200 ditado-api; return }
        docker logs -f --tail 200 ("ditado-" + $Service); return
    }
    $args = @('compose')
    if (Test-Path $envFile) { $args += @('--env-file', $envFile) }
    $args += @('logs', '-f', '--tail=200')
    if (-not [string]::IsNullOrWhiteSpace($Service)) { $args += $Service }
    docker @args
}
finally { Pop-Location }
