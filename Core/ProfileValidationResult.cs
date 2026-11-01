using System;
using System.Collections.Generic;

namespace IFCAlignmentManager.Core
{
    public class ProfileValidationResult
    {
        public bool IsCompatible { get; set; }
        public List<string> Mismatches { get; set; } = new List<string>();
        public double MaxDeviation_Feet { get; set; }
    }
}
