[CmdletBinding()]
param(
    [switch]$SelfTest,
    [switch]$SmokeOpen
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$EntregaRoot = Split-Path -Parent $ScriptDir
$ProjectPath = Join-Path $EntregaRoot "01_PROYECTO"
$PublishedPath = Join-Path $EntregaRoot "09_APP_PUBLICADA"
$PublishedDll = Join-Path $PublishedPath "Intranet.dll"
$BackupDir = Join-Path $EntregaRoot "02_BASE_DE_DATOS_SQLSERVER"
$ReportsPath = Join-Path $EntregaRoot "05_REPORTES_VALIDACION"
$StoragePath = Join-Path $ProjectPath "Storage"
$DefaultBackup = Join-Path $BackupDir "intranet_fget_sqlserver_2026-05-13.bak"
$GuiLogPath = Join-Path $ReportsPath "restore_sqlserver_gui.log"
$GuiReportPath = Join-Path $ReportsPath "REPORTE_ASISTENTE_SQLSERVER.txt"

if (-not (Test-Path -LiteralPath $ReportsPath)) {
    New-Item -ItemType Directory -Path $ReportsPath -Force | Out-Null
}

$script:SqlCmdPath = $null
$script:AppProcess = $null
$script:LastRestoreResult = "No ejecutada"
$script:LastCounts = @()
$script:LastSiteResults = @()
$script:LastErrors = New-Object System.Collections.Generic.List[string]
$script:ConnectionOk = $false
$script:DatabaseValidated = $false
$script:AppStartedOk = $false
$script:SiteValidatedOk = $false

function Mask-SensitiveText {
    param([string]$Text)
    if ($null -eq $Text) {
        return ""
    }

    $masked = $Text -replace '(?i)(Password\s*=\s*)([^;\s]+)', '$1<oculta>'
    $masked = $masked -replace '(?i)(SQLCMDPASSWORD\s*=\s*)(\S+)', '$1<oculta>'
    return $masked
}

function Add-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )

    $safeMessage = Mask-SensitiveText $Message
    $line = "[{0}] [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level.ToUpperInvariant(), $safeMessage
    if ($script:LogBox) {
        $script:LogBox.AppendText($line + [Environment]::NewLine)
        $script:LogBox.SelectionStart = $script:LogBox.TextLength
        $script:LogBox.ScrollToCaret()
        [System.Windows.Forms.Application]::DoEvents()
    }
    Add-Content -LiteralPath $GuiLogPath -Value $line -Encoding UTF8
}

function Add-ErrorLog {
    param([string]$Message)
    $safeMessage = Mask-SensitiveText $Message
    $script:LastErrors.Add($safeMessage) | Out-Null
    Add-Log $safeMessage -Level "ERROR"
}

function Add-OkLog {
    param([string]$Message)
    Add-Log $Message -Level "OK"
}

