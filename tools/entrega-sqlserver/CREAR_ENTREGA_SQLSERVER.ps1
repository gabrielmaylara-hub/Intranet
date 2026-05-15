[CmdletBinding()]
param(
    [string]$DestinoBase = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) ".."),
    [string]$NombreEntrega,
    [string]$RutaRespaldoBaseDatos,
    [string]$DotnetPath,
    [switch]$NoPublish,
    [switch]$IncluirGit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    $root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    return (Resolve-Path -LiteralPath $root).Path
}

function New-CleanDirectory {
    param([string]$Path)
    if (Test-Path -LiteralPath $Path) {
        throw "La carpeta de entrega ya existe: $Path"
    }
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Invoke-RobocopyChecked {
    param(
        [string]$Source,
        [string]$Destination,
        [string[]]$ExtraArgs = @()
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        return 0
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    $args = @(
        $Source,
        $Destination,
        "/E",
        "/R:2",
        "/W:1",
        "/NFL",
        "/NDL",
        "/NP"
    ) + $ExtraArgs

    & robocopy @args | Out-Null
    $code = $LASTEXITCODE
    if ($code -ge 8) {
        throw "Robocopy fallo con codigo $code desde '$Source' hacia '$Destination'."
    }
    return $code
}

function Write-TextFile {
    param(
        [string]$Path,
        [string[]]$Lines
    )
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($Path, $Lines, $utf8NoBom)
}

function Test-DotnetSdk {
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($DotnetPath)) {
        $candidates.Add($DotnetPath) | Out-Null
    }

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet -and $dotnet.Source) {
        $candidates.Add($dotnet.Source) | Out-Null
    }

    foreach ($candidate in @(
        "$env:USERPROFILE\.dotnet\dotnet.exe",
        "C:\Recuperacion\tools\dotnet10\dotnet.exe",
        "C:\Program Files\dotnet\dotnet.exe",
        "C:\Program Files (x86)\dotnet\dotnet.exe"
    )) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            $candidates.Add($candidate) | Out-Null
        }
    }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
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

$repoRoot = Resolve-RepoRoot
$fecha = Get-Date -Format "dd-MM-yyyy"
if ([string]::IsNullOrWhiteSpace($NombreEntrega)) {
    $NombreEntrega = "ENTREGA INTRANET SQLSERVER 2022 $fecha"
}

$destinoBaseResuelto = if (Test-Path -LiteralPath $DestinoBase) {
    (Resolve-Path -LiteralPath $DestinoBase).Path
}
else {
    New-Item -ItemType Directory -Path $DestinoBase -Force | Out-Null
    (Resolve-Path -LiteralPath $DestinoBase).Path
}

$entregaRoot = Join-Path $destinoBaseResuelto $NombreEntrega
New-CleanDirectory -Path $entregaRoot

$carpetas = @(
    "01_PROYECTO",
    "02_BASE_DE_DATOS_SQLSERVER",
    "03_STORAGE",
    "04_DOCUMENTACION_ENTREGA",
    "05_REPORTES_VALIDACION",
    "06_RESTAURAR_SQLSERVER",
    "07_NOTAS_TECNICAS",
    "08_PRERREQUISITOS",
    "09_APP_PUBLICADA"
)

foreach ($carpeta in $carpetas) {
    New-Item -ItemType Directory -Path (Join-Path $entregaRoot $carpeta) -Force | Out-Null
}

$excludeDirs = @("bin", "obj", ".vs", ".idea", "logs", ".tmp", ".tmp-logs", "publish", ".backups", ".tools", "TestResults", "ENTREGA*")
if (-not $IncluirGit) {
    $excludeDirs += ".git"
}

$excludeFiles = @(
    "*.user",
    "*.suo",
    "*.trx",
    "*.log",
    "*.tmp",
    "*.bak",
    "*.dump",
    "*.dmp",
    "*.mdf",
    "*.ldf",
    "*.zip",
    "*.7z",
    "*.rar",
    ".env",
    ".env.local",
    ".env.*.local",
    ".env.intranet-local",
    "CREDENCIALES*"
)

$robocopyProjectArgs = @("/XD") + $excludeDirs + @("/XF") + $excludeFiles
Invoke-RobocopyChecked -Source $repoRoot -Destination (Join-Path $entregaRoot "01_PROYECTO") -ExtraArgs $robocopyProjectArgs | Out-Null

$storageSource = Join-Path $repoRoot "Storage"
if (Test-Path -LiteralPath $storageSource) {
    Invoke-RobocopyChecked -Source $storageSource -Destination (Join-Path $entregaRoot "03_STORAGE") -ExtraArgs @("/XD", "logs", "/XF", "*.tmp", "*.log") | Out-Null
}

if (-not [string]::IsNullOrWhiteSpace($RutaRespaldoBaseDatos)) {
    if (-not (Test-Path -LiteralPath $RutaRespaldoBaseDatos)) {
        throw "No existe el respaldo indicado: $RutaRespaldoBaseDatos"
    }
    Copy-Item -LiteralPath $RutaRespaldoBaseDatos -Destination (Join-Path $entregaRoot "02_BASE_DE_DATOS_SQLSERVER") -Force
}

