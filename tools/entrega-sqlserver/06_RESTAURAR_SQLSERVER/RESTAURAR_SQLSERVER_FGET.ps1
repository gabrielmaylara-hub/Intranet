[CmdletBinding()]
param(
    [switch]$ValidarSolo
)

$ErrorActionPreference = "Stop"

function Write-Title {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host " RESTAURADOR INTRANET FGET - SQL SERVER 2022" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
}

function Write-Step {
    param(
        [int]$Number,
        [string]$Title
    )
    Write-Host ""
    Write-Host ("PASO {0} DE 8: {1}" -f $Number, $Title) -ForegroundColor Cyan
    Write-Host "------------------------------------------------------------"
}

function Write-Ok {
    param(
        [string]$Label,
        [string]$Value = ""
    )
    Write-Host "[OK] $Label" -ForegroundColor Green
    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        Write-Host "     $Value"
    }
}

function Get-FriendlySqlConnectionMessage {
    param(
        [string]$Instance,
        [string]$AuthMode,
        [string]$OriginalMessage
    )

    return @"
No se pudo conectar con SQL Server.

La instancia configurada no esta disponible:
$Instance

Revise lo siguiente:
1. SQL Server 2022 esta instalado.
2. El servicio de SQL Server esta iniciado.
3. El nombre de la instancia es correcto.
4. La autenticacion seleccionada corresponde al servidor.
5. La cuenta usada tiene permisos de conexion.

Este error ocurre antes de restaurar la base de datos.
La base de datos no fue modificada.

Detalle tecnico:
$OriginalMessage
"@
}

function Read-WithDefault {
    param(
        [string]$Prompt,
        [string]$Default
    )

    $value = Read-Host "$Prompt [$Default]"
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $Default
    }

    return $value.Trim()
}

function Escape-SqlLiteral {
    param([string]$Value)
    return ($Value -replace "'", "''")
}

function Escape-SqlIdentifier {
    param([string]$Value)
    return ($Value -replace "]", "]]")
}

function Find-SqlCmd {
    $found = New-Object System.Collections.Generic.List[string]

    foreach ($cmdName in @("sqlcmd.exe", "sqlcmd")) {
        $cmd = Get-Command $cmdName -ErrorAction SilentlyContinue
        if ($cmd -and $cmd.Source -and -not $found.Contains($cmd.Source)) {
            $found.Add($cmd.Source) | Out-Null
        }
    }

    $directCandidates = @(
        "C:\Program Files\SqlCmd\sqlcmd.exe",
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\sqlcmd.exe",
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\190\Tools\Binn\sqlcmd.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\sqlcmd.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\Client SDK\ODBC\190\Tools\Binn\sqlcmd.exe",
        (Join-Path (Split-Path -Parent $PSScriptRoot) "08_PRERREQUISITOS\sqlcmd\sqlcmd.exe")
    )

    foreach ($candidate in $directCandidates) {
        if ((Test-Path -LiteralPath $candidate) -and -not $found.Contains($candidate)) {
            $found.Add($candidate) | Out-Null
        }
    }

    $searchRoots = @(
        "C:\Program Files\Microsoft SQL Server",
        "C:\Program Files (x86)\Microsoft SQL Server"
    )

    foreach ($root in $searchRoots) {
        if (Test-Path -LiteralPath $root) {
            Get-ChildItem -LiteralPath $root -Filter "sqlcmd.exe" -Recurse -ErrorAction SilentlyContinue |
                Select-Object -First 20 |
                ForEach-Object {
                    if (-not $found.Contains($_.FullName)) {
                        $found.Add($_.FullName) | Out-Null
                    }
                }
        }
    }

    if ($found.Count -eq 0) {
        Write-Host ""
        Write-Host "No se encontro sqlcmd.exe." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "sqlcmd sirve para restaurar y validar la base, pero no instala SQL Server."
        Write-Host ""
        Write-Host "Opciones:"
        Write-Host "1. Instalar Microsoft sqlcmd o SQL Server Command Line Utilities."
        Write-Host "2. Usar el sqlcmd incluido en 08_PRERREQUISITOS si existe."
        Write-Host "3. Restaurar manualmente el .bak desde SSMS y despues usar el asistente para validar."
        Write-Host "4. Capturar manualmente la ruta completa de sqlcmd.exe."
        Write-Host ""
        $manual = Read-Host "Pegue la ruta completa de sqlcmd.exe, o presione Enter para cancelar"
        if (-not [string]::IsNullOrWhiteSpace($manual) -and (Test-Path -LiteralPath $manual)) {
            return $manual.Trim()
        }

        throw "No se encontro sqlcmd.exe. Instale Microsoft sqlcmd, use el sqlcmd incluido en 08_PRERREQUISITOS o restaure el .bak desde SSMS."
    }

    if ($found.Count -eq 1) {
        return $found[0]
    }

    Write-Host ""
    Write-Host "Se encontraron varios clientes sqlcmd:" -ForegroundColor Yellow
    for ($i = 0; $i -lt $found.Count; $i++) {
        Write-Host "$($i + 1). $($found[$i])"
    }

    do {
        $selection = Read-Host "Seleccione el numero de sqlcmd a usar"
        $number = 0
        $valid = [int]::TryParse($selection, [ref]$number) -and $number -ge 1 -and $number -le $found.Count
    } until ($valid)

    return $found[$number - 1]
}

