# This script fixes PdfViewerViewModel constructor calls to include RenderingCoordinator and UIBindingVerifier
# The two new parameters must be added before the ILogger parameter

$ErrorActionPreference = "Stop"

function Fix-ConstructorCall {
    param(
        [string]$FilePath
    )
    
    Write-Host "Processing $FilePath"
    
    $lines = Get-Content $FilePath -Encoding UTF8
    $newLines = @()
    $i = 0
    
    while ($i -lt $lines.Count) {
        $line = $lines[$i]
        
        # Check if this is a PdfViewerViewModel constructor call
        if ($line -match 'new PdfViewerViewModel\s*\(') {
            # Add this line
            $newLines += $line
            $i++
            
            # Now we need to find the closing line with logger parameter
            # Track parenthesis depth
            $depth = ($line.ToCharArray() | Where-Object {$_ -eq '('}).Count - ($line.ToCharArray() | Where-Object {$_ -eq ')'}).Count
            $constructorLines = @()
            
            while ($i -lt $lines.Count -and $depth -gt 0) {
                $line = $lines[$i]
                $constructorLines += $line
                $depth += ($line.ToCharArray() | Where-Object {$_ -eq '('}).Count
                $depth -= ($line.ToCharArray() | Where-Object {$_ -eq ')'}).Count
                $i++
            }
            
            # Now process the constructor lines
            # Find the last parameter line (should be logger)
            for ($j = $constructorLines.Count - 1; $j >= 0; $j--) {
                $cline = $constructorLines[$j]
                
                # Check if this line has a logger parameter
                if ($cline -match '^\s+.*Logger.*\);?\s*$' -or 
                    $cline -match '^\s+_loggerMock\.Object\s*\);?\s*$' -or
                    $cline -match '^\s+_viewerLoggerMock\.Object\s*\);?\s*$' -or
                    $cline -match '^\s+Mock\.Of<ILogger<.*>>\(\)\s*\);?\s*$') {
                    
                    # Get indentation
                    $indent = $cline -replace '^(\s+).*', '$1'
                    
                    # Insert the two new lines before this one
                    $constructorLines = $constructorLines[0..($j-1)] + 
                                       @("$indent" + "Mock.Of<FluentPDF.App.Services.RenderingCoordinator>(),",
                                         "$indent" + "Mock.Of<FluentPDF.App.Services.UIBindingVerifier>(),") +
                                       $constructorLines[$j..($constructorLines.Count-1)]
                    break
                }
            }
            
            # Add all constructor lines
            $newLines += $constructorLines
        }
        else {
            $newLines += $line
            $i++
        }
    }
    
    # Write back
    $newLines | Set-Content $FilePath -Encoding UTF8
    Write-Host "  Fixed!"
}

# Fix each file
Fix-ConstructorCall "C:\Users\ryosu\repos\FluentPDF\tests\FluentPDF.App.Tests\ViewModels\PdfViewerViewModelTests.cs"
Fix-ConstructorCall "C:\Users\ryosu\repos\FluentPDF\tests\FluentPDF.App.Tests\ViewModels\TabViewModelTests.cs"
Fix-ConstructorCall "C:\Users\ryosu\repos\FluentPDF\tests\FluentPDF.App.Tests\Integration\SaveWorkflowTests.cs"
Fix-ConstructorCall "C:\Users\ryosu\repos\FluentPDF\tests\FluentPDF.App.Tests\Snapshots\ToolbarSnapshotTests.cs"
Fix-ConstructorCall "C:\Users\ryosu\repos\FluentPDF\tests\FluentPDF.App.Tests\Snapshots\PdfViewerSnapshotTests.cs"
Fix-ConstructorCall "C:\Users\ryosu\repos\FluentPDF\tests\FluentPDF.App.Tests\ViewModels\PdfViewerViewModelDpiTests.cs"
Fix-ConstructorCall "C:\Users\ryosu\repos\FluentPDF\tests\FluentPDF.App.Tests\ViewModels\PdfViewerViewModelSaveTests.cs"
Fix-ConstructorCall "C:\Users\ryosu\repos\FluentPDF\tests\FluentPDF.App.Tests\ViewModels\PdfViewerViewModelSearchTests.cs"
Fix-ConstructorCall "C:\Users\ryosu\repos\FluentPDF\tests\FluentPDF.App.Tests\ViewModels\MainViewModelTests.cs"

Write-Host "All files processed!"