function Get-FriendlySqlConnectionMessage {
    param(
        [string]$Stage,
        [string]$Server,
        [string]$OriginalMessage
    )

    return @"
No se pudo conectar con SQL Server.

Etapa: $Stage

La instancia configurada no esta disponible:
$Server

Revise lo siguiente:
1. SQL Server 2022 esta instalado.
2. El servicio de SQL Server esta iniciado.
3. El nombre de la instancia es correcto.
4. La autenticacion seleccionada corresponde al servidor.
5. La cuenta usada tiene permisos de conexion.

Este error ocurre antes de restaurar la base de datos.
La base de datos no fue modificada.

Detalle tecnico:
$(Mask-SensitiveText $OriginalMessage)
"@
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

    foreach ($name in @("sqlcmd.exe", "sqlcmd")) {
        $cmd = Get-Command $name -ErrorAction SilentlyContinue
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

    foreach ($root in @("C:\Program Files\Microsoft SQL Server", "C:\Program Files (x86)\Microsoft SQL Server")) {
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

    if ($found.Count -gt 0) {
        return $found[0]
    }

    return $null
}

function Find-Dotnet {
    $cmd = Get-Command "dotnet.exe" -ErrorAction SilentlyContinue
    if ($cmd -and $cmd.Source) {
        return $cmd.Source
    }

    foreach ($candidate in @(
        "C:\Program Files\dotnet\dotnet.exe",
        "$env:USERPROFILE\.dotnet\dotnet.exe",
        "C:\Program Files (x86)\dotnet\dotnet.exe"
    )) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    return $null
}

function Find-DotnetWithSdk {
    foreach ($candidate in @(
        (Find-Dotnet),
        "$env:USERPROFILE\.dotnet\dotnet.exe",
        "C:\Recuperacion\tools\dotnet10\dotnet.exe",
        "C:\Program Files\dotnet\dotnet.exe",
        "C:\Program Files (x86)\dotnet\dotnet.exe"
    )) {
        if ([string]::IsNullOrWhiteSpace($candidate) -or -not (Test-Path -LiteralPath $candidate)) {
            continue
        }

        try {
            $sdks = & $candidate --list-sdks 2>$null
            if ($sdks) {
                return $candidate
            }
        }
        catch {
        }
    }

    return $null
}

function Add-InstanceCandidate {
    param(
        [System.Collections.Generic.List[string]]$List,
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $candidate = $Value.Trim()
    foreach ($item in $List) {
        if ($item.Equals($candidate, [System.StringComparison]::OrdinalIgnoreCase)) {
            return
        }
    }

    $List.Add($candidate) | Out-Null
}

function Get-DefaultSqlInstanceOptions {
    $options = New-Object System.Collections.Generic.List[string]
    Add-InstanceCandidate -List $options -Value ".\SQLEXPRESS"
    Add-InstanceCandidate -List $options -Value "localhost"
    Add-InstanceCandidate -List $options -Value "."
    Add-InstanceCandidate -List $options -Value "(local)"

    if (-not [string]::IsNullOrWhiteSpace($env:COMPUTERNAME)) {
        Add-InstanceCandidate -List $options -Value "$env:COMPUTERNAME\SQLEXPRESS"
    }

    return $options.ToArray()
}

function Get-DetectedSqlInstances {
    param([switch]$IncludeSqlCmd)

    $instances = New-Object System.Collections.Generic.List[string]

    try {
        $services = Get-Service -ErrorAction SilentlyContinue | Where-Object {
            $_.Name -eq "MSSQLSERVER" -or $_.Name -like "MSSQL`$*"
        }

        foreach ($svc in $services) {
            if ($svc.Name -eq "MSSQLSERVER") {
                Add-InstanceCandidate -List $instances -Value "localhost"
                Add-InstanceCandidate -List $instances -Value "."
                Add-InstanceCandidate -List $instances -Value "(local)"
            }
            elseif ($svc.Name -like "MSSQL`$*") {
                $instanceName = $svc.Name.Substring(6)
                Add-InstanceCandidate -List $instances -Value ".\$instanceName"
                if (-not [string]::IsNullOrWhiteSpace($env:COMPUTERNAME)) {
                    Add-InstanceCandidate -List $instances -Value "$env:COMPUTERNAME\$instanceName"
                }
            }
        }
    }
    catch {
    }

    foreach ($regPath in @(
        "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL"
    )) {
        try {
            if (-not (Test-Path -LiteralPath $regPath)) {
                continue
            }

            $props = Get-ItemProperty -LiteralPath $regPath -ErrorAction SilentlyContinue
            foreach ($prop in $props.PSObject.Properties) {
                if ($prop.Name -like "PS*") {
                    continue
                }

                if ($prop.Name -eq "MSSQLSERVER") {
                    Add-InstanceCandidate -List $instances -Value "localhost"
                    Add-InstanceCandidate -List $instances -Value "."
                    Add-InstanceCandidate -List $instances -Value "(local)"
                }
                else {
                    Add-InstanceCandidate -List $instances -Value ".\$($prop.Name)"
                    if (-not [string]::IsNullOrWhiteSpace($env:COMPUTERNAME)) {
                        Add-InstanceCandidate -List $instances -Value "$env:COMPUTERNAME\$($prop.Name)"
                    }
                }
            }
        }
        catch {
        }
    }

    if ($IncludeSqlCmd) {
        $sqlcmd = Find-SqlCmd
        if ($sqlcmd) {
            try {
                $psi = [System.Diagnostics.ProcessStartInfo]::new()
                $psi.FileName = $sqlcmd
                $psi.Arguments = "-L"
                $psi.UseShellExecute = $false
                $psi.RedirectStandardOutput = $true
                $psi.RedirectStandardError = $true
                $psi.CreateNoWindow = $true
                $process = [System.Diagnostics.Process]::Start($psi)
                if ($process.WaitForExit(7000)) {
                    $output = $process.StandardOutput.ReadToEnd()
                    foreach ($line in ($output -split "`r?`n")) {
                        $text = $line.Trim()
                        if ([string]::IsNullOrWhiteSpace($text) -or $text -match "^Servers:") {
                            continue
                        }
                        Add-InstanceCandidate -List $instances -Value $text
                    }
                }
                else {
                    $process.Kill()
                    $process.WaitForExit()
                }
            }
            catch {
            }
        }
    }

    return $instances.ToArray()
}

function Add-ComboItemUnique {
    param(
        $ComboBox,
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    $candidate = $Value.Trim()
    foreach ($item in $ComboBox.Items) {
        if ([string]::Equals([string]$item, $candidate, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
    }

    $ComboBox.Items.Add($candidate) | Out-Null
    return $true
}

function Get-AvailableSuggestedPorts {
    $available = New-Object System.Collections.Generic.List[int]
    foreach ($candidate in @(5080, 5088, 5090)) {
        $listener = Get-NetTCPConnection -LocalPort $candidate -State Listen -ErrorAction SilentlyContinue
        if (-not $listener) {
            $available.Add($candidate) | Out-Null
        }
    }

    return $available.ToArray()
}

function Get-SelectedAuth {
    if ($script:CmbAuth.SelectedItem -eq "SQL Authentication") {
        return "Sql"
    }

    return "Windows"
}

function Get-SqlPasswordPlain {
    if (Get-SelectedAuth -ne "Sql") {
        return $null
    }

    return $script:TxtSqlPassword.Text
}

function Clear-SqlPasswordField {
    if ($script:TxtSqlPassword) {
        $script:TxtSqlPassword.Clear()
    }
}

function Invoke-SqlText {
    param(
        [string]$Server,
        [string]$Database = "master",
        [string]$Query,
        [string]$SqlUser,
        [string]$SqlPassword,
        [switch]$UseSqlAuth,
        [int]$Timeout = 60,
        [string]$Separator = "",
        [switch]$NoHeader
    )

    if (-not $script:SqlCmdPath) {
        $script:SqlCmdPath = Find-SqlCmd
    }
    if (-not $script:SqlCmdPath) {
        throw "No se encontro sqlcmd.exe. Instale Microsoft sqlcmd o SQL Server Command Line Utilities."
    }

    $tempSql = Join-Path $env:TEMP ("intranet_sqlserver_gui_{0}.sql" -f ([Guid]::NewGuid().ToString("N")))
    Set-Content -LiteralPath $tempSql -Value $Query -Encoding UTF8

    $args = @("-S", $Server, "-d", $Database, "-b", "-C", "-l", "$Timeout")
    $oldPassword = $env:SQLCMDPASSWORD
    try {
        if ($UseSqlAuth) {
            if ([string]::IsNullOrWhiteSpace($SqlUser)) {
                throw "Debe capturar usuario SQL."
            }
            if ([string]::IsNullOrWhiteSpace($SqlPassword)) {
                throw "Debe capturar contrasena SQL."
            }

            $env:SQLCMDPASSWORD = $SqlPassword
            $args = @("-S", $Server, "-d", $Database, "-U", $SqlUser, "-b", "-C", "-l", "$Timeout")
        }
        else {
            $args = @("-S", $Server, "-d", $Database, "-E", "-b", "-C", "-l", "$Timeout")
        }

        if ($NoHeader) {
            $args += @("-h", "-1")
        }
        if (-not [string]::IsNullOrWhiteSpace($Separator)) {
            $args += @("-s", $Separator)
        }
        $args += @("-i", $tempSql)

        $output = & $script:SqlCmdPath @args 2>&1
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw (($output | Out-String).Trim())
        }

        return $output
    }
    finally {
        if ($null -eq $oldPassword) {
            Remove-Item Env:\SQLCMDPASSWORD -ErrorAction SilentlyContinue
        }
        else {
            $env:SQLCMDPASSWORD = $oldPassword
        }
        Remove-Item -LiteralPath $tempSql -Force -ErrorAction SilentlyContinue
    }
}

function Get-DatabaseExists {
    param([string]$Server, [string]$Database, [string]$SqlUser, [string]$SqlPassword, [switch]$UseSqlAuth)

    $db = Escape-SqlLiteral $Database
    $query = "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'$db') IS NULL THEN 'NO' ELSE 'YES' END AS Existe;"
    $output = Invoke-SqlText -Server $Server -Database "master" -Query $query -SqlUser $SqlUser -SqlPassword $SqlPassword -UseSqlAuth:$UseSqlAuth
    return (($output | Out-String) -match "YES")
}

function Confirm-OverwriteDatabase {
    param([string]$Database)

    $dialog = New-Object System.Windows.Forms.Form
    $dialog.Text = "Confirmar restauracion"
    $dialog.StartPosition = "CenterParent"
    $dialog.Size = New-Object System.Drawing.Size(520, 220)
    $dialog.FormBorderStyle = "FixedDialog"
    $dialog.MaximizeBox = $false
    $dialog.MinimizeBox = $false

    $label = New-Object System.Windows.Forms.Label
    $label.Text = "La base '$Database' ya existe.`r`nPara restaurar encima, escriba RESTAURAR."
    $label.Location = New-Object System.Drawing.Point(18, 18)
    $label.Size = New-Object System.Drawing.Size(470, 55)
    $dialog.Controls.Add($label)

    $input = New-Object System.Windows.Forms.TextBox
    $input.Location = New-Object System.Drawing.Point(22, 82)
    $input.Size = New-Object System.Drawing.Size(460, 24)
    $dialog.Controls.Add($input)

    $ok = New-Object System.Windows.Forms.Button
    $ok.Text = "Confirmar"
    $ok.Location = New-Object System.Drawing.Point(300, 126)
    $ok.Size = New-Object System.Drawing.Size(90, 32)
    $ok.Add_Click({
        if ($input.Text -eq "RESTAURAR") {
            $dialog.Tag = $true
            $dialog.Close()
        }
        else {
            [System.Windows.Forms.MessageBox]::Show("Debe escribir RESTAURAR exactamente.", "Confirmacion requerida", "OK", "Warning") | Out-Null
        }
    })
    $dialog.Controls.Add($ok)

    $cancel = New-Object System.Windows.Forms.Button
    $cancel.Text = "Cancelar"
    $cancel.Location = New-Object System.Drawing.Point(395, 126)
    $cancel.Size = New-Object System.Drawing.Size(90, 32)
    $cancel.Add_Click({
        $dialog.Tag = $false
        $dialog.Close()
    })
    $dialog.Controls.Add($cancel)

    $dialog.AcceptButton = $ok
    $dialog.CancelButton = $cancel
    $dialog.ShowDialog($script:Form) | Out-Null
    return [bool]$dialog.Tag
}

function Test-Requirements {
    Add-Log "Validando requisitos..."

    $items = @(
        @{ Name = "Respaldo"; Path = $script:TxtBackup.Text },
        @{ Name = "Proyecto"; Path = $script:TxtProject.Text },
        @{ Name = "Storage"; Path = $script:TxtStorage.Text }
    )

    foreach ($item in $items) {
        if (Test-Path -LiteralPath $item.Path) {
            Add-Log "$($item.Name): OK - $($item.Path)"
        }
        else {
            Add-ErrorLog "$($item.Name): no existe - $($item.Path)"
        }
    }

    $script:SqlCmdPath = Find-SqlCmd
    if ($script:SqlCmdPath) {
        Add-Log "sqlcmd detectado: $script:SqlCmdPath"
    }
    else {
        Add-ErrorLog "No se detecto sqlcmd en PATH ni en la entrega. Instale Microsoft sqlcmd, o restaure el .bak desde SSMS y luego use Validar base / Levantar app."
    }

    $dotnet = Find-Dotnet
    $dotnetSdk = Find-DotnetWithSdk
    if ($dotnet) {
        Add-Log "dotnet detectado: $dotnet"
    }
    else {
        Add-ErrorLog "No se encontro dotnet. Instale .NET Runtime o ASP.NET Core Hosting Bundle compatible."
    }

    if ($dotnetSdk) {
        Add-Log "SDK .NET disponible: $dotnetSdk"
    }
    else {
        if (Test-Path -LiteralPath $PublishedDll) {
            Add-Log "No se detecto SDK .NET. Para ejecutar desde codigo fuente se requiere SDK. Se puede usar la app publicada en 09_APP_PUBLICADA."
        }
        else {
            Add-ErrorLog "No se detecto SDK .NET. Para ejecutar desde codigo fuente se requiere SDK. Si existe app publicada, puede ejecutarse desde 09_APP_PUBLICADA."
        }
    }

    $sqlServices = Get-Service -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -like "MSSQL*" -or $_.DisplayName -like "*SQL Server*"
    }
    if ($sqlServices) {
        Add-Log "Servicios SQL Server locales detectados: $($sqlServices.Count)."
    }
    else {
        Add-Log "No se detecto SQL Server local. Instale SQL Server 2022 o indique una instancia remota."
    }

    $port = 0
    if ([int]::TryParse($script:TxtPort.Text, [ref]$port)) {
        $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        if ($listener) {
            $suggestions = Get-AvailableSuggestedPorts
            if ($suggestions.Count -gt 0) {
                Add-ErrorLog "El puerto $port ya esta ocupado. Use otro puerto, por ejemplo $($suggestions[0])."
                $script:TxtPort.Text = [string]$suggestions[0]
                Add-Log "Puerto sugerido aplicado en la interfaz: $($suggestions[0]). Puede cambiarlo si lo necesita."
            }
            else {
                Add-ErrorLog "El puerto $port ya esta ocupado. Revise puertos disponibles antes de levantar la app."
            }
        }
        else {
            Add-Log "Puerto $port disponible."
        }
    }
    else {
        Add-ErrorLog "Puerto web invalido."
    }

    Add-Log "Requisitos revisados. Use Ayuda / Diagnostico para detalle de uso."
}

function Test-SqlConnection {
    Add-Log "Probando conexion SQL Server..."
    $script:ConnectionOk = $false
    $useSql = (Get-SelectedAuth) -eq "Sql"
    $sqlPassword = Get-SqlPasswordPlain
    $query = "SET NOCOUNT ON; SELECT @@VERSION AS VersionSql;"
    try {
        $output = Invoke-SqlText -Server $script:TxtServer.Text -Database "master" -Query $query -SqlUser $script:TxtSqlUser.Text -SqlPassword $sqlPassword -UseSqlAuth:$useSql
        $script:ConnectionOk = $true
        Add-OkLog "Conexion SQL Server correcta. La base todavia no fue modificada."
        (($output | Select-Object -First 3) -join " ") | ForEach-Object {
            if (-not [string]::IsNullOrWhiteSpace($_)) {
                Add-Log $_
            }
        }
    }
    catch {
        $message = Get-FriendlySqlConnectionMessage -Stage "Conexion a SQL Server" -Server $script:TxtServer.Text.Trim() -OriginalMessage $_.Exception.Message
        Add-ErrorLog "No se pudo conectar con SQL Server. La base no fue modificada."
        Add-Log "Use Ayuda / Diagnostico o copie el diagnostico inferior para soporte."
        [System.Windows.Forms.MessageBox]::Show($message, "No se pudo conectar", "OK", "Error") | Out-Null
    }
    finally {
        $sqlPassword = $null
        if ($useSql) {
            Clear-SqlPasswordField
            Add-Log "Contrasena SQL limpiada del campo."
        }
    }
}

function Restore-Database {
    Add-Log "Iniciando restauracion de base..."
    if (-not $script:ConnectionOk) {
        $continue = [System.Windows.Forms.MessageBox]::Show(
            "Todavia no hay una conexion SQL Server confirmada.`r`n`r`nRecomendado: use primero '2 Probar conexion'.`r`n`r`nSi continua y falla la conexion, la base no sera modificada.",
            "Confirmar flujo",
            "OKCancel",
            "Warning")
        if ($continue -ne "OK") {
            Add-Log "Restauracion cancelada antes de conectar. La base no fue modificada."
            return
        }
    }

    $server = $script:TxtServer.Text.Trim()
    $database = $script:TxtDatabase.Text.Trim()
    $backup = $script:TxtBackup.Text.Trim()
    $useSql = (Get-SelectedAuth) -eq "Sql"
    $sqlUser = $script:TxtSqlUser.Text.Trim()
    $sqlPassword = Get-SqlPasswordPlain

    $restoreStarted = $false
    try {
    if (-not (Test-Path -LiteralPath $backup)) {
        throw "No existe el respaldo: $backup"
    }

    $exists = Get-DatabaseExists -Server $server -Database $database -SqlUser $sqlUser -SqlPassword $sqlPassword -UseSqlAuth:$useSql
    $replaceClause = ""
    $preSql = ""
    $postSql = ""
    if ($exists) {
        Add-Log "La base $database ya existe."
        if (-not (Confirm-OverwriteDatabase -Database $database)) {
            Add-Log "Restauracion cancelada por el usuario."
            return
        }
        $dbId = Escape-SqlIdentifier $database
        $preSql = "ALTER DATABASE [$dbId] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;"
        $postSql = "ALTER DATABASE [$dbId] SET MULTI_USER;"
        $replaceClause = ", REPLACE"
    }

    $backupLiteral = Escape-SqlLiteral $backup
    $fileListQuery = "RESTORE FILELISTONLY FROM DISK = N'$backupLiteral';"
    $fileList = Invoke-SqlText -Server $server -Database "master" -Query $fileListQuery -SqlUser $sqlUser -SqlPassword $sqlPassword -UseSqlAuth:$useSql -Separator "|" -NoHeader
    Add-Log "RESTORE FILELISTONLY ejecutado."

    $logicalData = $null
    $logicalLog = $null
    foreach ($line in $fileList) {
        $text = ([string]$line).Trim()
        if ([string]::IsNullOrWhiteSpace($text) -or $text -match "^-+$" -or $text -match "^\(\d+ rows? affected\)$") {
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

        if ($fileType -eq "D" -and -not $logicalData) {
            $logicalData = $logicalName
        }
        elseif ($fileType -eq "L" -and -not $logicalLog) {
            $logicalLog = $logicalName
        }
    }

    if (-not $logicalData -or -not $logicalLog) {
        throw "No se pudieron detectar los nombres logicos del .bak. Revise que el respaldo sea valido o restaure manualmente con SSMS."
    }

    $pathsQuery = @"
SET NOCOUNT ON;
SELECT
    COALESCE(CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultDataPath')),
        LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name)) + 1))
FROM sys.master_files WHERE database_id = 1 AND file_id = 1;
SELECT
    COALESCE(CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultLogPath')),
        LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name)) + 1))
FROM sys.master_files WHERE database_id = 1 AND file_id = 2;
"@
    $pathOutput = Invoke-SqlText -Server $server -Database "master" -Query $pathsQuery -SqlUser $sqlUser -SqlPassword $sqlPassword -UseSqlAuth:$useSql
    $pathLines = $pathOutput | Where-Object {
        $_ -and
        $_ -notmatch "^-+$" -and
        $_ -notmatch "^\(\d+ rows? affected\)" -and
        $_ -notmatch "COALESCE" -and
        $_.Trim() -match "[\\/]"
    } | ForEach-Object { $_.Trim() }

    $dataPath = $pathLines | Select-Object -First 1
    $logPath = $pathLines | Select-Object -Skip 1 -First 1
    if (-not $dataPath) { throw "No se pudo detectar ruta de datos SQL Server." }
    if (-not $logPath) { $logPath = $dataPath }

    $safeDbFile = ($database -replace "[^\w\-]", "_")
    $mdf = Join-Path $dataPath "$safeDbFile.mdf"
    $ldf = Join-Path $logPath "${safeDbFile}_log.ldf"

    $dbIdentifier = Escape-SqlIdentifier $database
    $restoreQuery = @"
$preSql
RESTORE DATABASE [$dbIdentifier]
FROM DISK = N'$backupLiteral'
WITH
    MOVE N'$(Escape-SqlLiteral $logicalData)' TO N'$(Escape-SqlLiteral $mdf)',
    MOVE N'$(Escape-SqlLiteral $logicalLog)' TO N'$(Escape-SqlLiteral $ldf)',
    STATS = 10$replaceClause;
$postSql
"@

    $restoreStarted = $true
    $restoreOutput = Invoke-SqlText -Server $server -Database "master" -Query $restoreQuery -SqlUser $sqlUser -SqlPassword $sqlPassword -UseSqlAuth:$useSql -Timeout 600
    $restoreOutput | ForEach-Object { Add-Log $_ }
    $script:LastRestoreResult = "Exitosa"
    Add-OkLog "Restauracion completada."
    $script:DatabaseValidated = $false
    }
    catch {
        if ($restoreStarted) {
            Add-ErrorLog "Error al restaurar base. La operacion pudo haber iniciado; revise SQL Server y el reporte antes de intentar de nuevo. Detalle: $($_.Exception.Message)"
        }
        else {
            $message = Get-FriendlySqlConnectionMessage -Stage "Preparacion de restauracion" -Server $server -OriginalMessage $_.Exception.Message
            Add-ErrorLog "Error antes de restaurar. La base no fue modificada."
            [System.Windows.Forms.MessageBox]::Show($message, "Error antes de restaurar", "OK", "Error") | Out-Null
        }
    }
    finally {
        $sqlPassword = $null
        if ($useSql) {
            Clear-SqlPasswordField
            Add-Log "Contrasena SQL limpiada del campo."
        }
    }
}