function Get-BackupFile {
    param([string]$BackupDir)

    if (-not (Test-Path -LiteralPath $BackupDir)) {
        return $null
    }

    $bak = Get-ChildItem -LiteralPath $BackupDir -File -Filter "*.bak" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($bak) {
        return $bak
    }

    return Get-ChildItem -LiteralPath $BackupDir -File -Filter "*.sql" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Build-SqlCmdArgs {
    param(
        [string]$Database,
        [string]$Query,
        [string]$InputFile,
        [switch]$NoHeader
    )

    $args = @("-S", $script:Instance)

    if ($script:AuthMode -eq "Windows") {
        $args += "-E"
    } else {
        $args += @("-U", $script:SqlUser)
    }

    if (-not [string]::IsNullOrWhiteSpace($Database)) {
        $args += @("-d", $Database)
    }

    $args += @("-b", "-W", "-s", "|")

    if ($NoHeader) {
        $args += @("-h", "-1")
    }

    if (-not [string]::IsNullOrWhiteSpace($Query)) {
        $args += @("-Q", $Query)
    }

    if (-not [string]::IsNullOrWhiteSpace($InputFile)) {
        $args += @("-i", $InputFile)
    }

    return $args
}

function Invoke-SqlText {
    param(
        [string]$Database = "master",
        [string]$Query,
        [switch]$NoHeader
    )

    $args = Build-SqlCmdArgs -Database $Database -Query $Query -NoHeader:$NoHeader
    $output = & $script:SqlCmdPath @args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ($output -join [Environment]::NewLine)
    }

    return @($output)
}

function Invoke-SqlFile {
    param(
        [string]$Database,
        [string]$InputFile
    )

    $args = Build-SqlCmdArgs -Database $Database -InputFile $InputFile
    $output = & $script:SqlCmdPath @args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ($output -join [Environment]::NewLine)
    }

    return @($output)
}

function Get-SqlScalar {
    param(
        [string]$Database = "master",
        [string]$Query
    )

    $lines = Invoke-SqlText -Database $Database -Query "SET NOCOUNT ON; $Query" -NoHeader
    foreach ($line in $lines) {
        $value = ([string]$line).Trim()
        if ($value -and $value -notmatch "^\(\d+ rows affected\)$") {
            return $value
        }
    }

    return $null
}

