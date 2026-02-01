#!/usr/bin/env pwsh
# Generate minimal XAML code-behind stub files to bypass XAML compiler

param(
    [string]$ProjectPath = "src/FluentPDF.App"
)

$ErrorActionPreference = "Stop"

# Get all XAML files
$xamlFiles = Get-ChildItem -Path $ProjectPath -Filter "*.xaml" -Recurse | Where-Object {
    $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\"
}

Write-Host "Found $($xamlFiles.Count) XAML files"

foreach ($xamlFile in $xamlFiles) {
    $xamlContent = Get-Content $xamlFile.FullName -Raw

    # Extract class name from x:Class attribute
    if ($xamlContent -match 'x:Class="([^"]+)"') {
        $fullClassName = $Matches[1]
        $className = $fullClassName.Split('.')[-1]
        $namespace = $fullClassName.Substring(0, $fullClassName.LastIndexOf('.'))

        Write-Host "Processing $className..."

        # Extract x:Name elements with their types
        # Match pattern: <ElementType ... x:Name="ElementName" ...
        $nameMatches = [regex]::Matches($xamlContent, '<(\w+(?::\w+)?)[^>]*x:Name="([^"]+)"')
        $namedElements = @{}

        # Known type mappings for common WinUI types
        $typeMappings = @{
            'VisualState' = 'global::Microsoft.UI.Xaml.VisualState'
            'VisualStateGroup' = 'global::Microsoft.UI.Xaml.VisualStateGroup'
            'Rectangle' = 'global::Microsoft.UI.Xaml.Shapes.Rectangle'
            'Ellipse' = 'global::Microsoft.UI.Xaml.Shapes.Ellipse'
            'Path' = 'global::Microsoft.UI.Xaml.Shapes.Path'
            'Line' = 'global::Microsoft.UI.Xaml.Shapes.Line'
            'Polygon' = 'global::Microsoft.UI.Xaml.Shapes.Polygon'
            'Polyline' = 'global::Microsoft.UI.Xaml.Shapes.Polyline'
            'Canvas' = 'global::Microsoft.UI.Xaml.Controls.Canvas'
            'Grid' = 'global::Microsoft.UI.Xaml.Controls.Grid'
            'StackPanel' = 'global::Microsoft.UI.Xaml.Controls.StackPanel'
            'Border' = 'global::Microsoft.UI.Xaml.Controls.Border'
            'ScrollViewer' = 'global::Microsoft.UI.Xaml.Controls.ScrollViewer'
            'TextBlock' = 'global::Microsoft.UI.Xaml.Controls.TextBlock'
            'TextBox' = 'global::Microsoft.UI.Xaml.Controls.TextBox'
            'Button' = 'global::Microsoft.UI.Xaml.Controls.Button'
            'Image' = 'global::Microsoft.UI.Xaml.Controls.Image'
            'MenuFlyoutItem' = 'global::Microsoft.UI.Xaml.Controls.MenuFlyoutItem'
            'MenuFlyoutSubItem' = 'global::Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem'
        }

        # Custom controls in this project
        $customControls = @('AnnotationLayer', 'ContinuousScrollViewer', 'TwoPageViewer',
                           'ThumbnailsSidebar', 'ValidationErrorPanel', 'DiagnosticsPanelControl',
                           'ImageManipulationOverlay', 'FormFieldControl', 'BookmarksPanel',
                           'PdfViewerControl', 'LogViewerControl')

        foreach ($match in $nameMatches) {
            $elementType = $match.Groups[1].Value
            $elementName = $match.Groups[2].Value

            # Remove namespace prefix if present
            if ($elementType -match ':') {
                $elementType = $elementType.Split(':')[1]
            }

            # Determine C# type
            if ($typeMappings.ContainsKey($elementType)) {
                $csType = $typeMappings[$elementType]
            } elseif ($customControls -contains $elementType) {
                $csType = "global::FluentPDF.App.Controls.$elementType"
            } else {
                # Default to Microsoft.UI.Xaml.Controls
                $csType = "global::Microsoft.UI.Xaml.Controls.$elementType"
            }

            $namedElements[$elementName] = $csType
        }

        # Determine output path
        $projectFullPath = (Resolve-Path $ProjectPath).Path
        $relativePath = $xamlFile.FullName.Substring($projectFullPath.Length + 1)
        $relativeDir = Split-Path $relativePath -Parent
        $outputDir = Join-Path $projectFullPath "obj\x64\Debug\net8.0-windows10.0.19041.0\win-x64\$relativeDir"
        $outputFile = Join-Path $outputDir "$className.g.cs"

        # Create directory if needed
        New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

        # Determine base type
        $baseType = "global::Microsoft.UI.Xaml.Window"
        if ($xamlContent -match '<Application') { $baseType = "global::Microsoft.UI.Xaml.Application" }
        elseif ($xamlContent -match '<Page') { $baseType = "global::Microsoft.UI.Xaml.Controls.Page" }
        elseif ($xamlContent -match '<ContentDialog') { $baseType = "global::Microsoft.UI.Xaml.Controls.ContentDialog" }
        elseif ($xamlContent -match '<UserControl') { $baseType = "global::Microsoft.UI.Xaml.Controls.UserControl" }

        # Convert backslashes to forward slashes for URI
        $relativePathUri = $relativePath.Replace('\', '/')

        # Generate stub .g.cs file
        $stubContent = @"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool (stub generator).
//     Runtime Version: Stub
// </auto-generated>
//------------------------------------------------------------------------------

namespace $namespace
{
    partial class $className : $baseType
    {
        private bool _contentLoaded;

        /// <summary>
        /// InitializeComponent()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent()
        {
            if (_contentLoaded)
                return;

            _contentLoaded = true;

            global::System.Uri resourceLocator = new global::System.Uri("ms-appx:///$relativePathUri");
            global::Microsoft.UI.Xaml.Application.LoadComponent(this, resourceLocator, global::Microsoft.UI.Xaml.Controls.Primitives.ComponentResourceLocation.Application);
        }

        partial void UnloadObject(global::Microsoft.UI.Xaml.DependencyObject unloadableObject);

"@

        # Add field declarations for x:Name elements
        if ($namedElements.Count -gt 0) {
            $stubContent += "`n#pragma warning disable CS0169, CS0649 // Field never used/assigned`n"
            foreach ($entry in $namedElements.GetEnumerator()) {
                $stubContent += "        private $($entry.Value) $($entry.Key);`n"
            }
            $stubContent += "#pragma warning restore CS0169, CS0649`n"
        }

        $stubContent += @"
    }
}
"@

        # Add Main entry point for Application classes
        if ($baseType -eq "global::Microsoft.UI.Xaml.Application") {
            $stubContent += @"

namespace $namespace
{
    public static class Program
    {
        [global::System.STAThreadAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler"," 1.0.0.0")]
        public static void Main(string[] args)
        {
            global::WinRT.ComWrappersSupport.InitializeComWrappers();
            global::Microsoft.UI.Xaml.Application.Start((p) => {
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new $className();
            });
        }
    }
}
"@
        }

        # Write stub file
        Set-Content -Path $outputFile -Value $stubContent -Encoding UTF8
        Write-Host "  Generated: $outputFile"
    }
}

Write-Host ""
Write-Host "✓ Generated $($xamlFiles.Count) stub files" -ForegroundColor Green
