using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace IFCAlignmentManager.Core
{
    [DataContract]
    public class AlignmentTransformResult
    {
        [DataMember] public double Translation_X { get; set; }   // decimal feet
        [DataMember] public double Translation_Y { get; set; }
        [DataMember] public double Translation_Z { get; set; }
        [DataMember] public double Rotation_Radians { get; set; }
        [DataMember] public double RotationPivot_X { get; set; }
        [DataMember] public double RotationPivot_Y { get; set; }
        [DataMember] public double RotationPivot_Z { get; set; }
        [DataMember] public double EstimatedResidualError_Feet { get; set; }
        [DataMember] public string Method { get; set; }
        [DataMember] public List<string> Warnings { get; set; } = new List<string>();
    }
}