function Set-TemporarySqlPassword {
    param([securestring]$SecurePassword)

    if (-not $SecurePassword) {
        return
    }

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
    try {
        $env:SQLCMDPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Confirm-OverwriteDatabase {
    param([string]$DatabaseName)

    Write-Host ""
    Write-Host "La base $DatabaseName ya existe." -ForegroundColor Yellow
    Write-Host "Para sobrescribirla debe escribir exactamente: RESTAURAR" -ForegroundColor Yellow
    Write-Host "Si no escribe RESTAURAR, el proceso se cancelara sin modificar la base." -ForegroundColor Yellow
    $confirmation = Read-Host "Para continuar escriba exactamente RESTAURAR"
    return $confirmation -eq "RESTAURAR"
}

function Restore-FromSqlFile {
    param(
        [string]$DatabaseName,
        [string]$SqlFile,
        [bool]$DatabaseExists
    )

    $dbEsc = Escape-SqlIdentifier $DatabaseName

    if ($DatabaseExists) {
        Invoke-SqlText -Database "master" -Query "ALTER DATABASE [$dbEsc] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$dbEsc];" | Out-Null
    }

    Invoke-SqlText -Database "master" -Query "CREATE DATABASE [$dbEsc];" | Out-Null
    Invoke-SqlFile -Database $DatabaseName -InputFile $SqlFile | Out-Null
}

function Get-DefaultSqlDirectory {
    param([string]$Kind)

    $property = if ($Kind -eq "Log") { "InstanceDefaultLogPath" } else { "InstanceDefaultDataPath" }
    $path = Get-SqlScalar -Database "master" -Query "SELECT COALESCE(CAST(SERVERPROPERTY(N'$property') AS nvarchar(4000)), N'');"

    if (-not [string]::IsNullOrWhiteSpace($path)) {
        return $path
    }

    $fileId = if ($Kind -eq "Log") { 2 } else { 1 }
    $fallback = Get-SqlScalar -Database "master" -Query "SELECT LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name)) + 1) FROM sys.master_files WHERE database_id = 1 AND file_id = $fileId;"
    if ([string]::IsNullOrWhiteSpace($fallback)) {
        throw "No se pudo detectar la ruta predeterminada de archivos SQL Server."
    }

    return $fallback
}

function Restore-FromBakFile {
    param(
        [string]$DatabaseName,
        [string]$BakFile,
        [bool]$DatabaseExists
    )

    $bakEsc = Escape-SqlLiteral $BakFile
    $dbEsc = Escape-SqlIdentifier $DatabaseName
    $dataDir = Get-DefaultSqlDirectory -Kind "Data"
    $logDir = Get-DefaultSqlDirectory -Kind "Log"

    $fileList = Invoke-SqlText -Database "master" -Query "RESTORE FILELISTONLY FROM DISK = N'$bakEsc';" -NoHeader
    $moveClauses = New-Object System.Collections.Generic.List[string]
    $dataIndex = 0
    $logIndex = 0

    foreach ($row in $fileList) {
        $text = ([string]$row).Trim()
        if (-not $text -or $text -match "^\(\d+ rows affected\)$") {
            continue
        }

        $parts = $text -split "\|"
        if ($parts.Count -lt 3) {
            continue
        }

        $logicalName = $parts[0].Trim()
        $fileType = $parts[2].Trim()
        if ([string]::IsNullOrWhiteSpace($logicalName)) {
            continue
        }

        $safeLogical = ($logicalName -replace "[^\w\-]", "_")
        if ($fileType -eq "L") {
            $logIndex++
            $target = Join-Path $logDir ("{0}_{1}.ldf" -f $DatabaseName, $safeLogical)
        } else {
            $dataIndex++
            $ext = if ($dataIndex -eq 1) { "mdf" } else { "ndf" }
            $target = Join-Path $dataDir ("{0}_{1}.{2}" -f $DatabaseName, $safeLogical, $ext)
        }

        $moveClauses.Add("MOVE N'$(Escape-SqlLiteral $logicalName)' TO N'$(Escape-SqlLiteral $target)'") | Out-Null
    }

    if ($moveClauses.Count -eq 0) {
        throw "No se pudieron detectar archivos logicos dentro del .bak."
    }

    $setSingleUser = ""
    if ($DatabaseExists) {
        $setSingleUser = "ALTER DATABASE [$dbEsc] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;"
    }

    $restoreSql = @"
$setSingleUser
RESTORE DATABASE [$dbEsc]
FROM DISK = N'$bakEsc'
WITH REPLACE, RECOVERY, $($moveClauses -join ", ");
ALTER DATABASE [$dbEsc] SET MULTI_USER;
"@

    Invoke-SqlText -Database "master" -Query $restoreSql | Out-Null
}

function Get-Counts {
    param([string]$DatabaseName)

    $tables = @(
        "usuarios_admin",
        "areas_publicacion",
        "configuracion_sitio",
        "accesos_rapidos",
        "sitio_enlaces",
        "archivos_seccion",
        "avisos",
        "tutoriales",
        "directorio_areas",
        "directorio"
    )

    $counts = New-Object System.Collections.Generic.List[string]
    foreach ($table in $tables) {
        $tableEsc = Escape-SqlIdentifier $table
        $query = "IF OBJECT_ID(N'$table', N'U') IS NULL SELECT N'$table|NO_EXISTE'; ELSE SELECT N'$table|' + CONVERT(nvarchar(30), COUNT(*)) FROM [$tableEsc];"
        $counts.Add((Get-SqlScalar -Database $DatabaseName -Query $query)) | Out-Null
    }

    return @($counts)
}

function Write-RestoreReport {
    param(
        [string]$ReportPath,
        [string]$Instance,
        [string]$DatabaseName,
        [string]$AuthMode,
        [string]$BackupPath,
        [string]$Result,
        [string[]]$Counts,
        [string]$ErrorMessage = ""
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("Reporte restauracion SQL Server Intranet FGET") | Out-Null
    $lines.Add("Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')") | Out-Null
    $lines.Add("Instancia: $Instance") | Out-Null
    $lines.Add("Base restaurada: $DatabaseName") | Out-Null
    $lines.Add("Autenticacion: $AuthMode, sin contrasena") | Out-Null
    $lines.Add("Respaldo usado: $BackupPath") | Out-Null
    $lines.Add("Resultado: $Result") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("Conteos:") | Out-Null
    foreach ($count in $Counts) {
        $lines.Add("- $count") | Out-Null
    }
    $lines.Add("") | Out-Null
    $lines.Add("Errores:") | Out-Null
    if ([string]::IsNullOrWhiteSpace($ErrorMessage)) {
        $lines.Add("- Ninguno") | Out-Null
    } else {
        $lines.Add("- $ErrorMessage") | Out-Null
    }

    Set-Content -LiteralPath $ReportPath -Value $lines -Encoding UTF8
}

function Write-FinalSummary {
    param(
        [string]$Result,
        [string]$Instance,
        [string]$DatabaseName,
        [string]$AuthMode,
        [string]$BackupPath,
        [string]$Site,
        [string]$ReportPath
    )

    Write-Host ""
    Write-Host "============================================================"
    Write-Host "RESUMEN FINAL"
    Write-Host "============================================================"
    Write-Host "Resultado             : $Result"
    Write-Host "Instancia SQL Server  : $Instance"
    Write-Host "Base de datos         : $DatabaseName"
    Write-Host "Autenticacion         : $AuthMode, sin contrasena"
    Write-Host "Respaldo usado        : $BackupPath"
    Write-Host "Sitio                 : $Site"
    Write-Host "Reporte generado en   : $ReportPath"
    Write-Host ""
    Write-Host "Si necesita seguimiento, copie el resumen y el diagnostico completo."
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$EntregaRoot = Split-Path -Parent $ScriptDir
$ProjectPath = Join-Path $EntregaRoot "01_PROYECTO"
$SqlBackupDir = Join-Path $EntregaRoot "02_BASE_DE_DATOS_SQLSERVER"
$ReportsPath = Join-Path $EntregaRoot "05_REPORTES_VALIDACION"
$StoragePath1 = Join-Path $EntregaRoot "03_STORAGE\Storage"
$StoragePath2 = Join-Path $ProjectPath "Storage"

$DefaultInstance = ".\SQLEXPRESS"
$DefaultDatabase = "intranet_fget"
$DefaultWebPort = "5077"
$Provider = "SqlServer"

Write-Title

Write-Step 1 "DETECCION DE ARCHIVOS"
Write-Ok "Carpeta de entrega detectada:" $EntregaRoot
Write-Ok "Proyecto detectado:" $ProjectPath
Write-Ok "Carpeta de respaldos detectada:" $SqlBackupDir
if (Test-Path -LiteralPath $StoragePath2) {
    Write-Ok "Storage detectado:" $StoragePath2
    Write-Host "     Se usara al ejecutar desde 01_PROYECTO."
    Write-Host "     No borre Storage."
}
elseif (Test-Path -LiteralPath $StoragePath1) {
    Write-Ok "Storage detectado como respaldo de entrega:" $StoragePath1
    Write-Host "     La app usa 01_PROYECTO\\Storage o 09_APP_PUBLICADA\\Storage segun como se levante."
    Write-Host "     No borre Storage; confirme la ruta antes de levantar la app."
}
else {
    Write-Host "[ADVERTENCIA] No se detecto Storage en las rutas esperadas." -ForegroundColor Yellow
    Write-Host "     Revise 01_PROYECTO\\Storage, 09_APP_PUBLICADA\\Storage o 03_STORAGE\\Storage."
}

New-Item -ItemType Directory -Force -Path $ReportsPath | Out-Null

$backupPathForReport = "N/A"
$backup = Get-BackupFile -BackupDir $SqlBackupDir

if ($backup) {
    $backupPathForReport = $backup.FullName
    Write-Ok "Respaldo de base de datos detectado:" $backup.FullName
} else {
    Write-Host "No se encontro respaldo .bak ni .sql en $SqlBackupDir" -ForegroundColor Yellow
}

$script:SqlCmdPath = $null
try {
    Write-Step 2 "VALIDACION DE REQUISITOS"
    $script:SqlCmdPath = Find-SqlCmd
    Write-Ok "sqlcmd detectado:" $script:SqlCmdPath
    Write-Host "El restaurador no instala el motor SQL Server. SQL Server 2022 debe estar instalado o disponible en una instancia remota."
}
catch {
    Write-Host ""
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    if ($ValidarSolo) {
        Write-Host ""
        Write-Host "Validacion de restaurador completada. Falta instalar sqlcmd antes de restaurar." -ForegroundColor Cyan
        exit 0
    }

    throw
}

if ($ValidarSolo) {
    Write-Host ""
    Write-Host "Validacion de restaurador completada. No se restauro la base por -ValidarSolo." -ForegroundColor Cyan
    exit 0
}

if (-not $backup) {
    throw "No existe respaldo para restaurar. Coloque un .bak o .sql en 02_BASE_DE_DATOS_SQLSERVER."
}

$useDefaults = ""
Write-Host ""
Write-Host "------------------------------------------------------------"
Write-Host "VALORES SUGERIDOS PARA LA RESTAURACION"
Write-Host "------------------------------------------------------------"
Write-Host ("Instancia SQL Server : {0}" -f $DefaultInstance)
Write-Host ("Base de datos        : {0}" -f $DefaultDatabase)
Write-Host ("Puerto web           : {0}" -f $DefaultWebPort)
Write-Host ("Proveedor            : {0}" -f $Provider)
Write-Host ""
Write-Host "Estos valores se usaran para probar la conexion, restaurar la base y levantar la aplicacion."
Write-Host ""
Write-Host "La base de datos todavia no sera modificada. La restauracion requiere confirmacion posterior."
Write-Host ""
Write-Host "Desea continuar con estos valores?"
Write-Host ""
Write-Host "[S] Si, usar estos valores y continuar"
Write-Host "[N] No, capturar otros valores"
$useDefaults = Read-Host "Seleccione una opcion [S/N]"
if ([string]::IsNullOrWhiteSpace($useDefaults) -or $useDefaults.Trim().ToUpperInvariant() -eq "S") {
    $script:Instance = $DefaultInstance
    $databaseName = $DefaultDatabase
    $webPort = $DefaultWebPort
    Write-Host ""
    Write-Host "Continuando con los valores sugeridos." -ForegroundColor Cyan
    Write-Host "La base de datos todavia no sera modificada. La restauracion requiere confirmacion posterior."
} else {
    Write-Host ""
    Write-Host "Captura manual de valores." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Ejemplos de instancia SQL Server:"
    Write-Host "- .\SQLEXPRESS"
    Write-Host "- localhost"
    Write-Host "- ."
    Write-Host "- SERVIDOR\INSTANCIA"
    $script:Instance = Read-WithDefault -Prompt "Instancia SQL Server" -Default $DefaultInstance
    $databaseName = Read-WithDefault -Prompt "Nombre de base" -Default $DefaultDatabase
    $webPort = Read-WithDefault -Prompt "Puerto web" -Default $DefaultWebPort
}

Write-Host ""
Write-Host "Metodo de autenticacion:"
Write-Host "1. Windows Authentication"
Write-Host "2. SQL Authentication"
do {
    $authSelection = Read-Host "Seleccione 1 o 2"
} until ($authSelection -in @("1", "2"))

$previousSqlCmdPassword = $env:SQLCMDPASSWORD
$script:AuthMode = if ($authSelection -eq "1") { "Windows" } else { "SQL" }
$script:SqlUser = ""

try {
    if ($script:AuthMode -eq "SQL") {
        $script:SqlUser = Read-Host "Usuario SQL Server"
        $securePassword = Read-Host "Contrasena SQL Server" -AsSecureString
        Set-TemporarySqlPassword -SecurePassword $securePassword
    }

    Write-Step 3 "CONEXION A SQL SERVER"
    Write-Host "Probando conexion..."
    $version = Get-SqlScalar -Database "master" -Query "SELECT @@VERSION;"
    Write-Ok "Conexion correcta."

    $dbLiteral = Escape-SqlLiteral $databaseName
    $dbExistsValue = Get-SqlScalar -Database "master" -Query "SELECT CASE WHEN DB_ID(N'$dbLiteral') IS NULL THEN 0 ELSE 1 END;"
    $databaseExists = $dbExistsValue -eq "1"

    if ($databaseExists -and -not (Confirm-OverwriteDatabase -DatabaseName $databaseName)) {
        Write-Host "Operacion cancelada. No se modifico la base." -ForegroundColor Yellow
        exit 0
    }

    Write-Step 4 "RESTAURACION DE BASE"
    Write-Host "Restaurando base $databaseName desde $($backup.Name)..."
    if ($backup.Extension.Equals(".bak", [StringComparison]::OrdinalIgnoreCase)) {
        Restore-FromBakFile -DatabaseName $databaseName -BakFile $backup.FullName -DatabaseExists $databaseExists
    } else {
        Restore-FromSqlFile -DatabaseName $databaseName -SqlFile $backup.FullName -DatabaseExists $databaseExists
    }

    Write-Step 5 "VALIDACION DE BASE"
    $counts = Get-Counts -DatabaseName $databaseName
    $reportPath = Join-Path $ReportsPath "REPORTE_RESTAURACION_SQLSERVER.txt"
    Write-RestoreReport -ReportPath $reportPath -Instance $script:Instance -DatabaseName $databaseName -AuthMode $script:AuthMode -BackupPath $backupPathForReport -Result "OK" -Counts $counts

    Write-Step 6 "PREPARACION DE APP"
    Write-Ok "Restauracion finalizada correctamente."
    Write-Ok "Reporte generado en:" $reportPath
    Write-Host ""
    Write-Host "Para levantar la app configure:"
    Write-Host "DatabaseProvider=$Provider"
    if ($script:AuthMode -eq "Windows") {
        Write-Host "ConnectionStrings__SqlServer=Server=$script:Instance;Database=$databaseName;Trusted_Connection=True;TrustServerCertificate=True;"
    } else {
        Write-Host "ConnectionStrings__SqlServer=Server=$script:Instance;Database=$databaseName;User Id=<usuario>;Password=<capturar>;TrustServerCertificate=True;"
    }
    Write-Host "ASPNETCORE_URLS=http://127.0.0.1:$webPort"
    Write-Step 7 "VALIDACION DEL SITIO"
    Write-Host "Este modo consola restaura y valida la base."
    Write-Host "Para levantar la app, validar rutas y abrir el sitio, use EJECUTAR_ASISTENTE.bat o 09_APP_PUBLICADA\\EJECUTAR_APP_PUBLICADA.bat."
    Write-Step 8 "REPORTE FINAL"
    Write-FinalSummary -Result "Correcto" -Instance $script:Instance -DatabaseName $databaseName -AuthMode $script:AuthMode -BackupPath $backupPathForReport -Site "http://127.0.0.1:$webPort" -ReportPath $reportPath
}
catch {
    $reportPath = Join-Path $ReportsPath "REPORTE_RESTAURACION_SQLSERVER.txt"
    $errorMessage = $_.Exception.Message
    if ($errorMessage -match "pipe|SQLLocal|Login failed|network|server was not found|could not open|timeout|Timed out") {
        $errorMessage = Get-FriendlySqlConnectionMessage -Instance $script:Instance -AuthMode $script:AuthMode -OriginalMessage $_.Exception.Message
    }
    Write-RestoreReport -ReportPath $reportPath -Instance $script:Instance -DatabaseName $databaseName -AuthMode $script:AuthMode -BackupPath $backupPathForReport -Result "ERROR" -Counts @() -ErrorMessage $errorMessage
    Write-Host ""
    Write-Host "Error durante la restauracion:" -ForegroundColor Red
    Write-Host $errorMessage -ForegroundColor Red
    Write-Host "Reporte: $reportPath"
    Write-FinalSummary -Result "Error" -Instance $script:Instance -DatabaseName $databaseName -AuthMode $script:AuthMode -BackupPath $backupPathForReport -Site "http://127.0.0.1:$webPort" -ReportPath $reportPath
    exit 1
}
finally {
    if ($null -eq $previousSqlCmdPassword) {
        Remove-Item Env:\SQLCMDPASSWORD -ErrorAction SilentlyContinue
    } else {
        $env:SQLCMDPASSWORD = $previousSqlCmdPassword
    }
}
