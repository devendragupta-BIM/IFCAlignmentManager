using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IFCAlignmentManager.Core;
using IFCAlignmentManager.Services;
using IFCAlignmentManager.Tests;
using IFCAlignmentManager.UI;

namespace IFCAlignmentManager
{
    public partial class ThisApplication
    {
        public UIApplication Application { get; set; }

        // MACRO 1: Main alignment wizard — shows full UI
        public void RunAlignmentWizard()
        {
            try
            {
                AlignmentWizardForm form = new AlignmentWizardForm(this.Application);
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("IFC Alignment Manager — Error", ex.ToString());
            }
        }

        // MACRO 2: Diagnostics only — no transformation
        public void RunDiagnosticsOnly()
        {
            try
            {
                Document hostDoc = this.Application?.ActiveUIDocument?.Document;
                if (hostDoc == null)
                {
                    TaskDialog.Show("Diagnostics Error", "No active document open to inspect.");
                    return;
                }
                var diagService = new CoordinateDiagnosticsService();
                CoordinateSnapshot snap = diagService.ReadSnapshot(hostDoc);
                DiagnosticsViewForm form = new DiagnosticsViewForm(snap);
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Diagnostics Error", ex.ToString());
            }
        }

        // MACRO 3: Manage saved profiles
        public void ManageProfiles()
        {
            try
            {
                ProfileManagerForm form = new ProfileManagerForm();
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Profile Manager Error", ex.ToString());
            }
        }

        // MACRO 4: Run self-tests
        public void RunSelfTests()
        {
            try
            {
                var runner = new TestRunner(this.Application);
                string results = runner.RunAllTests();
                TaskDialog.Show("IFC Alignment Manager — Test Results", results);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Test Runner Error", ex.ToString());
            }
        }
    }
}
