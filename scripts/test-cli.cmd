@echo off
REM Quick CLI test wrapper for FluentPDF
REM Usage: test-cli.cmd [path\to\test.pdf]

if "%~1"=="" (
    powershell -ExecutionPolicy Bypass -File "%~dp0test-cli.ps1"
) else (
    powershell -ExecutionPolicy Bypass -File "%~dp0test-cli.ps1" -TestPdf "%~1"
)
