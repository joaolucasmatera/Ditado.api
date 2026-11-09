[CmdletBinding()]
param(
    [switch]$Test
)

$ErrorActionPreference = 'Stop'

$dotnetCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'dotnet-sdk-9\dotnet.exe'),
    'C:\Program Files\dotnet\dotnet.exe'
)
$dotnet = $dotnetCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($dotnet)) {
    throw 'SDK .NET 9 não encontrado. Instale-o em %LOCALAPPDATA%\dotnet-sdk-9 ou na instalação padrão do .NET.'
}

$cliHome = Join-Path $env:TEMP 'ditado-dotnet-cli-home'
New-Item -ItemType Directory -Force -Path $cliHome | Out-Null
$env:DOTNET_CLI_HOME = $cliHome
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_NO_WORKLOAD_UPDATES = '1'

$restoreArgs = @(
    'restore',
    'Ditado.API/Ditado.API.csproj',
    '--disable-build-servers',
    '-p:MaxCpuCount=1',
    '-p:UseSharedCompilation=false',
    '-v:minimal'
)
& $dotnet @restoreArgs
if ($LASTEXITCODE -ne 0) { throw "Restore da API falhou (código $LASTEXITCODE)." }

$buildArgs = @(
    'build',
    'Ditado.API/Ditado.API.csproj',
    '--no-restore',
    '--disable-build-servers',
    '-p:MaxCpuCount=1',
    '-p:UseSharedCompilation=false',
    '-v:minimal'
)
& $dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "Build da API falhou (código $LASTEXITCODE)." }

if ($Test) {
    $testArgs = @(
        'test',
        'Ditado.Testes/Ditado.Testes.csproj',
        '--no-restore',
        '--no-build',
        '--disable-build-servers',
        '-p:MaxCpuCount=1',
        '-p:UseSharedCompilation=false',
        '-v:minimal'
    )
    & $dotnet @testArgs
    if ($LASTEXITCODE -ne 0) { throw "Testes da API falharam (código $LASTEXITCODE)." }
}
