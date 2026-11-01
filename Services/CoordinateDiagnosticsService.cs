using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using IFCAlignmentManager.Core;

namespace IFCAlignmentManager.Services
{
    public class CoordinateDiagnosticsService
    {
        public CoordinateSnapshot ReadSnapshot(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var snap = new CoordinateSnapshot
            {
                DocumentTitle = doc.Title ?? "Untitled",
                DocumentPath = doc.PathName ?? string.Empty,
                SnapshotTime = DateTime.Now.ToString("o"),
                PBP_X = double.NaN,
                PBP_Y = double.NaN,
                PBP_Z = double.NaN,
                SP_X = double.NaN,
                SP_Y = double.NaN,
                SP_Z = double.NaN,
                ProjectLengthUnit = "Unknown"
            };

            try
            {
                // Read Base Points
                var bpCollector = new FilteredElementCollector(doc).OfClass(typeof(BasePoint));
                foreach (BasePoint bp in bpCollector)
                {
                    if (bp == null) continue;
                    if (!bp.IsShared)
                    {
                        // Project Base Point
                        XYZ pos = bp.Position;
                        if (pos != null)
                        {
                            snap.PBP_X = pos.X;
                            snap.PBP_Y = pos.Y;
                            snap.PBP_Z = pos.Z;
                        }
                        else
                        {
                            var pEw = bp.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM);
                            var pNs = bp.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM);
                            var pEl = bp.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM);
                            if (pEw != null) snap.PBP_X = pEw.AsDouble();
                            if (pNs != null) snap.PBP_Y = pNs.AsDouble();
                            if (pEl != null) snap.PBP_Z = pEl.AsDouble();
                        }
                    }
                    else
                    {
                        // Survey Point
                        XYZ pos = bp.Position;
                        if (pos != null)
                        {
                            snap.SP_X = pos.X;
                            snap.SP_Y = pos.Y;
                            snap.SP_Z = pos.Z;
                        }
                        else
                        {
                            var pEw = bp.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM);
                            var pNs = bp.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM);
                            var pEl = bp.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM);
                            if (pEw != null) snap.SP_X = pEw.AsDouble();
                            if (pNs != null) snap.SP_Y = pNs.AsDouble();
                            if (pEl != null) snap.SP_Z = pEl.AsDouble();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read BasePoints: {ex.Message}");
            }

            // Fallback if BasePoint was not found
            if (double.IsNaN(snap.PBP_X)) { snap.PBP_X = 0; snap.PBP_Y = 0; snap.PBP_Z = 0; }
            if (double.IsNaN(snap.SP_X)) { snap.SP_X = 0; snap.SP_Y = 0; snap.SP_Z = 0; }

            try
            {
                // Active Project Location & Shared Origin
                ProjectLocation loc = doc.ActiveProjectLocation;
                if (loc != null)
                {
                    ProjectPosition pos = loc.GetProjectPosition(XYZ.Zero);
                    if (pos != null)
                    {
                        snap.TrueNorthAngle_Radians = pos.Angle;
                        snap.SharedOrigin_EastWest = pos.EastWest;
                        snap.SharedOrigin_NorthSouth = pos.NorthSouth;
                        snap.SharedOrigin_Elevation = pos.Elevation;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read ProjectLocation: {ex.Message}");
            }

            try
            {
                // Length Unit
                Units units = doc.GetUnits();
                FormatOptions fo = units.GetFormatOptions(SpecTypeId.Length);
                snap.ProjectLengthUnit = fo.GetUnitTypeId().TypeId;
            }
            catch
            {
                snap.ProjectLengthUnit = "feet (default)";
            }

            try
            {
                // Bounding box
                var elements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .ToElements();

                double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
                bool found = false;

                foreach (Element e in elements)
                {
                    if (e.Category == null) continue;
                    BoundingBoxXYZ bb = e.get_BoundingBox(null);
                    if (bb == null) continue;

                    minX = Math.Min(minX, bb.Min.X);
                    minY = Math.Min(minY, bb.Min.Y);
                    minZ = Math.Min(minZ, bb.Min.Z);
                    maxX = Math.Max(maxX, bb.Max.X);
                    maxY = Math.Max(maxY, bb.Max.Y);
                    maxZ = Math.Max(maxZ, bb.Max.Z);
                    found = true;
                }

                if (found && minX < double.MaxValue)
                {
                    snap.BBox_MinX = minX; snap.BBox_MinY = minY; snap.BBox_MinZ = minZ;
                    snap.BBox_MaxX = maxX; snap.BBox_MaxY = maxY; snap.BBox_MaxZ = maxZ;
                }
                else
                {
                    snap.BBox_MinX = 0; snap.BBox_MinY = 0; snap.BBox_MinZ = 0;
                    snap.BBox_MaxX = 0; snap.BBox_MaxY = 0; snap.BBox_MaxZ = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read BoundingBox: {ex.Message}");
            }

            return snap;
        }

        public DiagnosticsResult Compare(CoordinateSnapshot host, CoordinateSnapshot consultant)
        {
            var result = new DiagnosticsResult
            {
                HostSnapshot = host,
                ConsultantSnapshot = consultant,
                Warnings = new List<string>()
            };

            if (host == null || consultant == null)
            {
                result.Severity = "Critical";
                result.Warnings.Add("One of the snapshots is null. Cannot perform comparison.");
                return result;
            }

            result.Delta_PBP_X = consultant.PBP_X - host.PBP_X;
            result.Delta_PBP_Y = consultant.PBP_Y - host.PBP_Y;
            result.Delta_PBP_Z = consultant.PBP_Z - host.PBP_Z;
            result.Delta_TrueNorth_Radians = consultant.TrueNorthAngle_Radians - host.TrueNorthAngle_Radians;
            result.Delta_SharedOrigin_EW = consultant.SharedOrigin_EastWest - host.SharedOrigin_EastWest;
            result.Delta_SharedOrigin_NS = consultant.SharedOrigin_NorthSouth - host.SharedOrigin_NorthSouth;

            double maxLinearDelta = Math.Max(
                Math.Max(Math.Abs(result.Delta_PBP_X), Math.Abs(result.Delta_PBP_Y)),
                Math.Max(
                    Math.Abs(result.Delta_PBP_Z),
                    Math.Max(Math.Abs(result.Delta_SharedOrigin_EW), Math.Abs(result.Delta_SharedOrigin_NS))
                )
            );

            double angleDelta = Math.Abs(result.Delta_TrueNorth_Radians);

            if (maxLinearDelta > 1000.0)
            {
                result.Severity = "Critical";
                result.Warnings.Add($"CRITICAL: Large coordinate deviation ({maxLinearDelta:F2} ft). Probable unit mismatch (e.g. IFC in millimeters imported into feet) or completely different coordinate frame.");
            }
            else if (maxLinearDelta > 1.0)
            {
                result.Severity = "Major";
                result.Warnings.Add($"MAJOR: Coordinate delta exceeds 1 foot ({maxLinearDelta:F2} ft). Models are noticeably offset.");
            }
            else if (maxLinearDelta >= 0.001 || angleDelta >= 0.0001)
            {
                result.Severity = "Minor";
                result.Warnings.Add($"MINOR: Small coordinate offset detected (Linear: {maxLinearDelta:F4} ft, Angle: {angleDelta:F5} rad). Alignment is recommended.");
            }
            else
            {
                result.Severity = "None";
            }

            if (!string.Equals(host.ProjectLengthUnit, consultant.ProjectLengthUnit, StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add($"Unit setting discrepancy: Host is '{host.ProjectLengthUnit}', Consultant is '{consultant.ProjectLengthUnit}'.");
                if (result.Severity == "None") result.Severity = "Minor";
            }

            if (angleDelta > 0.001)
            {
                double angleDeg = angleDelta * (180.0 / Math.PI);
                result.Warnings.Add($"True North orientation differs by {angleDeg:F2}° ({angleDelta:F4} rad).");
            }

            return result;
        }
    }
}
