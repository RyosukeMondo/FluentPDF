@echo off
REM Launch FluentPDF Avalonia App

echo.
echo ====================================
echo   FluentPDF Avalonia Launcher
echo ====================================
echo.

cd /d "%~dp0src\FluentPDF.Avalonia"

echo Building project...
dotnet build --nologo -v:quiet
if errorlevel 1 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo Build successful!
echo.
echo Starting FluentPDF Avalonia...
echo (Close the window to exit)
echo.

dotnet run --no-build

echo.
echo App closed.
pause
