# Filter WPF assemblies from XamlCompiler input.json to prevent crashes
# This script removes WPF assemblies that conflict with WinUI 3 XamlCompiler

param(
    [Parameter(Mandatory=$true)]
    [string]$InputJsonPath
)

if (-not (Test-Path $InputJsonPath)) {
    Write-Error "Input JSON file not found: $InputJsonPath"
    exit 1
}

Write-Host "Filtering WPF assemblies from: $InputJsonPath"

try {
    # Read and parse JSON
    $json = Get-Content $InputJsonPath -Raw | ConvertFrom-Json

    # WPF assembly name patterns to exclude
    $wpfPatterns = @(
        'PresentationCore',
        'PresentationFramework',
        'PresentationUI',
        'System.Windows.Forms',
        'System.Windows.Presentation',
        'WindowsFormsIntegration',
        'WindowsBase',
        'ReachFramework',
        'UIAutomation',
        'System.Xaml'
    )

    # Filter ReferenceAssemblies
    if ($json.ReferenceAssemblies) {
        $originalCount = $json.ReferenceAssemblies.Count
        $filtered = @()

        foreach ($ref in $json.ReferenceAssemblies) {
            $fullPath = $ref.FullPath
            $fileName = Split-Path -Leaf $fullPath

            $isWpf = $false
            foreach ($pattern in $wpfPatterns) {
                if ($fileName -like "*$pattern*") {
                    Write-Host "  Excluding: $fileName"
                    $isWpf = $true
                    break
                }
            }

            if (-not $isWpf) {
                $filtered += $ref
            }
        }

        $json.ReferenceAssemblies = $filtered
        $removedCount = $originalCount - $filtered.Count
        Write-Host "Removed $removedCount WPF assemblies ($originalCount -> $($filtered.Count))"
    }

    # Write back to file
    $json | ConvertTo-Json -Depth 100 | Set-Content $InputJsonPath -Encoding UTF8
    Write-Host "Successfully filtered input.json"
    exit 0
}
catch {
    Write-Error "Failed to filter input.json: $_"
    exit 1
}
