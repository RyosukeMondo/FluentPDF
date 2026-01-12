@echo off
echo Checking pdfium.dll...
if exist "C:\dev\FluentPDF\src\FluentPDF.App\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\pdfium.dll" (
    echo pdfium.dll found!
) else (
    echo ERROR: pdfium.dll not found!
)

echo.
echo Deleting old debug log...
del "%LOCALAPPDATA%\FluentPDF_Debug.log" 2>nul

echo Running FluentPDF.App.exe...
"C:\dev\FluentPDF\src\FluentPDF.App\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"

echo.
echo Application exited with code: %ERRORLEVEL%
echo.
echo === Debug Log ===
if exist "%LOCALAPPDATA%\FluentPDF_Debug.log" (
    type "%LOCALAPPDATA%\FluentPDF_Debug.log"
) else (
    echo No debug log created - app may have crashed before App constructor ran
)
echo.
pause
