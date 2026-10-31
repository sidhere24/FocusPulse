@echo off
setlocal
cd /d "%~dp0"

echo Detecting system architecture...
if "%PROCESSOR_ARCHITECTURE%"=="ARM64" (
    echo Launching FocusPulse for ARM64...
    start "" "bin\FocusPulse-ARM64.exe"
) else if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    echo Launching FocusPulse for x64...
    start "" "bin\FocusPulse-x64.exe"
) else (
    echo Launching FocusPulse for x86...
    start "" "bin\FocusPulse-x86.exe"
)

exit /b 0