function Validate-Database {
    Add-Log "Validando conteos de base..."
    $db = $script:TxtDatabase.Text.Trim()
    $useSql = (Get-SelectedAuth) -eq "Sql"
    $sqlPassword = Get-SqlPasswordPlain
    $query = @"
SET NOCOUNT ON;
SELECT 'usuarios_admin' AS tabla, COUNT(*) AS conteo FROM usuarios_admin
UNION ALL SELECT 'areas_publicacion', COUNT(*) FROM areas_publicacion
UNION ALL SELECT 'configuracion_sitio', COUNT(*) FROM configuracion_sitio
UNION ALL SELECT 'accesos_rapidos', COUNT(*) FROM accesos_rapidos
UNION ALL SELECT 'sitio_enlaces', COUNT(*) FROM sitio_enlaces
UNION ALL SELECT 'archivos_seccion', COUNT(*) FROM archivos_seccion
UNION ALL SELECT 'avisos', COUNT(*) FROM avisos
UNION ALL SELECT 'tutoriales', COUNT(*) FROM tutoriales
UNION ALL SELECT 'directorio_areas', COUNT(*) FROM directorio_areas
UNION ALL SELECT 'directorio', COUNT(*) FROM directorio;
"@
    try {
        $output = Invoke-SqlText -Server $script:TxtServer.Text -Database $db -Query $query -SqlUser $script:TxtSqlUser.Text -SqlPassword $sqlPassword -UseSqlAuth:$useSql
        $script:LastCounts = $output
        $script:DatabaseValidated = $true
        $output | ForEach-Object {
            if (-not [string]::IsNullOrWhiteSpace($_)) {
                Add-Log $_
            }
        }
        Add-OkLog "Validacion de base completada."
    }
    catch {
        $script:DatabaseValidated = $false
        Add-ErrorLog "Error al validar tablas. Revise que la base exista y que el respaldo sea correcto. Detalle: $($_.Exception.Message)"
    }
    finally {
        $sqlPassword = $null
        if ($useSql) {
            Clear-SqlPasswordField
            Add-Log "Contrasena SQL limpiada del campo."
        }
    }
}

