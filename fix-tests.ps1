$files = @(
    "tests\FluentPDF.App.Tests\ViewModels\TabViewModelTests.cs",
    "tests\FluentPDF.App.Tests\Integration\SaveWorkflowTests.cs",
    "tests\FluentPDF.App.Tests\Snapshots\ToolbarSnapshotTests.cs",
    "tests\FluentPDF.App.Tests\Snapshots\PdfViewerSnapshotTests.cs",
    "tests\FluentPDF.App.Tests\ViewModels\PdfViewerViewModelDpiTests.cs",
    "tests\FluentPDF.App.Tests\ViewModels\PdfViewerViewModelSaveTests.cs",
    "tests\FluentPDF.App.Tests\ViewModels\PdfViewerViewModelSearchTests.cs",
    "tests\FluentPDF.App.Tests\ViewModels\PdfViewerViewModelTests.cs",
    "tests\FluentPDF.App.Tests\ViewModels\MainViewModelTests.cs"
)

foreach ($file in $files) {
    $path = "C:\Users\ryosu\repos\FluentPDF\$file"
    Write-Host "Processing $file"
    
    $content = Get-Content $path -Encoding UTF8
    
    # Process line by line to maintain file structure
    $newContent = @()
    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i]
        
        # Check if this line has the logger parameter
        if ($line -match '^\s+(_loggerMock\.Object|Mock\.Of<ILogger<.*>>\(\))\);?\s*$') {
            # Get the indentation
            $indent = $line -replace '^(\s+).*', '$1'
            
            # Add the two new parameters before this line
            $newContent += "$indent" + "Mock.Of<FluentPDF.App.Services.RenderingCoordinator>(),"
            $newContent += "$indent" + "Mock.Of<FluentPDF.App.Services.UIBindingVerifier>(),"
            $newContent += $line
        } else {
            $newContent += $line
        }
    }
    
    $newContent | Set-Content $path -Encoding UTF8
}

Write-Host "Done!"
