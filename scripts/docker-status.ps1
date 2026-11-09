$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root '.env.docker'
Push-Location $root
try {
    docker ps --filter 'name=ditado-' --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
    if (-not (Test-Path $envFile)) { return }
    $args = @('compose')
    if (Test-Path $envFile) { $args += @('--env-file', $envFile) }
    $args += 'ps'
    docker @args
}
finally { Pop-Location }
