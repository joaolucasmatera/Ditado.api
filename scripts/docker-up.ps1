[CmdletBinding()]
param([switch]$Build)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root '.env.docker'
$docker = Get-Command docker -ErrorAction Stop
$container = (& $docker.Source ps --filter 'name=^/ditado-mysql$' --filter 'status=running' --format '{{.Names}}').Trim()
if ($container -ne 'ditado-mysql') { throw 'O container ditado-mysql precisa estar ativo.' }

if (-not (Test-Path $envFile)) {
    $env:MYSQL_ROOT_PASSWORD = (& $docker.Source exec ditado-mysql sh -c 'printf %s "$MYSQL_ROOT_PASSWORD"').Trim()
    if ([string]::IsNullOrWhiteSpace($env:MYSQL_ROOT_PASSWORD)) { throw 'Não foi possível obter a senha do MySQL no container.' }
    if ([string]::IsNullOrWhiteSpace($env:MYSQL_DATABASE)) { $env:MYSQL_DATABASE = 'ditado_per41_validation_20261107' }
    if ([string]::IsNullOrWhiteSpace($env:JWT_SECRET)) {
        $env:JWT_SECRET = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
    }
    $envFile = $null
}
Push-Location $root
try {
    $args = @('compose')
    if ($envFile) { $args += @('--env-file', $envFile) }
    $args += @('up', '-d')
    if ($Build) { $args += '--build' }
    & $docker.Source @args
    if ($LASTEXITCODE -ne 0) { throw "docker compose up falhou (código $LASTEXITCODE)." }
    if ($envFile) { & $docker.Source compose --env-file $envFile ps }
    else { & $docker.Source compose ps }
}
finally { Pop-Location }
