@echo off
setlocal
chcp 65001 >nul

cd /d "%~dp0"

if not exist "%~dp0RESTAURAR_SQLSERVER_FGET.ps1" (
    echo No se encontro el script RESTAURAR_SQLSERVER_FGET.ps1
    echo Ejecute este .bat desde la carpeta 06_RESTAURAR_SQLSERVER completa.
    echo.
    echo Presione una tecla para cerrar...
    pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0RESTAURAR_SQLSERVER_FGET.ps1"

if errorlevel 1 (
    echo.
    echo No se pudo iniciar o completar el script de PowerShell.
    echo.
    echo Puede deberse a la politica de ejecucion de Windows.
    echo Intente ejecutar este archivo como administrador o use PowerShell con:
    echo Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
    echo.
)

echo.
echo Presione una tecla para cerrar...
pause
