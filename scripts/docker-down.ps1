$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root '.env.docker'
Push-Location $root
try {
    if (-not (Test-Path $envFile)) { docker stop ditado-api ditado-frontend 2>$null; return }
    $args = @('compose')
    if (Test-Path $envFile) { $args += @('--env-file', $envFile) }
    $args += @('stop', 'api', 'frontend')
    docker @args
}
finally { Pop-Location }
