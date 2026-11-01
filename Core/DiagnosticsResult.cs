using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace IFCAlignmentManager.Core
{
    [DataContract]
    public class DiagnosticsResult
    {
        [DataMember] public CoordinateSnapshot HostSnapshot { get; set; }
        [DataMember] public CoordinateSnapshot ConsultantSnapshot { get; set; }

        // Computed differences (Consultant minus Host, in decimal feet)
        [DataMember] public double Delta_PBP_X { get; set; }
        [DataMember] public double Delta_PBP_Y { get; set; }
        [DataMember] public double Delta_PBP_Z { get; set; }
        [DataMember] public double Delta_TrueNorth_Radians { get; set; }
        [DataMember] public double Delta_SharedOrigin_EW { get; set; }
        [DataMember] public double Delta_SharedOrigin_NS { get; set; }

        // Human-readable warnings list
        [DataMember] public List<string> Warnings { get; set; } = new List<string>();

        // Severity: "None", "Minor", "Major", "Critical"
        [DataMember] public string Severity { get; set; } = "None";
    }
}
