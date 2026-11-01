using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using IFCAlignmentManager.Core;

namespace IFCAlignmentManager.Services
{
    public class AlignmentExecutionService
    {
        // Applies alignment by adjusting the consultant document's ProjectPosition.
        // This is the PREFERRED method — changes coordinate relationship, not geometry.
        // Returns true on success, false with error message on failure.
        public bool ApplySharedCoordinateAlignment(
            Document consultantDoc,
            AlignmentTransformResult transform,
            out string errorMessage)
        {
            errorMessage = null;
            if (consultantDoc == null)
            {
                errorMessage = "Consultant document is null.";
                return false;
            }
            if (transform == null)
            {
                errorMessage = "Transformation is null.";
                return false;
            }

            using (Transaction t = new Transaction(consultantDoc, "IFC Alignment - Shared Coordinates"))
            {
                try
                {
                    t.Start();

                    ProjectLocation loc = consultantDoc.ActiveProjectLocation;
                    if (loc == null)
                    {
                        t.RollBack();
                        errorMessage = "ActiveProjectLocation is null on consultant document.";
                        return false;
                    }

                    // Attempt to check out the project location element on workshared models
                    if (consultantDoc.IsWorkshared)
                    {
                        var locId = loc.Id;
                        var status = WorksharingUtils.GetCheckoutStatus(consultantDoc, locId);
                        if (status == CheckoutStatus.OwnedByOtherUser)
                        {
                            t.RollBack();
                            errorMessage = "Cannot check out the Project Location for editing. " +
                                           "Another user may own it. Synchronize with Central and try again.";
                            return false;
                        }

                        if (status == CheckoutStatus.NotOwned)
                        {
                            var checkoutResult = WorksharingUtils.CheckoutElements(consultantDoc, new[] { locId });
                            if (!checkoutResult.Contains(locId))
                            {
                                t.RollBack();
                                errorMessage = "Cannot check out the Project Location for editing. " +
                                               "Another user may own it. Synchronize with Central and try again.";
                                return false;
                            }
                        }
                    }

                    ProjectPosition currentPos = loc.GetProjectPosition(XYZ.Zero);

                    // Adjust position
                    ProjectPosition newPos = new ProjectPosition(
                        currentPos.EastWest   - transform.Translation_X,
                        currentPos.NorthSouth - transform.Translation_Y,
                        currentPos.Elevation  - transform.Translation_Z,
                        currentPos.Angle      - transform.Rotation_Radians
                    );

                    loc.SetProjectPosition(XYZ.Zero, newPos);

                    t.Commit();
                    return true;
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
                {
                    if (t.HasStarted() && !t.HasEnded()) t.RollBack();
                    errorMessage = $"Revit InvalidOperation: {ex.Message}. (Document may be workshared/locked or position is constrained).";
                    return false;
                }
                catch (Exception ex)
                {
                    if (t.HasStarted() && !t.HasEnded()) t.RollBack();
                    errorMessage = ex.Message;
                    return false;
                }
            }
        }

        // Fallback: physically moves all non-pinned elements if ProjectPosition method fails.
        public bool ApplyGeometryAlignment(
            Document consultantDoc,
            AlignmentTransformResult transform,
            out string errorMessage)
        {
            errorMessage = null;
            if (consultantDoc == null || transform == null)
            {
                errorMessage = "Document or transform is null.";
                return false;
            }

            using (Transaction t = new Transaction(consultantDoc, "IFC Alignment - Physical Geometry"))
            {
                try
                {
                    t.Start();

                    var collector = new FilteredElementCollector(consultantDoc)
                        .WhereElementIsNotElementType();

                    var elementIdsToMove = new List<ElementId>();
                    int totalElements = (int)collector.GetElementCount();
                    int processedCount = 0;

                    // Progress reporting fallback: ProgressHandle is not present in Revit 2026 DB namespace.
                    // Pump Windows message queue and allow cancellation/UI responsiveness every 500th element.
                    foreach (Element elem in collector)
                    {
                        processedCount++;
                        if (processedCount % 500 == 0)
                        {
                            System.Windows.Forms.Application.DoEvents();
                        }

                        if (elem == null || elem.Pinned || elem.ViewSpecific) continue;
                        if (elem.Category != null)
                        {
                            elementIdsToMove.Add(elem.Id);
                        }
                    }

                    if (elementIdsToMove.Count == 0)
                    {
                        t.RollBack();
                        errorMessage = "No movable non-pinned elements found in document.";
                        return false;
                    }

                    // Translation
                    XYZ moveVector = new XYZ(transform.Translation_X, transform.Translation_Y, transform.Translation_Z);
                    if (moveVector.GetLength() > 1e-6)
                    {
                        ElementTransformUtils.MoveElements(consultantDoc, elementIdsToMove, moveVector);
                    }

                    // Rotation
                    if (Math.Abs(transform.Rotation_Radians) > 1e-6)
                    {
                        XYZ p1 = new XYZ(transform.RotationPivot_X, transform.RotationPivot_Y, 0);
                        XYZ p2 = new XYZ(transform.RotationPivot_X, transform.RotationPivot_Y, 10);
                        Line axis = Line.CreateBound(p1, p2);
                        ElementTransformUtils.RotateElements(consultantDoc, elementIdsToMove, axis, transform.Rotation_Radians);
                    }

                    t.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    if (t.HasStarted() && !t.HasEnded()) t.RollBack();
                    errorMessage = ex.Message;
                    return false;
                }
            }
        }

        // Validates that the alignment was applied correctly by re-reading coordinates
        // and comparing against expected values within tolerance.
        public bool ValidateAlignment(
            Document consultantDoc,
            CoordinateSnapshot expectedResult,
            double toleranceFeet,
            out double actualResidual)
        {
            actualResidual = double.MaxValue;
            if (consultantDoc == null || expectedResult == null) return false;

            var diag = new CoordinateDiagnosticsService();
            CoordinateSnapshot actual = diag.ReadSnapshot(consultantDoc);

            double dx = actual.PBP_X - expectedResult.PBP_X;
            double dy = actual.PBP_Y - expectedResult.PBP_Y;
            double dz = actual.PBP_Z - expectedResult.PBP_Z;
            actualResidual = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            return actualResidual <= toleranceFeet;
        }

        // ─── RESTORE POINT (UNDO SUPPORT) ────────────────────────────────────────

        /// <summary>
        /// Captures the consultant document's current ProjectPosition to a JSON file
        /// before alignment is applied. Call this BEFORE ApplySharedCoordinateAlignment.
        /// Returns the path to the restore point file, or null on failure.
        /// </summary>
        public string SaveRestorePoint(Document consultantDoc, out string errorMessage)
        {
            errorMessage = null;
            if (consultantDoc == null)
            {
                errorMessage = "Document is null.";
                return null;
            }

            try
            {
                ProjectLocation loc = consultantDoc.ActiveProjectLocation;
                if (loc == null)
                {
                    errorMessage = "ActiveProjectLocation is null.";
                    return null;
                }

                ProjectPosition pos = loc.GetProjectPosition(XYZ.Zero);

                // Build a simple JSON manually — no NuGet, stays within constraint.
                string json = "{\n" +
                    $"  \"EastWest\":   {pos.EastWest.ToString(System.Globalization.CultureInfo.InvariantCulture)},\n" +
                    $"  \"NorthSouth\": {pos.NorthSouth.ToString(System.Globalization.CultureInfo.InvariantCulture)},\n" +
                    $"  \"Elevation\":  {pos.Elevation.ToString(System.Globalization.CultureInfo.InvariantCulture)},\n" +
                    $"  \"Angle\":      {pos.Angle.ToString(System.Globalization.CultureInfo.InvariantCulture)},\n" +
                    $"  \"DocumentTitle\": \"{EscapeJson(consultantDoc.Title)}\",\n" +
                    $"  \"CapturedAt\": \"{EscapeJson(DateTime.Now.ToString("o"))}\"\n" +
                    "}";

                string folder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "IFCAlignmentManager", "RestorePoints");

                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);

                // Name the file after the document title and timestamp so it's identifiable.
                string safeName = string.Concat((consultantDoc.Title ?? "doc").Split(System.IO.Path.GetInvalidFileNameChars()));
                string filePath = System.IO.Path.Combine(folder,
                    $"{safeName}_RestorePoint_{DateTime.Now:yyyyMMdd_HHmmss}.json");

                System.IO.File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
                return filePath;
            }
            catch (Exception ex)
            {
                errorMessage = $"SaveRestorePoint failed: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Reads a restore point JSON file and applies the original ProjectPosition
        /// back to the consultant document, effectively undoing the last alignment.
        /// </summary>
        public bool RestoreFromRestorePoint(Document consultantDoc, string restorePointFilePath, out string errorMessage)
        {
            errorMessage = null;
            if (consultantDoc == null)
            {
                errorMessage = "Document is null.";
                return false;
            }
            if (string.IsNullOrEmpty(restorePointFilePath) || !System.IO.File.Exists(restorePointFilePath))
            {
                errorMessage = "Restore point file not found.";
                return false;
            }

            try
            {
                string json = System.IO.File.ReadAllText(restorePointFilePath, System.Text.Encoding.UTF8);

                double eastWest   = ParseJsonDouble(json, "EastWest");
                double northSouth = ParseJsonDouble(json, "NorthSouth");
                double elevation  = ParseJsonDouble(json, "Elevation");
                double angle      = ParseJsonDouble(json, "Angle");

                using (Transaction t = new Transaction(consultantDoc, "IFC Alignment - Restore Original Position"))
                {
                    try
                    {
                        t.Start();

                        ProjectLocation loc = consultantDoc.ActiveProjectLocation;
                        if (loc == null)
                        {
                            t.RollBack();
                            errorMessage = "ActiveProjectLocation is null.";
                            return false;
                        }

                        ProjectPosition restored = new ProjectPosition(eastWest, northSouth, elevation, angle);
                        loc.SetProjectPosition(XYZ.Zero, restored);

                        t.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        if (t.HasStarted() && !t.HasEnded()) t.RollBack();
                        errorMessage = $"RestoreFromRestorePoint transaction failed: {ex.Message}";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"RestoreFromRestorePoint failed: {ex.Message}";
                return false;
            }
        }

        // ─── PRIVATE HELPERS FOR RESTORE POINT ───────────────────────────────────

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        /// <summary>
        /// Minimal JSON double parser. Reads the value of "key": valueHere from a flat JSON string.
        /// Does NOT handle arrays or nested objects — only used for the flat restore point file we write ourselves.
        /// </summary>
        private static double ParseJsonDouble(string json, string key)
        {
            string search = $"\"{key}\":";
            int idx = json.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0) throw new FormatException($"Key '{key}' not found in restore point JSON.");
            int start = idx + search.Length;
            // Skip whitespace
            while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
            // Read until comma, newline, or closing brace
            int end = start;
            while (end < json.Length && json[end] != ',' && json[end] != '\n' && json[end] != '}') end++;
            string raw = json.Substring(start, end - start).Trim();
            return double.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
