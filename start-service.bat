@echo off
cd /d "%~dp0server\DualSenser.Service"
echo Iniciando DualSenser Service...
dotnet run
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Erro ao executar o servico.
)
pause
