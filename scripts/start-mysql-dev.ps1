$ErrorActionPreference = "Stop"

$raiz = Split-Path -Parent $PSScriptRoot
$mysqlHome = Join-Path $raiz ".tools\mysql\mysql-8.0.44-winx64"
$myIni = Join-Path $raiz ".tools\my.ini"
$mysqld = Join-Path $mysqlHome "bin\mysqld.exe"
$mysqladmin = Join-Path $mysqlHome "bin\mysqladmin.exe"
$mysqlUser = $env:MYSQL_DEV_USER
$mysqlPassword = $env:MYSQL_DEV_PASSWORD

if ([string]::IsNullOrWhiteSpace($mysqlUser) -or [string]::IsNullOrEmpty($mysqlPassword)) {
    throw "Configure MYSQL_DEV_USER y MYSQL_DEV_PASSWORD para administrar el MySQL portable local."
}

if (!(Test-Path $mysqld)) {
    throw "No se encontró MySQL portable. Ejecute primero la instalación local de desarrollo."
}

$conexion = Get-NetTCPConnection -LocalPort 3306 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($conexion) {
    Write-Host "MySQL ya está escuchando en 127.0.0.1:3306."
    exit 0
}

Start-Process -FilePath $mysqld -ArgumentList "--defaults-file=`"$myIni`"" -WindowStyle Hidden | Out-Null

for ($i = 0; $i -lt 60; $i++) {
    & $mysqladmin "--user=$mysqlUser" "--password=$mysqlPassword" --protocol=tcp --host=127.0.0.1 --port=3306 ping 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "MySQL iniciado en 127.0.0.1:3306."
        exit 0
    }

    Start-Sleep -Seconds 1
}

throw "MySQL no respondió en el tiempo esperado."