function Build-ConnectionString {
    $server = $script:TxtServer.Text.Trim()
    $database = $script:TxtDatabase.Text.Trim()
    if ((Get-SelectedAuth) -eq "Sql") {
        return "Server=$server;Database=$database;User Id=$($script:TxtSqlUser.Text.Trim());Password=$(Get-SqlPasswordPlain);TrustServerCertificate=True;Encrypt=True"
    }

    return "Server=$server;Database=$database;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True"
}

function Start-App {
    Add-Log "Levantando app..."
    if (-not $script:DatabaseValidated) {
        Add-Log "Advertencia: aun no se valido la base desde este asistente. Si ya la restauro manualmente, puede continuar."
    }
    if ($script:AppProcess -and -not $script:AppProcess.HasExited) {
        Add-Log "La app ya esta en ejecucion. PID: $($script:AppProcess.Id)"
        return
    }

    $dotnet = Find-DotnetWithSdk
    $usePublished = $false
    if (-not $dotnet) {
        $dotnet = Find-Dotnet
        if ((Test-Path -LiteralPath $PublishedDll) -and $dotnet) {
            $usePublished = $true
            Add-Log "No se detecto SDK .NET. Se usara app publicada desde 09_APP_PUBLICADA."
        }
        else {
            throw "No se detecto SDK .NET 10 para ejecutar desde fuente, ni app publicada disponible en 09_APP_PUBLICADA. Para app publicada se requiere runtime .NET 10 o Hosting Bundle compatible."
        }
    }

    if (-not $usePublished -and -not (Test-Path -LiteralPath $script:TxtProject.Text)) {
        throw "No existe la ruta de proyecto."
    }

    $port = [int]$script:TxtPort.Text
    $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($listener) {
        $suggestions = Get-AvailableSuggestedPorts
        if ($suggestions.Count -gt 0) {
            $script:TxtPort.Text = [string]$suggestions[0]
            throw "El puerto $port ya esta ocupado. Use otro puerto, por ejemplo $($suggestions[0]). Ya actualice el campo Puerto web con esa sugerencia."
        }
        throw "El puerto $port ya esta ocupado."
    }

    $outLog = Join-Path $ReportsPath "app_gui_sqlserver.out.log"
    $errLog = Join-Path $ReportsPath "app_gui_sqlserver.err.log"
    $oldProvider = $env:DatabaseProvider
    $oldConn = $env:ConnectionStrings__SqlServer
    $oldUrls = $env:ASPNETCORE_URLS
    try {
        $env:DatabaseProvider = "SqlServer"
        $env:ConnectionStrings__SqlServer = Build-ConnectionString
        $env:ASPNETCORE_URLS = "http://127.0.0.1:$port"
        if ($usePublished) {
            $script:AppProcess = Start-Process -FilePath $dotnet -ArgumentList @("Intranet.dll") -WorkingDirectory $PublishedPath -PassThru -RedirectStandardOutput $outLog -RedirectStandardError $errLog
        }
        else {
            $script:AppProcess = Start-Process -FilePath $dotnet -ArgumentList @("run", "--no-launch-profile") -WorkingDirectory $script:TxtProject.Text -PassThru -RedirectStandardOutput $outLog -RedirectStandardError $errLog
        }
    }
    finally {
        $env:DatabaseProvider = $oldProvider
        $env:ConnectionStrings__SqlServer = $oldConn
        $env:ASPNETCORE_URLS = $oldUrls
    }

    $script:AppStartedOk = $true
    Add-OkLog "App iniciada. PID: $($script:AppProcess.Id). URL: http://127.0.0.1:$port"
}