Copy-Item -Path (Join-Path $PSScriptRoot "06_RESTAURAR_SQLSERVER\*") -Destination (Join-Path $entregaRoot "06_RESTAURAR_SQLSERVER") -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "templates\CREDENCIALES_BASE_SQLSERVER.template.txt") -Destination (Join-Path $entregaRoot "CREDENCIALES_BASE_SQLSERVER.template.txt") -Force

Write-TextFile -Path (Join-Path $entregaRoot "04_DOCUMENTACION_ENTREGA\README_ENTREGA_SQLSERVER.txt") -Lines @(
    "Entrega Intranet FGET SQL Server 2022",
    "",
    "Estructura:",
    "- 01_PROYECTO: fuente empaquetado sin bin, obj, .git, temporales, respaldos ni credenciales.",
    "- 02_BASE_DE_DATOS_SQLSERVER: respaldo .bak o script .sql proporcionado al empaquetar.",
    "- 03_STORAGE: contenido Storage copiado para restauracion funcional.",
    "- 06_RESTAURAR_SQLSERVER: scripts de restauracion con rutas relativas.",
    "- 09_APP_PUBLICADA: salida de dotnet publish cuando hay SDK compatible.",
    "",
    "No guardar contrasenas reales en esta entrega. Usar la plantilla CREDENCIALES_BASE_SQLSERVER.template.txt solo como guia."
)

Write-TextFile -Path (Join-Path $entregaRoot "07_NOTAS_TECNICAS\NOTAS_TECNICAS_SQLSERVER.txt") -Lines @(
    "Notas tecnicas",
    "",
    "- Proyecto fuente: $repoRoot",
    "- Fecha de empaquetado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "- Proveedor esperado para SQL Server: DatabaseProvider=SqlServer.",
    "- Las cadenas de conexion deben configurarse por entorno, no versionarse con secretos reales.",
    "- Los restauradores deben ejecutarse desde 06_RESTAURAR_SQLSERVER para conservar rutas relativas."
)

Write-TextFile -Path (Join-Path $entregaRoot "08_PRERREQUISITOS\README_PRERREQUISITOS.txt") -Lines @(
    "Prerrequisitos",
    "",
    "- Windows con PowerShell 5.1 o superior.",
    "- SQL Server 2022 disponible en la maquina destino.",
    "- sqlcmd instalado o disponible en 08_PRERREQUISITOS\sqlcmd.",
    "- .NET SDK compatible con el TargetFramework del proyecto para publicar desde fuente.",
    "",
    "Este paquete no descarga dependencias automaticamente. Instale prerrequisitos desde fuentes oficiales controladas por TI."
)

$dotnetPath = Test-DotnetSdk
$publishStatus = "Omitido"
if (-not $NoPublish -and $dotnetPath) {
    $publishDir = Join-Path $entregaRoot "09_APP_PUBLICADA"
    & $dotnetPath publish (Join-Path $repoRoot "Intranet.csproj") -c Release -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish fallo con codigo $LASTEXITCODE."
    }
    $publishStatus = "Generado con $dotnetPath"
}
elseif (-not $NoPublish -and -not $dotnetPath) {
    $publishStatus = "No generado: no se encontro dotnet SDK compatible en PATH."
}

$projectFiles = @(Get-ChildItem -LiteralPath (Join-Path $entregaRoot "01_PROYECTO") -Recurse -File -Force)
$storageFiles = @(Get-ChildItem -LiteralPath (Join-Path $entregaRoot "03_STORAGE") -Recurse -File -Force -ErrorAction SilentlyContinue)
$dbFiles = @(Get-ChildItem -LiteralPath (Join-Path $entregaRoot "02_BASE_DE_DATOS_SQLSERVER") -File -Force -ErrorAction SilentlyContinue)

Write-TextFile -Path (Join-Path $entregaRoot "05_REPORTES_VALIDACION\REPORTE_EMPAQUETADO.txt") -Lines @(
    "Reporte de empaquetado Intranet FGET SQL Server 2022",
    "Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "Proyecto fuente: $repoRoot",
    "Carpeta entrega: $entregaRoot",
    "",
    "Archivos en 01_PROYECTO: $($projectFiles.Count)",
    "Archivos en 02_BASE_DE_DATOS_SQLSERVER: $($dbFiles.Count)",
    "Archivos en 03_STORAGE: $($storageFiles.Count)",
    "Publicacion: $publishStatus",
    "",
    "Exclusiones aplicadas:",
    ($excludeDirs -join ", "),
    ($excludeFiles -join ", "),
    "",
    "Control de seguridad:",
    "- No se copiaron credenciales reales por patron.",
    "- No se imprimieron secretos.",
    "- Se genero plantilla de credenciales con placeholders."
)

Write-Host "Entrega generada en: $entregaRoot"
