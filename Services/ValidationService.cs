using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using IFCAlignmentManager.Core;

namespace IFCAlignmentManager.Services
{
    public class ValidationReport
    {
        public bool Passed { get; set; }
        public double ResidualError_Feet { get; set; }
        public CoordinateSnapshot SnapshotAfterAlignment { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
    }

    public class ValidationService
    {
        // Pre-flight check before attempting alignment.
        // Returns list of blocking errors (empty = safe to proceed).
        public List<string> ValidatePreAlignment(Document hostDoc, Document consultantDoc)
        {
            var errors = new List<string>();

            if (hostDoc == null)
            {
                errors.Add("Host document is null.");
                return errors;
            }

            if (consultantDoc == null)
            {
                errors.Add("Consultant document is null.");
                return errors;
            }

            if (hostDoc.Equals(consultantDoc) || hostDoc.PathName == consultantDoc.PathName && !string.IsNullOrEmpty(hostDoc.PathName))
            {
                errors.Add("Host and Consultant documents cannot be the same document.");
            }

            // Block only if a transaction is already open on the consultant doc
            // (IsModifiable is unreliable on workshared models — use IsReadOnly instead)
            if (consultantDoc.IsReadOnly)
            {
                errors.Add("Consultant document is open Read-Only and cannot be modified.");
            }
            else
            {
                // Check for an active transaction without starting one
                // TransactionStatus.Started means another operation owns the doc right now
                try
                {
                    using (Transaction probe = new Transaction(consultantDoc, "_probe"))
                    {
                        TransactionStatus status = probe.Start();
                        if (status == TransactionStatus.Started)
                            probe.RollBack(); // immediately release — we only needed to test
                        else
                            errors.Add("Consultant document cannot accept a transaction right now. Check worksharing checkout status.");
                    }
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                {
                    errors.Add("Consultant document is locked by an active transaction. Commit or cancel it before aligning.");
                }
            }

            if (hostDoc.IsReadOnly)
            {
                errors.Add("Host document is open Read-Only. Coordinate reading requires at least a non-locked document.");
            }

            return errors;
        }

        // Post-alignment check.
        // Re-reads consultant coordinates and compares to expected.
        public ValidationReport ValidatePostAlignment(
            Document consultantDoc,
            CoordinateSnapshot snapshotBeforeAlignment,
            AlignmentTransformResult appliedTransform,
            double toleranceFeet)
        {
            var report = new ValidationReport();

            if (consultantDoc == null || appliedTransform == null)
            {
                report.Passed = false;
                report.Issues.Add("Document or transform is null.");
                return report;
            }

            var diag = new CoordinateDiagnosticsService();
            report.SnapshotAfterAlignment = diag.ReadSnapshot(consultantDoc);

            // Compute expected ProjectPosition after transformation
            double tol = toleranceFeet > 0 ? toleranceFeet : 0.01;

            if (snapshotBeforeAlignment != null)
            {
                double expectedEW = snapshotBeforeAlignment.SharedOrigin_EastWest - appliedTransform.Translation_X;
                double expectedNS = snapshotBeforeAlignment.SharedOrigin_NorthSouth - appliedTransform.Translation_Y;
                double expectedAngle = snapshotBeforeAlignment.TrueNorthAngle_Radians - appliedTransform.Rotation_Radians;

                double diffEW = Math.Abs(report.SnapshotAfterAlignment.SharedOrigin_EastWest - expectedEW);
                double diffNS = Math.Abs(report.SnapshotAfterAlignment.SharedOrigin_NorthSouth - expectedNS);
                double diffAngle = Math.Abs(report.SnapshotAfterAlignment.TrueNorthAngle_Radians - expectedAngle);

                report.ResidualError_Feet = Math.Sqrt(diffEW * diffEW + diffNS * diffNS);

                if (report.ResidualError_Feet > tol)
                {
                    report.Issues.Add($"Residual linear position offset ({report.ResidualError_Feet:F4} ft) exceeds tolerance ({tol:F4} ft).");
                }

                if (diffAngle > 0.001)
                {
                    report.Issues.Add($"Residual True North rotation offset ({diffAngle:F5} rad) exceeds tolerance.");
                }

                report.Passed = report.Issues.Count == 0;
            }
            else
            {
                report.Passed = true;
                report.ResidualError_Feet = 0;
            }

            return report;
        }
    }
}
