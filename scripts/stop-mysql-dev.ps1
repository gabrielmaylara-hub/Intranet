$ErrorActionPreference = "Stop"

$raiz = Split-Path -Parent $PSScriptRoot
$mysqlHome = Join-Path $raiz ".tools\mysql\mysql-8.0.44-winx64"
$mysqladmin = Join-Path $mysqlHome "bin\mysqladmin.exe"
$mysqlUser = $env:MYSQL_DEV_USER
$mysqlPassword = $env:MYSQL_DEV_PASSWORD

if ((Test-Path $mysqladmin) -and
    -not [string]::IsNullOrWhiteSpace($mysqlUser) -and
    -not [string]::IsNullOrEmpty($mysqlPassword)) {
    & $mysqladmin "--user=$mysqlUser" "--password=$mysqlPassword" --protocol=tcp --host=127.0.0.1 --port=3306 shutdown 2>$null
}

Start-Sleep -Seconds 1

$procesos = Get-Process mysqld -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -like "$mysqlHome*"
}

foreach ($proceso in $procesos) {
    Stop-Process -Id $proceso.Id -Force
}

Write-Host "MySQL de desarrollo detenido."
