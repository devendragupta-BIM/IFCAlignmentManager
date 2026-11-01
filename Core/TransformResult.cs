using System;
using System.Runtime.Serialization;

namespace IFCAlignmentManager.Core
{
    [DataContract]
    public class TransformResult
    {
        [DataMember] public string DocumentTitle { get; set; }
        [DataMember] public string Timestamp { get; set; }
        [DataMember] public bool Success { get; set; }
        [DataMember] public string ErrorMessage { get; set; }
        [DataMember] public string Method { get; set; }
        [DataMember] public double Translation_X { get; set; }
        [DataMember] public double Translation_Y { get; set; }
        [DataMember] public double Translation_Z { get; set; }
        [DataMember] public double Rotation_Radians { get; set; }
        [DataMember] public double ResidualError_Feet { get; set; }
    }
}
