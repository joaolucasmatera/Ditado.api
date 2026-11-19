param(
    [Parameter(Mandatory)]
    [ValidatePattern('^ditado_per(64|65)_[a-z0-9]{8,24}$')]
    [string]$Schema,
    [Parameter(Mandatory)]
    [string]$ArtifactsPath,
    [switch]$Retomar
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$port = if ($env:DITADO_LLM_DEMO_PORT) { [int]$env:DITADO_LLM_DEMO_PORT } else { 5064 }
if ($port -notin 5064, 5065) { throw 'A demonstração usa apenas as portas 5064 e 5065.' }
$logs = Join-Path $root 'work\logs'
$pidFile = Join-Path $logs "$Schema.pid"
$sdk = Join-Path $env:USERPROFILE '.dotnet9\dotnet.exe'
$env:DOTNET_ROOT = Split-Path -Parent $sdk
$env:ASPNETCORE_ENVIRONMENT = 'Testing'
$env:Llm__Enabled = 'true'
$env:Llm__Simulated = 'true'
$env:Jwt__ChaveSecreta = 'fixture-per64-exclusiva-da-api-local-nao-usar-em-producao'
$dbPassword = (& docker exec ditado-mysql sh -c 'printf %s "$MYSQL_ROOT_PASSWORD"').Trim()
if (!$dbPassword) { throw 'MySQL local sem configuração de teste.' }
$env:ConnectionStrings__ConexaoPadrao = "Server=127.0.0.1;Port=3306;Database=$Schema;User=root;Password=$dbPassword;"
$api = Join-Path $ArtifactsPath 'bin\Ditado.API\debug'
$dll = Join-Path $api 'Ditado.API.dll'

function Invoke-FixtureSql([string]$sql) {
    $sql | & docker exec -i ditado-mysql sh -c 'MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysql --batch --skip-column-names -uroot'
    if ($LASTEXITCODE -ne 0) { throw 'SQL da fixture PER-64 falhou.' }
}

if ($Retomar) {
    $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction Stop
    $ownedPid = [int](Get-Content -LiteralPath $pidFile)
    if ($listener.OwningProcess -ne $ownedPid) { throw 'Porta de demonstração ocupada por outro processo.' }
    Stop-Process -Id $ownedPid -Force
    $claims = Invoke-FixtureSql ("USE {0}; SELECT COUNT(DISTINCT s.RespostaDitadoId) FROM LlmAnalisesTarefas t JOIN RespostaSegmentos s ON s.Id=t.RespostaSegmentoId WHERE t.Estado=2;" -f $Schema)
    if ($claims -ne '1') { throw 'A demonstração esperava uma tentativa interrompida.' }
    Invoke-FixtureSql ("USE {0}; UPDATE LlmAnalisesTarefas SET IniciadaEm=UTC_TIMESTAMP(6)-INTERVAL 6 MINUTE WHERE Estado=2;" -f $Schema)
    Write-Output 'Retomada: uma tentativa interrompida; vencimento da reserva simulado no MySQL.'
} else {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        throw 'Porta de demonstração ocupada.'
    }
    Invoke-FixtureSql ("CREATE DATABASE {0} CHARACTER SET utf8mb4;" -f $Schema)
    $ef = Join-Path $env:USERPROFILE '.dotnet\tools\.store\dotnet-ef\10.0.13\dotnet-ef\10.0.13\tools\net8.0\any\tools\net8.0\any\ef.dll'
    $efArgs = @('exec','--depsfile',(Join-Path $api 'Ditado.API.deps.json'),
        '--runtimeconfig',(Join-Path $api 'Ditado.API.runtimeconfig.json'),
        '--additionalprobingpath',(Join-Path $env:USERPROFILE '.nuget\packages'),$ef)
    $project = @('--assembly',(Join-Path $api 'Ditado.Infra.dll'),'--startup-assembly',$dll,
        '--working-dir',$root,'--context','DitadoDbContext')
    & $sdk @efArgs database update 20261117050554_IntegrarCatalogoCor01 @project
    if ($LASTEXITCODE -ne 0) { throw 'Migrations da base falharam.' }
    # Os builds antigos não descobrem as três migrations de cadastro.
    $colunas = Invoke-FixtureSql ("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA='{0}' AND TABLE_NAME='Usuarios' AND COLUMN_NAME IN ('CodigoCadastro','PinConfigurado','NumeroChamada');" -f $Schema)
    if ($colunas -eq '0') {
        Invoke-FixtureSql ("USE {0}; ALTER TABLE Usuarios ADD CodigoCadastro varchar(32) NULL, ADD PinConfigurado tinyint(1) NOT NULL DEFAULT 0, ADD NumeroChamada int NULL, ADD UNIQUE INDEX IX_Usuarios_CodigoCadastro (CodigoCadastro);" -f $Schema)
    } elseif ($colunas -ne '3') {
        throw 'A fixture encontrou uma parte das colunas legadas de cadastro.'
    }
    & $sdk @efArgs database update @project
    if ($LASTEXITCODE -ne 0) { throw 'Migration da PER-64 falhou.' }
    Invoke-FixtureSql ("USE {0}; SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId LIKE '%ClassificacaoHibridaPer64';" -f $Schema)
}

$process = Start-Process -FilePath $sdk -ArgumentList ('"{0}" --urls http://127.0.0.1:{1}' -f $dll, $port) `
    -WorkingDirectory (Join-Path $root 'Ditado.API') -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $logs "$Schema.stdout.log") -RedirectStandardError (Join-Path $logs "$Schema.stderr.log")
Set-Content -LiteralPath $pidFile -Value $process.Id
Write-Output "API de demonstração em http://127.0.0.1:$port (PID $($process.Id))."
