# Debug launcher to capture errors
$exePath = ".\src\FluentPDF.App\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe"

Write-Host "Attempting to launch FluentPDF..." -ForegroundColor Cyan
Write-Host "Executable: $exePath" -ForegroundColor Gray

if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: Executable not found!" -ForegroundColor Red
    exit 1
}

try {
    # Try to start the process and capture any errors
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = (Resolve-Path $exePath).Path
    $processInfo.UseShellExecute = $false
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.CreateNoWindow = $false

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo

    Write-Host "Starting process..." -ForegroundColor Yellow
    $started = $process.Start()

    if ($started) {
        Write-Host "Process started with PID: $($process.Id)" -ForegroundColor Green

        # Wait a bit to see if it crashes
        Start-Sleep -Seconds 2

        if ($process.HasExited) {
            Write-Host "Process exited with code: $($process.ExitCode)" -ForegroundColor Red

            $stdout = $process.StandardOutput.ReadToEnd()
            $stderr = $process.StandardError.ReadToEnd()

            if ($stdout) {
                Write-Host "`nStandard Output:" -ForegroundColor Yellow
                Write-Host $stdout
            }

            if ($stderr) {
                Write-Host "`nStandard Error:" -ForegroundColor Red
                Write-Host $stderr
            }
        } else {
            Write-Host "Process is running successfully!" -ForegroundColor Green
            Write-Host "Check if the window appeared on your screen." -ForegroundColor Cyan
        }
    } else {
        Write-Host "Failed to start process" -ForegroundColor Red
    }
} catch {
    Write-Host "Exception occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor Gray
}

Write-Host "`nPress any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
