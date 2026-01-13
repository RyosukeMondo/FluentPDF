@echo off
setlocal

set ROOT=%~dp0
set CONFIG=Debug
set EXEDIR=%ROOT%src\FluentPDF.App\bin\x64\%CONFIG%\net8.0-windows10.0.19041.0\win-x64
set PDFIUM_SRC=%ROOT%libs\x64\bin\pdfium.dll
set PDFIUM_DST=%EXEDIR%\pdfium.dll
set QPDF_SRC=%ROOT%libs\x64\bin\qpdf.dll
set QPDF_DST=%EXEDIR%\qpdf.dll

echo === FluentPDF Launcher ===
echo.

REM Check if exe exists
if not exist "%EXEDIR%\FluentPDF.App.exe" (
    echo ERROR: FluentPDF.App.exe not found!
    echo Run: dotnet build src\FluentPDF.App -c %CONFIG% -p:Platform=x64
    pause
    exit /b 1
)
echo [OK] FluentPDF.App.exe found

REM Check and copy pdfium.dll
if not exist "%PDFIUM_DST%" (
    if exist "%PDFIUM_SRC%" (
        echo Copying pdfium.dll to app directory...
        copy "%PDFIUM_SRC%" "%PDFIUM_DST%" >nul
    ) else (
        echo ERROR: pdfium.dll not found in libs\x64\bin\
        echo Run: powershell -ExecutionPolicy Bypass -File tools\download-pdfium.ps1
        pause
        exit /b 1
    )
)
echo [OK] pdfium.dll ready

REM Check and copy qpdf.dll
if not exist "%QPDF_DST%" (
    if exist "%QPDF_SRC%" (
        echo Copying qpdf.dll to app directory...
        copy "%QPDF_SRC%" "%QPDF_DST%" >nul
    ) else (
        echo WARNING: qpdf.dll not found - merge/split features may not work
    )
)
if exist "%QPDF_DST%" echo [OK] qpdf.dll ready

REM Delete old debug log
echo.
echo Deleting old debug log...
del "%LOCALAPPDATA%\FluentPDF_Debug.log" 2>nul

echo Running FluentPDF.App.exe...
echo.
"%EXEDIR%\FluentPDF.App.exe"

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
