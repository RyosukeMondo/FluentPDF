$ErrorActionPreference = "Continue"
$env:MSBUILDDEBUGENGINE = "1"

dotnet build src/FluentPDF.App/FluentPDF.App.csproj -p:Platform=x64 -v:d 2>&1 | Tee-Object -FilePath build-log.txt

Write-Host "Build log saved to build-log.txt"
