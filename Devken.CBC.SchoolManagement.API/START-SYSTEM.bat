@echo off
REM ═══════════════════════════════════════════════════════════
REM DevKen School Management System - Quick Launcher
REM ═══════════════════════════════════════════════════════════
REM Double-click this file to start both API and Angular UI
REM ═══════════════════════════════════════════════════════════

setlocal enabledelayedexpansion

REM Get the directory where this batch file is located
set "SCRIPT_DIR=%~dp0"

echo.
echo ═══════════════════════════════════════════════════════════
echo  DevKen School Management System - Starting...
echo ═══════════════════════════════════════════════════════════
echo.

REM Run the PowerShell script
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Start-DevKenSystem.ps1"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ Error occurred while starting the system
    echo.
    pause
    exit /b 1
)

endlocal
