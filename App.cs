using System;
using System.IO;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IFCAlignmentManager.Core;
using IFCAlignmentManager.Services;
using IFCAlignmentManager.UI;

namespace IFCAlignmentManager
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                string tabName = "IFC Alignment";
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch
                {
                    // Tab might already exist
                }

                RibbonPanel panel = null;
                try
                {
                    panel = application.CreateRibbonPanel(tabName, "Coordinate Tools");
                }
                catch
                {
                    // Fallback to Add-Ins tab if custom tab cannot be created
                    panel = application.CreateRibbonPanel("Coordinate Tools");
                }

                string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

                // Button 1: Alignment Wizard
                var btnWizardData = new PushButtonData(
                    "cmdAlignmentWizard",
                    "Alignment\nWizard",
                    thisAssemblyPath,
                    "IFCAlignmentManager.AlignmentWizardCommand")
                {
                    ToolTip = "Launch the 5-step IFC Coordinate Alignment Wizard.",
                    LargeImage = AppIcons.ToBitmapSource(AppIcons.CreateWizardIcon(32)),
                    Image = AppIcons.ToBitmapSource(AppIcons.CreateWizardIcon(16))
                };

                // Button 2: Diagnostics View
                var btnDiagData = new PushButtonData(
                    "cmdDiagnosticsView",
                    "Diagnostics\nView",
                    thisAssemblyPath,
                    "IFCAlignmentManager.DiagnosticsViewCommand")
                {
                    ToolTip = "Inspect coordinate systems, base points, and shared origins.",
                    LargeImage = AppIcons.ToBitmapSource(AppIcons.CreateDiagnosticsIcon(32)),
                    Image = AppIcons.ToBitmapSource(AppIcons.CreateDiagnosticsIcon(16))
                };

                // Button 3: Profile Manager
                var btnProfileData = new PushButtonData(
                    "cmdProfileManager",
                    "Profile\nManager",
                    thisAssemblyPath,
                    "IFCAlignmentManager.ProfileManagerCommand")
                {
                    ToolTip = "Manage saved consultant coordinate alignment profiles.",
                    LargeImage = AppIcons.ToBitmapSource(AppIcons.CreateProfileIcon(32)),
                    Image = AppIcons.ToBitmapSource(AppIcons.CreateProfileIcon(16))
                };

                // Button 4: Self Tests
                var btnTestRunnerData = new PushButtonData(
                    "cmdSelfTests",
                    "Run Self\nTests",
                    thisAssemblyPath,
                    "IFCAlignmentManager.RunSelfTestsCommand")
                {
                    ToolTip = "Run automated regression tests for transformation math and API functions.",
                    LargeImage = AppIcons.ToBitmapSource(AppIcons.CreateTestsIcon(32)),
                    Image = AppIcons.ToBitmapSource(AppIcons.CreateTestsIcon(16))
                };

                panel.AddItem(btnWizardData);
                panel.AddItem(btnDiagData);
                panel.AddItem(btnProfileData);
                panel.AddItem(btnTestRunnerData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("IFC Alignment Manager — Startup Error", ex.ToString());
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class AlignmentWizardCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var wizard = new AlignmentWizardForm(commandData.Application);
                wizard.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Alignment Wizard Error", ex.ToString());
                return Result.Failed;
            }
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class DiagnosticsViewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var doc = commandData.Application.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    TaskDialog.Show("Diagnostics", "No active document open to inspect.");
                    return Result.Cancelled;
                }

                var diag = new CoordinateDiagnosticsService();
                CoordinateSnapshot snap = diag.ReadSnapshot(doc);
                var form = new DiagnosticsViewForm(snap);
                form.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Diagnostics Error", ex.ToString());
                return Result.Failed;
            }
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ProfileManagerCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var form = new ProfileManagerForm();
                form.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Profile Manager Error", ex.ToString());
                return Result.Failed;
            }
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class RunSelfTestsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var runner = new Tests.TestRunner(commandData.Application);
                string results = runner.RunAllTests();
                TaskDialog.Show("IFC Alignment Manager — Test Results", results);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Test Runner Error", ex.ToString());
                return Result.Failed;
            }
        }
    }
}
