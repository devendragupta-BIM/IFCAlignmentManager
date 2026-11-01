using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.UI;

namespace IFCAlignmentManager.Tests
{
    public class TestRunner
    {
        private readonly UIApplication _uiApp;

        public TestRunner(UIApplication uiApp = null)
        {
            _uiApp = uiApp;
        }

        public string RunAllTests()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== IFC Alignment Manager — Self-Test Suite ===");
            sb.AppendLine($"Execution Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            int passed = 0;
            int total = 0;

            // Group A: Pure Math Tests
            sb.AppendLine("--- Group A: Pure Math & Transform Tests ---");
            var groupA = TransformMathTests.RunAll();
            foreach (var item in groupA)
            {
                total++;
                if (item.Passed) passed++;
                sb.AppendLine(item.ToString());
            }

            sb.AppendLine();

            // Group B: Revit API Integration & I/O Tests
            sb.AppendLine("--- Group B: API Integration & I/O Tests ---");
            var groupB = CoordinateReadTests.RunAll(_uiApp);
            foreach (var item in groupB)
            {
                total++;
                if (item.Passed) passed++;
                sb.AppendLine(item.ToString());
            }

            sb.AppendLine();
            sb.AppendLine($"SUMMARY: {passed}/{total} tests passed ({((double)passed / total * 100):F1}%).");

            return sb.ToString();
        }
    }
}
