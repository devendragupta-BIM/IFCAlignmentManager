# IFC Alignment Manager

**A free, open-source Autodesk Revit 2026 add-in that automatically aligns IFC consultant models to a host project's shared coordinate system â€” in seconds, with one-click undo.**

Built by [Devendra Gupta](https://www.linkedin.com/in/) Â· MIT License Â· Revit 2026 Â· .NET 10 Â· Zero dependencies

---

## The Problem

In any multi-discipline BIM project, you open an architect's IFC, a structural IFC, and an MEP IFC â€” and none of them sit in the same place. Coordinates are off by thousands of feet, True North is rotated, survey points don't match. You fix it manually, the consultant sends a revised model, and you do it all again.

IFC Alignment Manager eliminates this loop.

---

## What It Does

- **Reads the full coordinate state** of any two open Revit documents â€” Project Base Point, Survey Point, Shared Origin, True North angle, and bounding box â€” in a single diagnostic pass.
- **Computes the exact translation and rotation** needed to align the consultant model to the host, using your choice of method: Project Base Point, Survey Point, Internal Origin, or manual two-point pick.
- **Applies the transform** via Revit's native `ProjectPosition` API (shared coordinates, no geometry moved) with a full transaction that rolls back automatically on any error.
- **Saves a restore point** (JSON snapshot of the original position) before every alignment, giving you one-click undo even after closing the wizard.
- **Saves alignment profiles** per consultant, so when they send a revised IFC next week you re-align in two clicks without recalculating anything.
- **Exports an HTML report** with a full coordinate comparison table, transform values in both feet and meters, severity badges, and a residual error result â€” ready to attach to your project handover package.
- **Exports and imports profile bundles** as ZIP archives, so a BIM manager can distribute a project's full alignment setup to the whole team at once.

---

## Screenshots

> _Add screenshots here after first run: Ribbon tab, Step 3 transform preview, HTML report_

---

## Requirements

| Item | Value |
|---|---|
| Autodesk Revit | **2026** |
| .NET Runtime | 10.0 (bundled with Revit 2026) |
| OS | Windows 10 / 11 (64-bit) |
| NuGet packages | **None** |
| Admin rights | Not required for per-user install |

---

## Installation

### Option A â€” Add-in (Recommended, gives you a Ribbon tab)

1. Download the latest release ZIP from the [Releases](../../releases) page.
2. Unzip it.
3. Close Revit 2026 if it is open.
4. Run `deploy.ps1` in PowerShell:
   ```powershell
   .\deploy.ps1
   ```
5. Launch Revit 2026. You will see an **IFC Alignment** tab in the ribbon with four buttons.

> The script copies files to `%APPDATA%\Autodesk\Revit\Addins\2026\` (per-user) and `%ProgramData%\Autodesk\Revit\Addins\2026\` (all-users). It backs up any existing installation and rolls back if the copy fails.

### Option B â€” Macro Manager (no install, portable)

1. In Revit 2026, open **Manage â†’ Macro Manager**.
2. Under **Revit**, click **Create** and name the module `IFCAlignmentManager`.
3. Copy all `.cs` files from `Core\`, `Services\`, `UI\`, `Tests\`, plus `App.cs` and `ThisApplication.cs` into the macro folder shown by the Macro Manager.
4. Click **Build** in Macro Manager.
5. Run any of the four macros: `RunAlignmentWizard`, `RunDiagnosticsOnly`, `ManageProfiles`, `RunSelfTests`.

---

## Building from Source

```powershell
# Prerequisites: Visual Studio 2022 or .NET 10 SDK, Revit 2026 installed

git clone https://github.com/devendragupta-BIM/IFCAlignmentManager.git
cd IFCAlignmentManager\IFCAlignmentManager

# The .csproj references RevitAPI.dll and RevitAPIUI.dll from the default
# Revit 2026 install path: C:\Program Files\Autodesk\Revit 2026\
# If yours is different, update the HintPath in the .csproj before building.

dotnet build -c Release
# Expected: 0 errors, 0 warnings, output in bin\Release\
```

---

## Usage

### Alignment Wizard (main workflow)

1. Open your **host Revit model** and the **consultant's IFC-linked model** in the same Revit session.
2. Click **Alignment Wizard** on the IFC Alignment ribbon tab.
3. **Step 1 â€” Select Documents:** Choose host and consultant from the dropdowns.
4. **Step 2 â€” Diagnostics:** Review the coordinate delta table. Severity is shown as `None / Minor / Major / Critical`.
5. **Step 3 â€” Method & Preview:** Choose an alignment method (Project Base Point recommended for most IFC workflows). The transform preview shows translation in both feet and meters and an estimated residual error.
6. **Step 4 â€” Confirm:** Review the summary. Tick **Save as profile** and give it a name if you want to reuse this alignment next revision.
7. **Step 5 â€” Result:** A green tick confirms success. The post-alignment residual error is shown. Click **Open Report** for the full HTML report. Click **Undo** to restore the original position using the automatically saved restore point.

### Alignment Methods

| Method | Use when |
|---|---|
| **Project Base Point** | Both models have meaningful, comparable PBP locations. Most common for IFC. |
| **Survey Point** | Models are georeferenced to a real-world survey coordinate. |
| **Internal Origin** | IFC was exported from a tool that uses internal origin as its reference. |
| **Manual Two-Point** | None of the above. Pick two corresponding points in each model's view. |

### Diagnostics View

Click **Diagnostics** to inspect any open document's coordinate state without running any alignment. Useful for auditing a model before a project handover meeting.

### Profile Manager

Saved profiles store the full transform (translation + rotation + pivot) and a baseline coordinate snapshot. On the next IFC revision, the wizard validates that the consultant model's starting coordinates match the baseline before applying. If they don't match (e.g. the consultant changed their origin), you get a clear warning rather than a silent misalignment.

**Sharing profiles across a team:**
- BIM Manager: open Profile Manager â†’ **Export All** â†’ share the `.zip`
- Team member: open Profile Manager â†’ **Import** â†’ select the `.zip`

---

## Running Self-Tests

Click **Run Self Tests** in the ribbon (or call macro `RunSelfTests`). All 15 unit tests run without touching any open model and report pass/fail in a Revit dialog. All 15 must pass before using the tool in production.

Tests cover: zero transform, pure translation X/Y, 45Â° rotation, combined transform, two-point alignment, residual error calculation, national-grid-scale coordinates, unit conversion, mismatch detection, sub-milliradian angle handling, locale-safe restore point JSON, and profile ZIP round-trip.

---

## Project Structure

```
IFCAlignmentManager/
â”œâ”€â”€ App.cs                          # Revit IExternalApplication â€” ribbon registration
â”œâ”€â”€ ThisApplication.cs              # Macro Manager entry points (4 macros)
â”œâ”€â”€ IFCAlignmentManager.addin       # Revit add-in manifest
â”œâ”€â”€ deploy.ps1                      # Deployment script (user + all-user + macro paths)
â”‚
â”œâ”€â”€ Core/
â”‚   â”œâ”€â”€ AppVersion.cs               # Version constant (v2.0.0)
â”‚   â”œâ”€â”€ AlignmentProfile.cs         # ConsultantAlignmentProfile data contract
â”‚   â”œâ”€â”€ CoordinateData.cs           # CoordinateSnapshot, AlignmentRestorePoint
â”‚   â”œâ”€â”€ DiagnosticsResult.cs        # DiagnosticsResult data contract
â”‚   â”œâ”€â”€ AlignmentTransformResult.cs # Proposed transform output
â”‚   â”œâ”€â”€ TransformResult.cs          # Applied transform result
â”‚   â””â”€â”€ ProfileValidationResult.cs  # Profile compatibility check result
â”‚
â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ CoordinateDiagnosticsService.cs  # Reads PBP, SP, shared origin, units, bbox
â”‚   â”œâ”€â”€ TransformCalculatorService.cs    # Computes translation + rotation vector
â”‚   â”œâ”€â”€ AlignmentExecutionService.cs     # Applies transform via ProjectPosition API
â”‚   â”œâ”€â”€ ValidationService.cs             # Pre- and post-alignment validation
â”‚   â”œâ”€â”€ ProfileStorageService.cs         # Save/load/export/import profiles (JSON + ZIP)
â”‚   â””â”€â”€ HtmlReportService.cs             # Generates styled HTML alignment report
â”‚
â”œâ”€â”€ UI/
â”‚   â”œâ”€â”€ AlignmentWizardForm.cs      # 5-step alignment wizard (WinForms)
â”‚   â”œâ”€â”€ DiagnosticsViewForm.cs      # Read-only coordinate inspector
â”‚   â”œâ”€â”€ ProfileManagerForm.cs       # Saved profile browser + export/import
â”‚   â””â”€â”€ AppIcons.cs                 # Programmatic icon generation (GDI+, no image files needed)
â”‚
â”œâ”€â”€ Tests/
â”‚   â”œâ”€â”€ TransformMathTests.cs       # 15 pure-math unit tests (no Revit session needed)
â”‚   â”œâ”€â”€ CoordinateReadTests.cs      # API integration tests (require open documents)
â”‚   â””â”€â”€ TestRunner.cs               # Orchestrates all tests, returns formatted report
â”‚
â””â”€â”€ Icons/                          # Exported PNG icons for ribbon buttons
    â”œâ”€â”€ Wizard_32.png / Wizard_16.png
    â”œâ”€â”€ Diagnostics_32.png / Diagnostics_16.png
    â”œâ”€â”€ Profile_32.png / Profile_16.png
    â””â”€â”€ Tests_32.png / Tests_16.png
```

---

## Design Constraints

This project was built under strict constraints to ensure it installs and runs on any Revit 2026 machine without any IT involvement:

- **Zero NuGet / third-party packages.** Only `RevitAPI.dll`, `RevitAPIUI.dll`, and the .NET 10 BCL.
- **WinForms only.** No WPF, no XAML, no additional runtimes.
- **All Revit writes inside a `Transaction` with `try/catch/RollBack`.** The model is never left in a partial state.
- **JSON via `System.Runtime.Serialization.Json` only.** No `Newtonsoft.Json`, no `System.Text.Json`.
- **HTML reports only.** No PDF generation libraries.
- **Restore point before every alignment.** One-click undo is always available.

---

## Known Limitations

- **Revit 2026 only.** The API surface used (`ProjectPosition`, `WorksharingUtils`, `ProgressHandle`) targets Revit 2026 specifically. Earlier versions are not supported and will not build correctly.
- **Geometry fallback is slow on large models.** The `ProjectPosition` path (default) is instant. The geometry fallback (used when `ProjectPosition` is unavailable) moves all non-pinned elements and can take several minutes on dense models. A progress dialog is shown and the operation is cancellable.
- **BIM 360 / Revit Server worksharing** requires the ProjectLocation element to be checked out. The tool attempts this automatically but cannot override another user's checkout.
- **Macro Manager path** requires Revit to recompile the source files on first use. The ribbon add-in path (Option A install) is recommended for production use.

---

## Contributing

Pull requests are welcome. Please:

1. Fork the repo and create a feature branch.
2. Apply changes only to files in `Core/`, `Services/`, `UI/`, or `Tests/`. Do not add NuGet packages.
3. Run `RunSelfTests` in Revit and confirm all 15 pass.
4. Update `BUILD_NOTES.md` with what you changed and the test results.
5. Open a PR describing the problem solved and how you tested it.

---

## License

MIT â€” free to use, modify, and distribute. See [LICENSE](LICENSE) for details.

---

## Author

**Devendra Gupta** â€” BIM Engineer  
Built to solve a real problem on real projects. If it saves you time, a star on the repo is appreciated.

