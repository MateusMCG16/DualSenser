@echo off
title DualSenser Service
cd /d "%~dp0server\DualSenser.Service"

echo =======================================================
echo              DualSenser - Iniciando Servico            
echo =======================================================
echo.
echo Pressione CTRL+C para encerrar o servico a qualquer momento.
echo.

dotnet run --no-launch-profile
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Servico encerrado.
)