function Validate-Site {
    Add-Log "Validando sitio..."
    if (-not $script:AppStartedOk) {
        Add-Log "Advertencia: la app no fue levantada desde este asistente. Se intentara validar la URL capturada."
    }
    $port = [int]$script:TxtPort.Text
    $base = "http://127.0.0.1:$port"
    $routes = @(
        "/health/live",
        "/health/ready",
        "/",
        "/Admin/Login",
        "/Avisos",
        "/Buscar?q=manual",
        "/manuales",
        "/capacitacion",
        "/Directorio"
    )

    $results = @()
    foreach ($route in $routes) {
        try {
            $response = Invoke-WebRequest -Uri ($base + $route) -UseBasicParsing -TimeoutSec 20
            $results += "$route = $($response.StatusCode)"
            Add-Log "$route = $($response.StatusCode)"
        }
        catch {
            $status = "ERROR"
            if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
                $status = [int]$_.Exception.Response.StatusCode
            }
            $results += "$route = $status"
            Add-ErrorLog "$route = $status"
        }
    }
    $script:LastSiteResults = $results
    if ($results -notmatch "ERROR") {
        $script:SiteValidatedOk = $true
        Add-OkLog "Validacion de sitio completada."
    }
}

function Open-Site {
    if (-not $script:SiteValidatedOk) {
        Add-Log "Advertencia: el sitio aun no fue validado desde este asistente."
    }
    $port = [int]$script:TxtPort.Text
    Start-Process "http://127.0.0.1:$port"
    Add-Log "Sitio abierto en navegador."
}

function Show-HelpDiagnosis {
    Add-Log "Ayuda rapida abierta."
    Add-Log "Use `"Copiar diagnostico`" para enviar el resumen tecnico si ocurre un error."

    $helpText = @(
        "AYUDA RAPIDA",
        "",
        "1. Instancia SQL Server",
        "Es el servidor o instancia donde se restaurara la base.",
        "",
        "Ejemplos:",
        "- .\SQLEXPRESS",
        "- localhost",
        "- .",
        "- SERVIDOR\INSTANCIA",
        "",
        "2. Autenticacion",
        "Windows Authentication usa la cuenta actual de Windows.",
        "SQL Authentication usa usuario y contrasena SQL.",
        "",
        "La contrasena no se muestra, no se guarda y no aparece en reportes.",
        "",
        "3. Orden recomendado",
        "1. Validar requisitos",
        "2. Probar conexion",
        "3. Restaurar base",
        "4. Validar base",
        "5. Levantar app",
        "6. Validar sitio",
        "7. Abrir sitio",
        "8. Generar reporte",
        "",
        "4. Si falla la conexion",
        "Verifique:",
        "- SQL Server 2022 instalado.",
        "- Servicio SQL Server iniciado.",
        "- Nombre de instancia correcto.",
        "- Permisos de la cuenta usada.",
        "- Autenticacion correcta.",
        "",
        "5. Soporte",
        "Use `"Copiar diagnostico`" y envie el resumen completo, sin contrasenas."
    ) -join [Environment]::NewLine

    $dialog = New-Object System.Windows.Forms.Form
    $dialog.Text = "Ayuda rapida"
    $dialog.StartPosition = "CenterParent"
    $dialog.Size = New-Object System.Drawing.Size(700, 560)
    $dialog.MinimumSize = New-Object System.Drawing.Size(620, 480)

    $box = New-Object System.Windows.Forms.RichTextBox
    $box.ReadOnly = $true
    $box.ScrollBars = "Vertical"
    $box.WordWrap = $true
    $box.BorderStyle = "FixedSingle"
    $box.Font = New-Object System.Drawing.Font("Segoe UI", 10)
    $box.Text = $helpText
    $box.Location = New-Object System.Drawing.Point(16, 16)
    $box.Size = New-Object System.Drawing.Size(650, 450)
    $box.Anchor = "Top,Bottom,Left,Right"
    $dialog.Controls.Add($box)

    $close = New-Object System.Windows.Forms.Button
    $close.Text = "Cerrar"
    $close.Size = New-Object System.Drawing.Size(90, 30)
    $close.Location = New-Object System.Drawing.Point(576, 478)
    $close.Anchor = "Bottom,Right"
    $close.Add_Click({ $dialog.Close() })
    $dialog.Controls.Add($close)
    $dialog.AcceptButton = $close
    $dialog.CancelButton = $close

    $dialog.ShowDialog($script:Form) | Out-Null
}

