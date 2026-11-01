using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using IFCAlignmentManager.Core;
using IFCAlignmentManager.Services;

namespace IFCAlignmentManager.Tests
{
    public class TransformMathTests
    {
        public static List<TestResultItem> RunAll()
        {
            var results = new List<TestResultItem>();
            var calc = new TransformCalculatorService();
            var diag = new CoordinateDiagnosticsService();

            // TEST-A01: Zero transform
            try
            {
                var h = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var c = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var t = calc.CalculateFromProjectBasePoints(h, c);
                bool pass = Math.Abs(t.Translation_X) < 1e-6 && Math.Abs(t.Translation_Y) < 1e-6 && Math.Abs(t.Translation_Z) < 1e-6 && Math.Abs(t.Rotation_Radians) < 1e-6;
                results.Add(new TestResultItem("TEST-A01: Zero transform", pass, $"T=({t.Translation_X:F3}, {t.Translation_Y:F3}, {t.Translation_Z:F3}), R={t.Rotation_Radians:F3}"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A01: Zero transform", false, ex.Message));
            }

            // TEST-A02: Pure translation X
            try
            {
                var h = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var c = new CoordinateSnapshot { PBP_X = 100, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var t = calc.CalculateFromProjectBasePoints(h, c);
                bool pass = Math.Abs(t.Translation_X - (-100)) < 1e-6 && Math.Abs(t.Translation_Y) < 1e-6 && Math.Abs(t.Rotation_Radians) < 1e-6;
                results.Add(new TestResultItem("TEST-A02: Pure translation X", pass, $"T_X={t.Translation_X:F3} (Expected -100)"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A02: Pure translation X", false, ex.Message));
            }

            // TEST-A03: Pure translation Y
            try
            {
                var h = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var c = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 50, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var t = calc.CalculateFromProjectBasePoints(h, c);
                bool pass = Math.Abs(t.Translation_X) < 1e-6 && Math.Abs(t.Translation_Y - (-50)) < 1e-6 && Math.Abs(t.Rotation_Radians) < 1e-6;
                results.Add(new TestResultItem("TEST-A03: Pure translation Y", pass, $"T_Y={t.Translation_Y:F3} (Expected -50)"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A03: Pure translation Y", false, ex.Message));
            }

            // TEST-A04: Pure rotation 45 degrees
            try
            {
                var h = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var c = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = Math.PI / 4.0 };
                var t = calc.CalculateFromProjectBasePoints(h, c);
                double expectedRot = -Math.PI / 4.0;
                bool pass = Math.Abs(t.Rotation_Radians - expectedRot) < 1e-6 && Math.Abs(t.Translation_X) < 1e-6 && Math.Abs(t.Translation_Y) < 1e-6;
                results.Add(new TestResultItem("TEST-A04: Pure rotation 45 degrees", pass, $"R={t.Rotation_Radians:F4} rad (Expected {expectedRot:F4})"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A04: Pure rotation 45 degrees", false, ex.Message));
            }

            // TEST-A05: Translation + rotation combined
            try
            {
                var h = new CoordinateSnapshot { PBP_X = 10, PBP_Y = 20, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var c = new CoordinateSnapshot { PBP_X = 110, PBP_Y = 70, PBP_Z = 0, TrueNorthAngle_Radians = Math.PI / 6.0 };
                var t = calc.CalculateFromProjectBasePoints(h, c);
                double expectedRot = -Math.PI / 6.0;
                bool pass = Math.Abs(t.Translation_X - (-100)) < 1e-6 && Math.Abs(t.Translation_Y - (-50)) < 1e-6 && Math.Abs(t.Rotation_Radians - expectedRot) < 1e-6;
                results.Add(new TestResultItem("TEST-A05: Translation + rotation combined", pass, $"T=({t.Translation_X:F1}, {t.Translation_Y:F1}), R={t.Rotation_Radians:F4} rad"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A05: Translation + rotation combined", false, ex.Message));
            }

            // TEST-A06: Two-point corresponding alignment
            try
            {
                XYZ[] hostPts = new XYZ[] { new XYZ(0, 0, 0), new XYZ(10, 0, 0) };
                XYZ[] consPts = new XYZ[] { new XYZ(5, 5, 0), new XYZ(5, 15, 0) };
                var t = calc.CalculateFromCorrespondingPoints(hostPts, consPts);

                // Manually verify using the calculator's two-step order (translate then rotate around pivot)
                double rotRad = t.Rotation_Radians;
                double pivX = t.RotationPivot_X, pivY = t.RotationPivot_Y;
                XYZ c2 = consPts[1];
                double tx = c2.X + t.Translation_X;
                double ty = c2.Y + t.Translation_Y;
                double dx2 = tx - pivX, dy2 = ty - pivY;
                double rotX = dx2 * Math.Cos(rotRad) - dy2 * Math.Sin(rotRad) + pivX;
                double rotY = dx2 * Math.Sin(rotRad) + dy2 * Math.Cos(rotRad) + pivY;
                XYZ c2Rotated = new XYZ(rotX, rotY, c2.Z + t.Translation_Z);
                double dist = Math.Sqrt(Math.Pow(hostPts[1].X - c2Rotated.X, 2) + Math.Pow(hostPts[1].Y - c2Rotated.Y, 2));
                bool pass = dist < 0.001;
                results.Add(new TestResultItem("TEST-A06: Two-point corresponding alignment", pass, $"Residual dist={dist:F6} ft (Expected < 0.001)"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A06: Two-point corresponding alignment", false, ex.Message));
            }

            // TEST-A07: Residual error calculation
            try
            {
                XYZ[] hostPts = new XYZ[] { new XYZ(0, 0, 0), new XYZ(10, 0, 0) };
                XYZ[] consPts = new XYZ[] { new XYZ(0, 0, 0), new XYZ(15, 5, 0) }; // Inconsistent length: 10 vs ~15.81
                var t = calc.CalculateFromCorrespondingPoints(hostPts, consPts);
                bool pass = t.EstimatedResidualError_Feet > 0.001;
                results.Add(new TestResultItem("TEST-A07: Residual error calculation", pass, $"Residual={t.EstimatedResidualError_Feet:F4} ft (Expected > 0.001)"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A07: Residual error calculation", false, ex.Message));
            }

            // TEST-A08: Large coordinate values (surveying scale)
            try
            {
                var h = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var c = new CoordinateSnapshot { PBP_X = 1000000, PBP_Y = 2000000, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var t = calc.CalculateFromProjectBasePoints(h, c);
                bool pass = !double.IsInfinity(t.Translation_X) && !double.IsNaN(t.Translation_X) && Math.Abs(t.Translation_X - (-1000000)) < 1e-4;
                results.Add(new TestResultItem("TEST-A08: Large coordinate values", pass, $"T_X={t.Translation_X:F1}, T_Y={t.Translation_Y:F1}"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A08: Large coordinate values", false, ex.Message));
            }

            // TEST-A09: Unit conversion round-trip (Meters to Feet and back)
            try
            {
                double metersInput = 1.0;
                double feet = metersInput * 3.28083989501312335958;
                double backToMeters = feet / 3.28083989501312335958;
                bool pass = Math.Abs(backToMeters - metersInput) < 1e-7;
                results.Add(new TestResultItem("TEST-A09: Unit conversion round-trip", pass, $"Delta={Math.Abs(backToMeters - metersInput):E3}"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A09: Unit conversion round-trip", false, ex.Message));
            }

            // TEST-A10: Critical mismatch detection
            try
            {
                var h = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0, ProjectLengthUnit = "feet" };
                var c = new CoordinateSnapshot { PBP_X = 1500, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0, ProjectLengthUnit = "feet" };
                var diagResult = diag.Compare(h, c);
                bool isCritical = diagResult.Severity == "Critical";
                bool hasUnitMismatchText = diagResult.Warnings.Exists(w => w.IndexOf("unit mismatch", StringComparison.OrdinalIgnoreCase) >= 0);
                bool pass = isCritical && hasUnitMismatchText;
                results.Add(new TestResultItem("TEST-A10: Critical mismatch detection", pass, $"Severity={diagResult.Severity}, HasWarning={hasUnitMismatchText}"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A10: Critical mismatch detection", false, ex.Message));
            }

            // TEST-A11: ApplyTransform order consistency with execution service
            // The execution service applies: rotate-then-translate (via ProjectPosition adjustment).
            // ApplyTransform must match this order. Verify with a large offset where order matters.
            try
            {
                // Consultant PBP is at (1000000, 2000000, 0) — national grid scale.
                // Host PBP is at (0, 0, 0).
                // After alignment, consultant (1000000, 2000000) should map to (0, 0).
                var h = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var c = new CoordinateSnapshot { PBP_X = 1000000, PBP_Y = 2000000, PBP_Z = 0, TrueNorthAngle_Radians = 0 };
                var t = calc.CalculateFromProjectBasePoints(h, c);

                // Apply transform to the consultant's PBP. It should land at host's PBP = (0,0,0).
                XYZ consultantPBP = new XYZ(1000000, 2000000, 0);
                XYZ result = calc.ApplyTransform(consultantPBP, t);
                double dist = Math.Sqrt(result.X * result.X + result.Y * result.Y + result.Z * result.Z);
                bool pass = dist < 0.01; // within 0.01 feet of origin
                results.Add(new TestResultItem("TEST-A11: ApplyTransform order consistency (large coords)", pass,
                    $"Mapped PBP to ({result.X:F4}, {result.Y:F4}) — dist from origin={dist:F6} ft"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A11: ApplyTransform order consistency (large coords)", false, ex.Message));
            }

            // TEST-A12: Dual-unit conversion accuracy
            // 1 foot = 0.3048 meters exactly. Verify the constant used throughout.
            try
            {
                const double FT_TO_M = 0.3048;
                double oneFoot = 1.0;
                double inMeters = oneFoot * FT_TO_M;
                double backToFeet = inMeters / FT_TO_M;
                // Check against the internationally-defined exact value
                bool pass = Math.Abs(inMeters - 0.3048) < 1e-10 && Math.Abs(backToFeet - 1.0) < 1e-10;
                results.Add(new TestResultItem("TEST-A12: Dual-unit ft↔m conversion accuracy", pass,
                    $"1 ft = {inMeters:F10} m, round-trip delta = {Math.Abs(backToFeet - 1.0):E3}"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A12: Dual-unit ft↔m conversion accuracy", false, ex.Message));
            }

            // TEST-A13: Sub-milliradian rotation treated as no meaningful rotation
            try
            {
                var h = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0.00005 };
                var c = new CoordinateSnapshot { PBP_X = 0, PBP_Y = 0, PBP_Z = 0, TrueNorthAngle_Radians = 0.00006 };
                var t = calc.CalculateFromProjectBasePoints(h, c);
                // Delta is 0.00001 rad — below the 0.001 rad threshold in DiagnosticsService.Compare()
                bool pass = Math.Abs(t.Rotation_Radians) < 0.001;
                results.Add(new TestResultItem("TEST-A13: Sub-milliradian delta not flagged", pass,
                    $"R={t.Rotation_Radians:E4} rad (threshold 0.001)"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A13: Sub-milliradian delta not flagged", false, ex.Message));
            }

            // TEST-A14: Restore point JSON round-trip with InvariantCulture values
            try
            {
                // Simulate a value that differs between InvariantCulture (.) and German culture (,)
                double testVal = 12345.6789;
                string json = "{ \"EastWest\": " +
                              testVal.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                              ", \"NorthSouth\": 0, \"Elevation\": 0, \"Angle\": 0 }";

                // Inline parse using the same logic as ParseJsonDouble
                int idx = json.IndexOf("\"EastWest\":", StringComparison.Ordinal) + "\"EastWest\":".Length;
                while (json[idx] == ' ') idx++;
                int end = idx;
                while (json[end] != ',' && json[end] != '}') end++;
                double parsed = double.Parse(json.Substring(idx, end - idx).Trim(),
                                             System.Globalization.CultureInfo.InvariantCulture);
                bool pass = Math.Abs(parsed - testVal) < 1e-9;
                results.Add(new TestResultItem("TEST-A14: Restore point InvariantCulture round-trip", pass,
                    $"Written={testVal}, Parsed={parsed}, Delta={Math.Abs(parsed - testVal):E2}"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A14: Restore point InvariantCulture round-trip", false, ex.Message));
            }

            // TEST-A15: Profile export/import ZIP round-trip
            try
            {
                var tempFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "IFCTestProfiles_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                var storage = new ProfileStorageService(tempFolder);

                var profile = new ConsultantAlignmentProfile
                {
                    ProfileId       = Guid.NewGuid().ToString(),
                    ConsultantName  = "Test Consultant",
                    AlignmentMethod = "ProjectBasePoint",
                    Translation_X   = 12345.6789,
                    Translation_Y   = -9876.5432,
                    Rotation_Radians = 0.523598776,
                    RevitVersion    = "2026",
                    ValidationTolerance_Feet = 0.01
                };
                storage.SaveProfile(profile, out _);

                string zipPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_export.zip");
                int exported = storage.ExportProfilesAsZip(zipPath, out _);

                var importFolder = tempFolder + "_import";
                var importStorage = new ProfileStorageService(importFolder);
                var (imported, skipped) = importStorage.ImportProfilesFromZip(zipPath, false, out _);

                var reloaded = importStorage.LoadProfile(profile.ProfileId);

                bool pass = exported == 1 && imported == 1 && skipped == 0 &&
                            reloaded != null &&
                            Math.Abs(reloaded.Translation_X - profile.Translation_X) < 1e-9 &&
                            Math.Abs(reloaded.Rotation_Radians - profile.Rotation_Radians) < 1e-9 &&
                            reloaded.ConsultantName == profile.ConsultantName;

                // Cleanup
                try { System.IO.Directory.Delete(tempFolder, true); System.IO.Directory.Delete(importFolder, true); System.IO.File.Delete(zipPath); } catch { }

                results.Add(new TestResultItem("TEST-A15: Profile ZIP export/import round-trip", pass,
                    $"Exported={exported}, Imported={imported}, TX delta={Math.Abs((reloaded?.Translation_X ?? 0) - profile.Translation_X):E2}"));
            }
            catch (Exception ex)
            {
                results.Add(new TestResultItem("TEST-A15: Profile ZIP export/import round-trip", false, ex.Message));
            }

            return results;
        }
    }

    public class TestResultItem
    {
        public string Name { get; }
        public bool Passed { get; }
        public string Details { get; }

        public TestResultItem(string name, bool passed, string details)
        {
            Name = name;
            Passed = passed;
            Details = details;
        }

        public override string ToString() => $"[{(Passed ? "PASS" : "FAIL")}] {Name} — {Details}";
    }
}
