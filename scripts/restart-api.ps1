[CmdletBinding()]
param(
    [int]$Port = 5031,
    [string]$Schema = 'ditado_per41_validation_20261107',
    [string]$BuildOutput = 'work\proxy-api-build',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnetRoot = Join-Path $env:USERPROFILE '.dotnet9'
$dotnet = Join-Path $dotnetRoot 'dotnet.exe'
$msbuild = Join-Path $dotnetRoot 'sdk\9.0.320\MSBuild.dll'
$apiProject = Join-Path $repoRoot 'Ditado.API\Ditado.API.csproj'
$outputPath = Join-Path $repoRoot $BuildOutput
$logPath = Join-Path $repoRoot 'work\logs'

if (-not (Test-Path $dotnet)) { throw "SDK .NET 9 não encontrado em $dotnet" }
if (-not (Test-Path $apiProject)) { throw "Projeto da API não encontrado: $apiProject" }

$docker = Get-Command docker -ErrorAction SilentlyContinue
if (-not $docker) { throw 'Docker CLI não encontrado. Inicie o Docker Desktop antes de reiniciar a API.' }
$container = (& $docker.Source ps --filter 'name=^/ditado-mysql$' --filter 'status=running' --format '{{.Names}}').Trim()
if ($container -ne 'ditado-mysql') { throw 'O container ditado-mysql não está em execução.' }
$dbPassword = (& $docker.Source exec ditado-mysql sh -c 'printf %s "$MYSQL_ROOT_PASSWORD"').Trim()
if ([string]::IsNullOrWhiteSpace($dbPassword)) { throw 'Não foi possível obter a senha do MySQL no container.' }

$listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
foreach ($listener in $listeners) {
    $process = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
    if ($process -and $process.ProcessName -eq 'dotnet') { Stop-Process -Id $process.Id -Force }
}

if (-not $SkipBuild) {
    New-Item -ItemType Directory -Force $outputPath | Out-Null
    & $dotnet exec $msbuild $apiProject /t:Build /p:Configuration=Debug /p:OutputPath=$outputPath /p:OutDir=$outputPath /p:RestoreIgnoreFailedSources=true /p:MSBuildEnableWorkloadResolver=false /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build da API falhou (código $LASTEXITCODE)." }
}

$apiDll = Join-Path $outputPath 'Ditado.API.dll'
if (-not (Test-Path $apiDll)) { throw "DLL da API não encontrada: $apiDll" }
New-Item -ItemType Directory -Force $logPath | Out-Null
$stdout = Join-Path $logPath "api-$Port.stdout.log"
$stderr = Join-Path $logPath "api-$Port.stderr.log"
$env:ASPNETCORE_ENVIRONMENT = 'Testing'
$env:ConnectionStrings__ConexaoPadrao = "Server=127.0.0.1;Port=3306;Database=$Schema;User=root;Password=$dbPassword;"
$env:DOTNET_ROOT = $dotnetRoot
$env:PATH = "$dotnetRoot;$env:PATH"
$process = Start-Process -FilePath $dotnet -ArgumentList "`"$apiDll`" --urls http://127.0.0.1:$Port" -WorkingDirectory (Join-Path $repoRoot 'Ditado.API') -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru -WindowStyle Hidden
Write-Output "API iniciada: PID=$($process.Id) URL=http://127.0.0.1:$Port Schema=$Schema"