function Detect-SqlInstances {
    Add-Log "Detectando instancias locales..."
    $current = $script:TxtServer.Text.Trim()
    $detected = Get-DetectedSqlInstances -IncludeSqlCmd
    $added = 0

    foreach ($instance in $detected) {
        if (Add-ComboItemUnique -ComboBox $script:TxtServer -Value $instance) {
            $added++
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($current)) {
        $script:TxtServer.Text = $current
    }
    elseif ($script:TxtServer.Items.Count -gt 0) {
        $script:TxtServer.SelectedIndex = 0
    }

    if ($detected.Count -eq 0) {
        Add-Log "No se detectaron instancias locales automaticamente."
        [System.Windows.Forms.MessageBox]::Show(
            "No se detectaron instancias locales automaticamente. Puede escribir manualmente la instancia, por ejemplo .\SQLEXPRESS, localhost, . o SERVIDOR\INSTANCIA.",
            "Detectar instancias",
            "OK",
            "Information") | Out-Null
        return
    }

    Add-OkLog "Instancias detectadas agregadas al menu: $added nuevas."
}

function Generate-Report {
    Add-Log "Generando reporte..."
    $auth = $script:CmbAuth.SelectedItem
    $result = if ($script:LastErrors.Count -gt 0) { "Error" } else { "Correcto" }
    $site = "http://127.0.0.1:$($script:TxtPort.Text)"
    $content = @"
REPORTE ASISTENTE SQL SERVER - INTRANET FGET

Fecha y hora: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz")

============================================================
RESUMEN FINAL
============================================================

Resultado             : $result
Instancia SQL Server  : $($script:TxtServer.Text)
Base de datos         : $($script:TxtDatabase.Text)
Autenticacion         : $auth, sin contrasena
Respaldo usado        : $($script:TxtBackup.Text)
Sitio                 : $site
Reporte generado en   : $GuiReportPath

Si necesita seguimiento, copie el resumen y el diagnostico completo.

DETALLE
-------

Instancia: $($script:TxtServer.Text)
Base: $($script:TxtDatabase.Text)
Tipo de autenticacion: $auth, sin contrasena
Respaldo usado: $($script:TxtBackup.Text)
Proyecto: $($script:TxtProject.Text)
Storage: $($script:TxtStorage.Text)
Puerto web: $($script:TxtPort.Text)

Resultado de restauracion:
$script:LastRestoreResult

Conteos:
$($script:LastCounts -join [Environment]::NewLine)

Rutas validadas:
$($script:LastSiteResults -join [Environment]::NewLine)

Errores:
$(
    if ($script:LastErrors.Count -gt 0) {
        $script:LastErrors -join [Environment]::NewLine
    }
    else {
        "No registrados."
    }
)

Conclusion:
Revise los resultados anteriores. Si no hay errores y las rutas responden 200, la entrega quedo restaurada y levantada correctamente.
"@
    Set-Content -LiteralPath $GuiReportPath -Value $content -Encoding UTF8
    Add-Log "Reporte generado: $GuiReportPath"
}

function Run-SelfTest {
    $port5077 = Get-NetTCPConnection -LocalPort 5077 -State Listen -ErrorAction SilentlyContinue
    $suggestedPorts = Get-AvailableSuggestedPorts
    $detectedInstances = Get-DetectedSqlInstances
    $result = [ordered]@{
        ScriptDir = $ScriptDir
        EntregaRoot = $EntregaRoot
        ProjectPathExists = Test-Path -LiteralPath $ProjectPath
        PublishedAppExists = Test-Path -LiteralPath $PublishedDll
        BackupExists = Test-Path -LiteralPath $DefaultBackup
        StorageExists = Test-Path -LiteralPath $StoragePath
        ReportsPathExists = Test-Path -LiteralPath $ReportsPath
        SqlCmd = Find-SqlCmd
        Dotnet = Find-Dotnet
        DotnetSdk = Find-DotnetWithSdk
        Port5077Occupied = [bool]$port5077
        SuggestedPorts = ($suggestedPorts -join ", ")
        DetectedSqlInstances = ($detectedInstances -join ", ")
    }
    $result.GetEnumerator() | ForEach-Object { "{0}: {1}" -f $_.Key, $_.Value }
}

function New-Label {
    param([string]$Text, [int]$X, [int]$Y, [int]$W = 170)
    $label = New-Object System.Windows.Forms.Label
    $label.Text = $Text
    $label.Location = New-Object System.Drawing.Point($X, $Y)
    $label.Size = New-Object System.Drawing.Size($W, 23)
    return $label
}

function New-TextBox {
    param([string]$Text, [int]$X, [int]$Y, [int]$W = 470)
    $box = New-Object System.Windows.Forms.TextBox
    $box.Text = $Text
    $box.Location = New-Object System.Drawing.Point($X, $Y)
    $box.Size = New-Object System.Drawing.Size($W, 23)
    return $box
}

function Test-PortInUse {
    param([string]$PortText)

    $port = 0
    if (-not [int]::TryParse($PortText, [ref]$port) -or $port -lt 1 -or $port -gt 65535) {
        return $false
    }

    $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    return [bool]$listener
}

function Update-PortStatus {
    if (-not $script:LblPortStatus -or -not $script:TxtPort) {
        return
    }

    $portText = $script:TxtPort.Text.Trim()
    $port = 0
    if (-not [int]::TryParse($portText, [ref]$port) -or $port -lt 1 -or $port -gt 65535) {
        $script:LblPortStatus.Text = "Puerto invalido."
        $script:LblPortStatus.ForeColor = [System.Drawing.Color]::DarkRed
        return
    }

    if (Test-PortInUse $portText) {
        $suggested = Get-AvailableSuggestedPorts
        if ($suggested.Count -gt 0) {
            $script:LblPortStatus.Text = "Ocupado. Sugeridos: $($suggested -join ', ')."
        }
        else {
            $script:LblPortStatus.Text = "Ocupado. Capture otro puerto."
        }
        $script:LblPortStatus.ForeColor = [System.Drawing.Color]::DarkRed
    }
    else {
        $script:LblPortStatus.Text = "Disponible."
        $script:LblPortStatus.ForeColor = [System.Drawing.Color]::DarkGreen
    }
}

function Apply-InitialPortSuggestion {
    if (-not $script:TxtPort) {
        return
    }

    if ($script:TxtPort.Text.Trim() -eq "5077" -and (Test-PortInUse "5077")) {
        $suggested = Get-AvailableSuggestedPorts
        if ($suggested.Count -gt 0) {
            $script:TxtPort.Text = [string]$suggested[0]
            Add-Log "Puerto 5077 ocupado. Se sugirio automaticamente el puerto $($suggested[0])."
        }
        else {
            Add-Log "Puerto 5077 ocupado. Capture otro puerto disponible antes de levantar la app."
        }
    }

    Update-PortStatus
}

if ($SelfTest) {
    Run-SelfTest
    return
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[System.Windows.Forms.Application]::EnableVisualStyles()

$script:Form = New-Object System.Windows.Forms.Form
$script:Form.Text = "Restaurador Intranet FGET - SQL Server 2022"
$script:Form.StartPosition = "CenterScreen"
$script:Form.Size = New-Object System.Drawing.Size(1180, 760)
$script:Form.MinimumSize = New-Object System.Drawing.Size(1080, 700)

$script:Tips = New-Object System.Windows.Forms.ToolTip
$script:Tips.AutoPopDelay = 12000
$script:Tips.InitialDelay = 400
$script:Tips.ReshowDelay = 150

$title = New-Object System.Windows.Forms.Label
$title.Text = "Restaurador Intranet FGET - SQL Server 2022"
$title.Font = New-Object System.Drawing.Font("Segoe UI", 14, [System.Drawing.FontStyle]::Bold)
$title.Location = New-Object System.Drawing.Point(18, 14)
$title.Size = New-Object System.Drawing.Size(760, 30)
$script:Form.Controls.Add($title)

$subtitle = New-Object System.Windows.Forms.Label
$subtitle.Text = "Flujo recomendado: 1 requisitos, 2 conexion, 3 restaurar, 4 validar base, 5 levantar app, 6 validar sitio."
$subtitle.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$subtitle.ForeColor = [System.Drawing.Color]::DimGray
$subtitle.Location = New-Object System.Drawing.Point(20, 46)
$subtitle.Size = New-Object System.Drawing.Size(980, 22)
$script:Form.Controls.Add($subtitle)

$y = 78
$script:Form.Controls.Add((New-Label "Instancia SQL Server" 20 $y))
$script:TxtServer = New-Object System.Windows.Forms.ComboBox
$script:TxtServer.DropDownStyle = "DropDown"
$script:TxtServer.Location = New-Object System.Drawing.Point(200, $y)
$script:TxtServer.Size = New-Object System.Drawing.Size(260, 23)
foreach ($option in Get-DefaultSqlInstanceOptions) {
    Add-ComboItemUnique -ComboBox $script:TxtServer -Value $option | Out-Null
}
$script:TxtServer.Text = ".\SQLEXPRESS"
$script:Form.Controls.Add($script:TxtServer)
$script:Tips.SetToolTip($script:TxtServer, "Si la instancia no es .\SQLEXPRESS, capture el nombre correcto.")

$instanceHelp = New-Object System.Windows.Forms.Label
$instanceHelp.Text = "Nombre de la instancia SQL Server. Ejemplos: .\SQLEXPRESS, localhost, ., SERVIDOR\INSTANCIA"
$instanceHelp.ForeColor = [System.Drawing.Color]::DimGray
$instanceHelp.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$instanceHelp.UseCompatibleTextRendering = $true
$instanceHelp.Location = New-Object System.Drawing.Point(200, ($y + 24))
$instanceHelp.Size = New-Object System.Drawing.Size(940, 22)
$script:Form.Controls.Add($instanceHelp)

$script:Form.Controls.Add((New-Label "Base de datos" 500 $y 120))
$script:TxtDatabase = New-TextBox "intranet_fget" 620 $y 260
$script:Form.Controls.Add($script:TxtDatabase)
$script:Tips.SetToolTip($script:TxtDatabase, "Base destino. Si ya existe, el asistente pedira confirmacion antes de restaurar encima.")

$y += 48
$script:Form.Controls.Add((New-Label "Autenticacion" 20 $y))
$script:CmbAuth = New-Object System.Windows.Forms.ComboBox
$script:CmbAuth.DropDownStyle = "DropDownList"
$script:CmbAuth.Items.Add("Windows Authentication") | Out-Null
$script:CmbAuth.Items.Add("SQL Authentication") | Out-Null
$script:CmbAuth.SelectedIndex = 0
$script:CmbAuth.Location = New-Object System.Drawing.Point(200, $y)
$script:CmbAuth.Size = New-Object System.Drawing.Size(260, 23)
$script:Form.Controls.Add($script:CmbAuth)
$script:Tips.SetToolTip($script:CmbAuth, "Use Windows Authentication si su usuario tiene permisos en SQL Server.")

$authHelp = New-Object System.Windows.Forms.Label
$authHelp.Text = "Windows Authentication: se usara la cuenta actual. SQL Authentication: la contrasena no se mostrara, no se guardara y no aparecera en reportes."
$authHelp.ForeColor = [System.Drawing.Color]::DimGray
$authHelp.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$authHelp.UseCompatibleTextRendering = $true
$authHelp.Location = New-Object System.Drawing.Point(200, ($y + 24))
$authHelp.Size = New-Object System.Drawing.Size(940, 22)
$script:Form.Controls.Add($authHelp)

$script:Form.Controls.Add((New-Label "Puerto web" 500 $y 120))
$script:TxtPort = New-TextBox "5077" 620 $y 100
$script:Form.Controls.Add($script:TxtPort)
$script:LblPortStatus = New-Object System.Windows.Forms.Label
$script:LblPortStatus.Text = ""
$script:LblPortStatus.Location = New-Object System.Drawing.Point(730, ($y + 2))
$script:LblPortStatus.Size = New-Object System.Drawing.Size(380, 22)
$script:LblPortStatus.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$script:Form.Controls.Add($script:LblPortStatus)
$script:TxtPort.Add_TextChanged({ Update-PortStatus })
$script:Tips.SetToolTip($script:TxtPort, "Si 5077 esta ocupado, use 5080, 5088 o 5090.")

$y += 48
$script:Form.Controls.Add((New-Label "Usuario SQL" 20 $y))
$script:TxtSqlUser = New-TextBox "" 200 $y 260
$script:TxtSqlUser.Enabled = $false
$script:Form.Controls.Add($script:TxtSqlUser)
$script:Tips.SetToolTip($script:TxtSqlUser, "Solo se usa con SQL Authentication. No se guarda en reportes.")

$script:Form.Controls.Add((New-Label "Contrasena SQL" 500 $y 120))
$script:TxtSqlPassword = New-TextBox "" 620 $y 260
$script:TxtSqlPassword.UseSystemPasswordChar = $true
$script:TxtSqlPassword.Enabled = $false
$script:Form.Controls.Add($script:TxtSqlPassword)
$script:Tips.SetToolTip($script:TxtSqlPassword, "No se muestra, no se guarda y no aparece en reportes.")

$script:CmbAuth.Add_SelectedIndexChanged({
    $enabled = ($script:CmbAuth.SelectedItem -eq "SQL Authentication")
    $script:TxtSqlUser.Enabled = $enabled
    $script:TxtSqlPassword.Enabled = $enabled
})

$y += 40
$script:Form.Controls.Add((New-Label "Archivo de respaldo seleccionado" 20 $y 190))
$script:TxtBackup = New-TextBox $DefaultBackup 200 $y 810
$script:Form.Controls.Add($script:TxtBackup)
$script:Tips.SetToolTip($script:TxtBackup, "Debe apuntar al .bak incluido en 02_BASE_DE_DATOS_SQLSERVER.")
$btnBrowseBackup = New-Object System.Windows.Forms.Button
$btnBrowseBackup.Text = "Buscar"
$btnBrowseBackup.Location = New-Object System.Drawing.Point(1030, ($y - 1))
$btnBrowseBackup.Size = New-Object System.Drawing.Size(70, 26)
$btnBrowseBackup.Add_Click({
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Filter = "Backups SQL Server (*.bak)|*.bak|Scripts SQL (*.sql)|*.sql|Todos (*.*)|*.*"
    $dialog.InitialDirectory = $BackupDir
    if ($dialog.ShowDialog($script:Form) -eq "OK") {
        $script:TxtBackup.Text = $dialog.FileName
    }
})
$script:Form.Controls.Add($btnBrowseBackup)

$y += 34
$script:Form.Controls.Add((New-Label "Ruta del proyecto" 20 $y))
$script:TxtProject = New-TextBox $ProjectPath 200 $y 900
$script:Form.Controls.Add($script:TxtProject)
$script:Tips.SetToolTip($script:TxtProject, "Proyecto copiado dentro de la entrega.")

$y += 34
$script:Form.Controls.Add((New-Label "Ruta de Storage" 20 $y))
$script:TxtStorage = New-TextBox $StoragePath 200 $y 900
$script:Form.Controls.Add($script:TxtStorage)
$script:Tips.SetToolTip($script:TxtStorage, "Ruta detectada para codigo fuente: 01_PROYECTO\\Storage. Desde app publicada se usa 09_APP_PUBLICADA\\Storage. No borre Storage.")

$flowLabel = New-Object System.Windows.Forms.Label
$flowLabel.Text = "Use estos botones en orden. Con Windows Authentication no necesita capturar contrasena SQL."
$flowLabel.ForeColor = [System.Drawing.Color]::DimGray
$flowLabel.Location = New-Object System.Drawing.Point(20, ($y + 34))
$flowLabel.Size = New-Object System.Drawing.Size(1020, 22)
$script:Form.Controls.Add($flowLabel)

$buttonY = $y + 60
$buttons = @(
    @{ Text = "1 Validar requisitos"; X = 20; Stage = "Error al validar requisitos. La base no fue modificada."; Action = { Test-Requirements } },
    @{ Text = "2 Probar conexion"; X = 195; Stage = "Error al conectar con SQL Server. La base no fue modificada."; Action = { Test-SqlConnection } },
    @{ Text = "3 Restaurar base"; X = 370; Stage = "Error al restaurar base. Revise el diagnostico antes de intentar de nuevo."; Action = { Restore-Database } },
    @{ Text = "4 Validar base"; X = 545; Stage = "Error al validar tablas. La base no fue modificada por esta validacion."; Action = { Validate-Database } },
    @{ Text = "5 Levantar app"; X = 720; Stage = "Error al levantar aplicacion. La base no fue modificada."; Action = { Start-App } },
    @{ Text = "6 Validar sitio"; X = 895; Stage = "Error al validar sitio. La base no fue modificada."; Action = { Validate-Site } }
)

foreach ($btnData in $buttons) {
    $btn = New-Object System.Windows.Forms.Button
    $btn.Text = $btnData.Text
    $btn.Location = New-Object System.Drawing.Point($btnData.X, $buttonY)
    $btn.Size = New-Object System.Drawing.Size(160, 36)
    $action = $btnData.Action
    $stageMessage = $btnData.Stage
    $btn.Add_Click({
        try {
            & $action
        }
        catch {
            $fullMessage = "$stageMessage`r`n`r`nDetalle tecnico:`r`n$($_.Exception.Message)"
            Add-ErrorLog $fullMessage
            [System.Windows.Forms.MessageBox]::Show($fullMessage, "Error", "OK", "Error") | Out-Null
        }
    }.GetNewClosure())
    $script:Form.Controls.Add($btn)
}

$buttonY += 42
$btnOpen = New-Object System.Windows.Forms.Button
$btnOpen.Text = "7 Abrir sitio"
$btnOpen.Location = New-Object System.Drawing.Point(20, $buttonY)
$btnOpen.Size = New-Object System.Drawing.Size(160, 36)
$btnOpen.Add_Click({
    try { Open-Site } catch { Add-ErrorLog $_.Exception.Message }
})
$script:Form.Controls.Add($btnOpen)

$btnReport = New-Object System.Windows.Forms.Button
$btnReport.Text = "8 Generar reporte"
$btnReport.Location = New-Object System.Drawing.Point(195, $buttonY)
$btnReport.Size = New-Object System.Drawing.Size(160, 36)
$btnReport.Add_Click({
    try {
        Generate-Report
        [System.Windows.Forms.MessageBox]::Show("Reporte generado.", "Asistente", "OK", "Information") | Out-Null
    }
    catch {
        Add-ErrorLog $_.Exception.Message
    }
})
$script:Form.Controls.Add($btnReport)

$btnHelp = New-Object System.Windows.Forms.Button
$btnHelp.Text = "Ayuda / Diagnostico"
$btnHelp.Location = New-Object System.Drawing.Point(370, $buttonY)
$btnHelp.Size = New-Object System.Drawing.Size(160, 36)
$btnHelp.Add_Click({
    try { Show-HelpDiagnosis } catch { Add-ErrorLog $_.Exception.Message }
})
$script:Form.Controls.Add($btnHelp)

$btnDetectInstances = New-Object System.Windows.Forms.Button
$btnDetectInstances.Text = "Detectar instancias"
$btnDetectInstances.Location = New-Object System.Drawing.Point(720, $buttonY)
$btnDetectInstances.Size = New-Object System.Drawing.Size(160, 36)
$btnDetectInstances.Add_Click({
    try { Detect-SqlInstances } catch { Add-ErrorLog $_.Exception.Message }
})
$script:Form.Controls.Add($btnDetectInstances)

$btnReportsFolder = New-Object System.Windows.Forms.Button
$btnReportsFolder.Text = "Abrir reportes"
$btnReportsFolder.Location = New-Object System.Drawing.Point(895, $buttonY)
$btnReportsFolder.Size = New-Object System.Drawing.Size(160, 36)
$btnReportsFolder.Add_Click({
    try { Start-Process -FilePath $ReportsPath } catch { Add-ErrorLog $_.Exception.Message }
})
$script:Form.Controls.Add($btnReportsFolder)

$btnCopyDiag = New-Object System.Windows.Forms.Button
$btnCopyDiag.Text = "Copiar diagnostico"
$btnCopyDiag.Location = New-Object System.Drawing.Point(720, ($buttonY + 42))
$btnCopyDiag.Size = New-Object System.Drawing.Size(160, 30)
$btnCopyDiag.Add_Click({
    try {
        [System.Windows.Forms.Clipboard]::SetText($script:LogBox.Text)
        Add-Log "Diagnostico copiado al portapapeles."
    }
    catch {
        Add-ErrorLog $_.Exception.Message
    }
})
$script:Form.Controls.Add($btnCopyDiag)

$supportLabel = New-Object System.Windows.Forms.Label
$supportLabel.Text = "Si necesita seguimiento, copie el resumen y el diagnostico completo."
$supportLabel.ForeColor = [System.Drawing.Color]::DimGray
$supportLabel.Location = New-Object System.Drawing.Point(20, ($buttonY + 44))
$supportLabel.Size = New-Object System.Drawing.Size(660, 24)
$script:Form.Controls.Add($supportLabel)

$script:LogBox = New-Object System.Windows.Forms.TextBox
$script:LogBox.Multiline = $true
$script:LogBox.ScrollBars = "Both"
$script:LogBox.WordWrap = $false
$script:LogBox.ReadOnly = $true
$script:LogBox.Font = New-Object System.Drawing.Font("Consolas", 9)
$script:LogBox.Location = New-Object System.Drawing.Point(20, ($buttonY + 78))
$script:LogBox.Size = New-Object System.Drawing.Size(1100, 274)
$script:LogBox.Anchor = "Left,Right,Top,Bottom"
$script:Form.Controls.Add($script:LogBox)

if ($SmokeOpen) {
    $timer = New-Object System.Windows.Forms.Timer
    $timer.Interval = 1400
    $timer.Add_Tick({
        $timer.Stop()
        $script:Form.Close()
    })
    $timer.Start()
}

$script:Form.Add_Shown({
    Add-Log "Asistente abierto."
    Add-Log "Entrega detectada: $EntregaRoot"
    if (Test-Path -LiteralPath $DefaultBackup) {
        Add-OkLog "Respaldo detectado: $DefaultBackup"
    }
    else {
        Add-ErrorLog "No se encontro respaldo: $DefaultBackup"
    }
    if (Test-Path -LiteralPath $StoragePath) {
        Add-OkLog "Storage detectado: $StoragePath"
    }
    else {
        Add-Log "Storage no detectado: $StoragePath"
    }
    Apply-InitialPortSuggestion
})

[System.Windows.Forms.Application]::Run($script:Form)
