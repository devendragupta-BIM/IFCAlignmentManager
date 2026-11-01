using System;
using System.Runtime.Serialization;

namespace IFCAlignmentManager.Core
{
    [DataContract]
    public class CoordinateSnapshot
    {
        [DataMember] public string DocumentTitle { get; set; }
        [DataMember] public string DocumentPath { get; set; }

        // Project Base Point — in decimal feet (internal units)
        [DataMember] public double PBP_X { get; set; }
        [DataMember] public double PBP_Y { get; set; }
        [DataMember] public double PBP_Z { get; set; }

        // Survey Point — in decimal feet (internal units)
        [DataMember] public double SP_X { get; set; }
        [DataMember] public double SP_Y { get; set; }
        [DataMember] public double SP_Z { get; set; }

        // Shared coordinate origin (from ProjectPosition at internal origin)
        [DataMember] public double SharedOrigin_EastWest { get; set; }
        [DataMember] public double SharedOrigin_NorthSouth { get; set; }
        [DataMember] public double SharedOrigin_Elevation { get; set; }

        // True North angle in radians (from ProjectPosition.Angle)
        [DataMember] public double TrueNorthAngle_Radians { get; set; }

        // Project units display string e.g. "Meters", "Feet"
        [DataMember] public string ProjectLengthUnit { get; set; }

        // Model bounding box in internal units
        [DataMember] public double BBox_MinX { get; set; }
        [DataMember] public double BBox_MinY { get; set; }
        [DataMember] public double BBox_MinZ { get; set; }
        [DataMember] public double BBox_MaxX { get; set; }
        [DataMember] public double BBox_MaxY { get; set; }
        [DataMember] public double BBox_MaxZ { get; set; }

        // Timestamp of snapshot
        [DataMember] public string SnapshotTime { get; set; }
    }

    /// <summary>
    /// Lightweight record of a saved restore point — passed to the UI so it can
    /// offer the user a restore/undo button with a meaningful label.
    /// </summary>
    public class AlignmentRestorePoint
    {
        /// <summary>Full path to the JSON file on disk.</summary>
        public string FilePath { get; set; }

        /// <summary>Human-readable timestamp of when the restore point was captured.</summary>
        public string CapturedAt { get; set; }

        /// <summary>Document title at time of capture.</summary>
        public string DocumentTitle { get; set; }

        /// <summary>True if the file still exists on disk (user may have deleted it).</summary>
        public bool IsValid => !string.IsNullOrEmpty(FilePath) && System.IO.File.Exists(FilePath);
    }
}
