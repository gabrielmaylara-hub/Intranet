@echo off
setlocal
chcp 65001 >nul
set "SCRIPT_DIR=%~dp0"
set "SCRIPT=%SCRIPT_DIR%ASISTENTE_RESTAURACION_SQLSERVER.ps1"

cd /d "%SCRIPT_DIR%"

if not exist "%SCRIPT%" (
    echo No se encontro el asistente:
    echo %SCRIPT%
    pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%SCRIPT%"

if errorlevel 1 (
    echo.
    echo No se pudo iniciar o completar el asistente de PowerShell.
    echo.
    echo Puede deberse a la politica de ejecucion de Windows.
    echo Intente ejecutar este archivo como administrador o use PowerShell con:
    echo Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
    echo.
    echo Presione una tecla para cerrar...
    pause
)

endlocal
