using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using IFCAlignmentManager.Core;

namespace IFCAlignmentManager.Services
{
    public class HtmlReportService
    {
        public string GenerateReport(
            DiagnosticsResult diagnostics,
            AlignmentTransformResult proposedTransform,
            TransformResult appliedResult,
            List<string> warnings)
        {
            var sb = new StringBuilder();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var host = diagnostics?.HostSnapshot;
            var consultant = diagnostics?.ConsultantSnapshot;

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset='UTF-8'>");
            sb.AppendLine($"  <title>IFC Alignment Report — {timestamp}</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 2em; color: #222; background-color: #fdfdfd; }");
            sb.AppendLine("    h1 { color: #005a9c; border-bottom: 2px solid #005a9c; padding-bottom: 6px; }");
            sb.AppendLine("    h2 { color: #333; margin-top: 1.8em; border-left: 4px solid #005a9c; padding-left: 8px; }");
            sb.AppendLine("    table { border-collapse: collapse; width: 100%; margin: 1em 0; background: #fff; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }");
            sb.AppendLine("    th { background: #005a9c; color: white; padding: 10px 14px; text-align: left; }");
            sb.AppendLine("    td { border: 1px solid #ddd; padding: 8px 14px; }");
            sb.AppendLine("    tr:nth-child(even) { background: #f9f9f9; }");
            sb.AppendLine("    .warning { color: #b86000; font-weight: bold; }");
            sb.AppendLine("    .critical { color: #c00000; font-weight: bold; background: #ffebeb; }");
            sb.AppendLine("    .ok { color: #008000; font-weight: bold; }");
            sb.AppendLine("    .badge { display: inline-block; padding: 4px 8px; border-radius: 4px; font-weight: bold; font-size: 0.9em; }");
            sb.AppendLine("    .badge-none { background: #e8f5e9; color: #2e7d32; }");
            sb.AppendLine("    .badge-minor { background: #fff8e1; color: #f57f17; }");
            sb.AppendLine("    .badge-major { background: #fff3e0; color: #e65100; }");
            sb.AppendLine("    .badge-critical { background: #ffebee; color: #c62828; }");
            sb.AppendLine("    .card { background: #fff; border: 1px solid #e0e0e0; border-radius: 6px; padding: 16px; margin: 12px 0; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }");
            sb.AppendLine("    @media print { .no-print { display: none; } body { margin: 0; } }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Section 1: Header
            sb.AppendLine("  <div class='card'>");
            sb.AppendLine("    <h1>IFC Alignment Manager — Diagnostics & Execution Report</h1>");
            sb.AppendLine($"   <p><strong>Generated:</strong> {timestamp} &nbsp;|&nbsp; " +
                          $"<strong>Revit Version:</strong> {AppVersion.RevitTarget} &nbsp;|&nbsp; " +
                          $"<strong>Add-in Version:</strong> {AppVersion.Version} ({AppVersion.BuildDate})</p>");
            string severity = diagnostics?.Severity ?? "Unknown";
            string badgeClass = $"badge-{severity.ToLowerInvariant()}";
            sb.AppendLine($"   <p><strong>Overall Severity Status:</strong> <span class='badge {badgeClass}'>{severity.ToUpperInvariant()}</span></p>");
            sb.AppendLine("  </div>");

            // Section 2 & 3: Host and Consultant Model Info
            sb.AppendLine("  <h2>1. Model Metadata</h2>");
            sb.AppendLine("  <table>");
            sb.AppendLine("    <tr><th style='width: 25%;'>Property</th><th style='width: 37.5%;'>Host Document</th><th style='width: 37.5%;'>Consultant Document</th></tr>");
            sb.AppendLine($"   <tr><td><strong>Title</strong></td><td>{host?.DocumentTitle ?? "—"}</td><td>{consultant?.DocumentTitle ?? "—"}</td></tr>");
            sb.AppendLine($"   <tr><td><strong>File Path</strong></td><td>{host?.DocumentPath ?? "—"}</td><td>{consultant?.DocumentPath ?? "—"}</td></tr>");
            sb.AppendLine($"   <tr><td><strong>Length Unit</strong></td><td>{host?.ProjectLengthUnit ?? "—"}</td><td>{consultant?.ProjectLengthUnit ?? "—"}</td></tr>");
            sb.AppendLine($"   <tr><td><strong>Snapshot Time</strong></td><td>{host?.SnapshotTime ?? "—"}</td><td>{consultant?.SnapshotTime ?? "—"}</td></tr>");
            sb.AppendLine("  </table>");

            // Section 4: Coordinate Comparison
            sb.AppendLine("  <h2>2. Coordinate Systems Comparison</h2>");
            sb.AppendLine("  <table>");
            sb.AppendLine("    <tr><th>Coordinate Metric (feet)</th><th>Host</th><th>Consultant</th><th>Delta (Consultant - Host)</th><th>Status</th></tr>");

            void AddRow(string label, double hVal, double cVal, double delta, bool isAngle = false)
            {
                double abs = Math.Abs(delta);
                string cssClass = abs > (isAngle ? 0.05 : 1000.0) ? "critical" : (abs > (isAngle ? 0.001 : 1.0) ? "warning" : "ok");
                string status = abs > (isAngle ? 0.05 : 1000.0) ? "Critical Mismatch" : (abs > (isAngle ? 0.001 : 1.0) ? "Offset" : "Aligned");
                string fmt = isAngle ? "F5" : "F4";
                sb.AppendLine($"    <tr class='{cssClass}'><td><strong>{label}</strong></td><td>{hVal.ToString(fmt)}</td><td>{cVal.ToString(fmt)}</td><td>{delta.ToString(fmt)}</td><td>{status}</td></tr>");
            }

            if (diagnostics != null && host != null && consultant != null)
            {
                AddRow("Project Base Point X", host.PBP_X, consultant.PBP_X, diagnostics.Delta_PBP_X);
                AddRow("Project Base Point Y", host.PBP_Y, consultant.PBP_Y, diagnostics.Delta_PBP_Y);
                AddRow("Project Base Point Z", host.PBP_Z, consultant.PBP_Z, diagnostics.Delta_PBP_Z);
                AddRow("Shared Origin East-West", host.SharedOrigin_EastWest, consultant.SharedOrigin_EastWest, diagnostics.Delta_SharedOrigin_EW);
                AddRow("Shared Origin North-South", host.SharedOrigin_NorthSouth, consultant.SharedOrigin_NorthSouth, diagnostics.Delta_SharedOrigin_NS);
                AddRow("True North (radians)", host.TrueNorthAngle_Radians, consultant.TrueNorthAngle_Radians, diagnostics.Delta_TrueNorth_Radians, true);
            }
            sb.AppendLine("  </table>");

            // Section 5: Warnings & Discrepancies
            sb.AppendLine("  <h2>3. Detected Discrepancies & Warnings</h2>");
            var allWarnings = new List<string>();
            if (diagnostics?.Warnings != null) allWarnings.AddRange(diagnostics.Warnings);
            if (warnings != null) allWarnings.AddRange(warnings);

            if (allWarnings.Count > 0)
            {
                sb.AppendLine("  <ul>");
                foreach (var w in allWarnings)
                {
                    string color = w.IndexOf("CRITICAL", StringComparison.OrdinalIgnoreCase) >= 0 ? "critical" : (w.IndexOf("MAJOR", StringComparison.OrdinalIgnoreCase) >= 0 ? "warning" : "");
                    sb.AppendLine($"    <li class='{color}'>{w}</li>");
                }
                sb.AppendLine("  </ul>");
            }
            else
            {
                sb.AppendLine("  <p class='ok'>No coordinate discrepancies detected.</p>");
            }

            // Section 6: Proposed Transformation
            if (proposedTransform != null)
            {
                sb.AppendLine("  <h2>4. Calculated Alignment Transformation</h2>");
                sb.AppendLine("  <table>");
                const double FT_TO_M = 0.3048;
                sb.AppendLine("    <tr><th>Parameter</th><th>Value (feet)</th><th>Value (meters)</th><th>Unit</th></tr>");
                sb.AppendLine($"   <tr><td>Alignment Method</td><td colspan='2'>{proposedTransform.Method}</td><td>—</td></tr>");
                sb.AppendLine($"   <tr><td>Translation X</td><td>{proposedTransform.Translation_X:F4}</td><td>{(proposedTransform.Translation_X * FT_TO_M):F4}</td><td>ft / m</td></tr>");
                sb.AppendLine($"   <tr><td>Translation Y</td><td>{proposedTransform.Translation_Y:F4}</td><td>{(proposedTransform.Translation_Y * FT_TO_M):F4}</td><td>ft / m</td></tr>");
                sb.AppendLine($"   <tr><td>Translation Z</td><td>{proposedTransform.Translation_Z:F4}</td><td>{(proposedTransform.Translation_Z * FT_TO_M):F4}</td><td>ft / m</td></tr>");
                double deg = proposedTransform.Rotation_Radians * (180.0 / Math.PI);
                sb.AppendLine($"   <tr><td>Rotation Angle</td><td colspan='2'>{proposedTransform.Rotation_Radians:F5} rad&nbsp;&nbsp;({deg:F4}°)</td><td>radians / degrees</td></tr>");
                sb.AppendLine($"   <tr><td>Rotation Pivot (X, Y, Z)</td><td>({proposedTransform.RotationPivot_X:F4}, {proposedTransform.RotationPivot_Y:F4}, {proposedTransform.RotationPivot_Z:F4})</td><td>({(proposedTransform.RotationPivot_X * FT_TO_M):F4}, {(proposedTransform.RotationPivot_Y * FT_TO_M):F4}, {(proposedTransform.RotationPivot_Z * FT_TO_M):F4})</td><td>ft / m</td></tr>");
                sb.AppendLine($"   <tr><td>Estimated Residual Error</td><td>{proposedTransform.EstimatedResidualError_Feet:F5}</td><td>{(proposedTransform.EstimatedResidualError_Feet * FT_TO_M):F5}</td><td>ft / m</td></tr>");
                sb.AppendLine("  </table>");
            }

            // Section 7: Applied Transformation Result
            if (appliedResult != null)
            {
                sb.AppendLine("  <h2>5. Transformation Execution Result</h2>");
                sb.AppendLine("  <div class='card'>");
                string statusText = appliedResult.Success ? "<span class='ok'>SUCCESSFULLY APPLIED</span>" : $"<span class='critical'>FAILED: {appliedResult.ErrorMessage}</span>";
                sb.AppendLine($"   <p><strong>Status:</strong> {statusText}</p>");
                sb.AppendLine($"   <p><strong>Target Document:</strong> {appliedResult.DocumentTitle}</p>");
                sb.AppendLine($"   <p><strong>Method:</strong> {appliedResult.Method}</p>");
                sb.AppendLine($"   <p><strong>Executed At:</strong> {appliedResult.Timestamp}</p>");
                sb.AppendLine($"   <p><strong>Post-Alignment Residual Error:</strong> {appliedResult.ResidualError_Feet:F5} ft</p>");
                sb.AppendLine("  </div>");
            }

            // Section 9: Footer
            sb.AppendLine("  <hr style='margin-top: 3em; border: 0; border-top: 1px solid #ccc;'>");
            sb.AppendLine("  <p style='color: #777; font-size: 0.85em;'>IFC Alignment Manager — Revit 2026 Macro / Add-in Suite | Report compiled automatically.</p>");

            // Print footer
            sb.AppendLine("  <div class='card no-print' style='margin-top:2em; border-top: 1px solid #ccc; padding-top: 1em;'>");
            sb.AppendLine("    <button onclick='window.print()' style='padding:8px 18px; background:#005a9c; color:white; border:none; border-radius:4px; cursor:pointer; font-size:0.95em;'>Print / Save as PDF</button>");
            sb.AppendLine("    &nbsp;");
            sb.AppendLine("    <button onclick='copyResidualToClipboard()' style='padding:8px 18px; background:#444; color:white; border:none; border-radius:4px; cursor:pointer; font-size:0.95em;'>Copy Residual Error</button>");
            sb.AppendLine("    <span id='copiedMsg' style='margin-left:12px; color:green; display:none;'>Copied!</span>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <script>");
            sb.AppendLine("    function copyResidualToClipboard() {");
            sb.AppendLine($"      navigator.clipboard.writeText('{(appliedResult != null ? appliedResult.ResidualError_Feet.ToString("F5", System.Globalization.CultureInfo.InvariantCulture) : "0.00000")} ft').then(function() {{");
            sb.AppendLine("        var msg = document.getElementById('copiedMsg');");
            sb.AppendLine("        msg.style.display = 'inline';");
            sb.AppendLine("        setTimeout(function() {{ msg.style.display = 'none'; }}, 2000);");
            sb.AppendLine("      });");
            sb.AppendLine("    }");
            sb.AppendLine("  </script>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        public bool WriteAndOpen(string htmlContent, string outputFolder, out string filePath)
        {
            filePath = null;
            try
            {
                if (string.IsNullOrEmpty(outputFolder))
                {
                    outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IFCAlignmentManager", "Reports");
                }

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                string fileName = $"AlignmentReport_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                filePath = Path.Combine(outputFolder, fileName);
                File.WriteAllText(filePath, htmlContent, Encoding.UTF8);

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write or open HTML report: {ex.Message}");
                return false;
            }
        }
    }
}
