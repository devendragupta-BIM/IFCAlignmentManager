using System;
using System.Runtime.Serialization;

namespace IFCAlignmentManager.Core
{
    [DataContract]
    public class ConsultantAlignmentProfile
    {
        [DataMember] public string ProfileId { get; set; }          // GUID string
        [DataMember] public string ConsultantName { get; set; }
        [DataMember] public string OriginalModelFileName { get; set; }
        [DataMember] public string RevitVersion { get; set; }       // "2026"
        [DataMember] public string CreatedDate { get; set; }
        [DataMember] public string LastUsedDate { get; set; }

        // The alignment method used: "ProjectBasePoint", "SurveyPoint", "InternalOrigin", "ManualPoints"
        [DataMember] public string AlignmentMethod { get; set; }

        // Translation vector in decimal feet (internal units)
        [DataMember] public double Translation_X { get; set; }
        [DataMember] public double Translation_Y { get; set; }
        [DataMember] public double Translation_Z { get; set; }

        // Rotation in radians about Z axis
        [DataMember] public double Rotation_Radians { get; set; }

        // Pivot point for rotation in decimal feet
        [DataMember] public double RotationPivot_X { get; set; }
        [DataMember] public double RotationPivot_Y { get; set; }
        [DataMember] public double RotationPivot_Z { get; set; }

        // Snapshot of the consultant model's coordinate state AT TIME OF PROFILE CREATION
        // Used to validate that a new IFC revision has the same starting coordinates
        [DataMember] public CoordinateSnapshot BaselineSnapshot { get; set; }

        // Validation tolerance in decimal feet
        [DataMember] public double ValidationTolerance_Feet { get; set; }

        // Notes entered by the user
        [DataMember] public string Notes { get; set; }
    }
}
