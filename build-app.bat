@echo off
cd /d "%~dp0android"

echo =======================================================
echo          DualSenser - Compilando App Android           
echo =======================================================
echo.
echo Executando build do APK de depuração via Gradle...
echo.

call gradlew.bat assembleDebug

if %ERRORLEVEL% EQU 0 (
    echo.
    echo =======================================================
    echo [SUCESSO] APK gerado com sucesso!
    echo Localização: android\app\build\outputs\apk\debug\app-debug.apk
    echo =======================================================
) else (
    echo.
    echo [ERRO] Falha ao compilar o aplicativo Android.
)

pause
