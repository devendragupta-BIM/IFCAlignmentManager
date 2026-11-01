param(
    [switch]$Force   # pass -Force to skip the Revit-running guard
)

$ErrorActionPreference = "Stop"

$userAddinDir    = [IO.Path]::Combine($env:APPDATA, 'Autodesk', 'Revit', 'Addins', '2026')
$allUserAddinDir = [IO.Path]::Combine($env:ProgramData, 'Autodesk', 'Revit', 'Addins', '2026')
$macroDir        = [IO.Path]::Combine($env:APPDATA, 'Autodesk', 'Revit', 'Macros', '2026', 'Revit', 'AppHookup')
$sourceDll       = 'bin\Release\IFCAlignmentManager.dll'

# --- Guard: Revit must not be running ---
if (-not $Force)
{
    $revitProcs = Get-Process -Name "Revit" -ErrorAction SilentlyContinue
    if ($revitProcs)
    {
        Write-Error ("Revit is currently running (PID: " + ($revitProcs.Id -join ', ') + "). " +
                     "Close Revit before deploying, or pass -Force to skip this check.")
        exit 1
    }
}

# --- Guard: DLL must exist ---
if (-not (Test-Path $sourceDll))
{
    Write-Error "Build output not found at '$sourceDll'. Run 'dotnet build -c Release' first."
    exit 1
}

# --- Helper: deploy with backup-then-replace ---
function Deploy-File {
    param([string]$Src, [string]$Dst)
    $destDir = Split-Path $Dst -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
    if (Test-Path $Dst)
    {
        $backup = $Dst + ".bak"
        Copy-Item $Dst -Destination $backup -Force
    }
    try   { Copy-Item $Src -Destination $Dst -Force }
    catch {
        # Roll back if copy failed (e.g. file lock)
        $backup = $Dst + ".bak"
        if (Test-Path $backup) { Copy-Item $backup -Destination $Dst -Force; Write-Warning "Rolled back $Dst from backup." }
        throw
    }
}

$filesToDeploy = @(
    @{ Src = 'IFCAlignmentManager.addin';              Dst = 'IFCAlignmentManager.addin' }
    @{ Src = 'bin\Release\IFCAlignmentManager.dll';    Dst = 'IFCAlignmentManager.dll'   }
    @{ Src = 'bin\Release\IFCAlignmentManager.pdb';    Dst = 'IFCAlignmentManager.pdb'   }
    @{ Src = 'bin\Release\IFCAlignmentManager.deps.json'; Dst = 'IFCAlignmentManager.deps.json' }
)

foreach ($target in @($userAddinDir, $allUserAddinDir))
{
    foreach ($f in $filesToDeploy)
    {
        Deploy-File -Src $f.Src -Dst (Join-Path $target $f.Dst)
    }
    if (Test-Path 'Icons') { Copy-Item 'Icons' -Destination $target -Recurse -Force }
    Write-Output "Deployed to: $target"
}

# Macro directory — source files only (no .addin manifest)
$macroSources = @(
    'ThisApplication.cs','App.cs',
    'Core\AlignmentProfile.cs','Core\AlignmentTransformResult.cs',
    'Core\AppVersion.cs','Core\CoordinateData.cs',
    'Core\DiagnosticsResult.cs','Core\ProfileValidationResult.cs','Core\TransformResult.cs',
    'Services\AlignmentExecutionService.cs','Services\CoordinateDiagnosticsService.cs',
    'Services\HtmlReportService.cs','Services\ProfileStorageService.cs',
    'Services\TransformCalculatorService.cs','Services\ValidationService.cs',
    'UI\AlignmentWizardForm.cs','UI\AppIcons.cs',
    'UI\DiagnosticsViewForm.cs','UI\ProfileManagerForm.cs',
    'Tests\CoordinateReadTests.cs','Tests\TestRunner.cs','Tests\TransformMathTests.cs'
)
foreach ($f in $macroSources)
{
    if (Test-Path $f)
    {
        $dest = Join-Path $macroDir (Split-Path $f -Leaf)
        Copy-Item $f -Destination $dest -Force
    }
}
foreach ($f in $filesToDeploy | Where-Object { $_.Src -ne 'IFCAlignmentManager.addin' })
{
    Deploy-File -Src $f.Src -Dst (Join-Path $macroDir $f.Dst)
}
Write-Output "Deployed to Macro AppHookup: $macroDir"

Write-Output ""
Write-Output "Deployment complete. Launch Revit 2026 to verify the 'IFC Alignment' ribbon tab."
