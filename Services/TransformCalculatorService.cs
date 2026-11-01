using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using IFCAlignmentManager.Core;

namespace IFCAlignmentManager.Services
{
    public class TransformCalculatorService
    {
        public AlignmentTransformResult CalculateFromProjectBasePoints(CoordinateSnapshot host, CoordinateSnapshot consultant)
        {
            if (host == null || consultant == null)
                throw new ArgumentNullException("Host and consultant snapshots cannot be null.");

            double tx = host.PBP_X - consultant.PBP_X;
            double ty = host.PBP_Y - consultant.PBP_Y;
            double tz = host.PBP_Z - consultant.PBP_Z;
            double rot = host.TrueNorthAngle_Radians - consultant.TrueNorthAngle_Radians;

            return new AlignmentTransformResult
            {
                Method = "ProjectBasePoint",
                Translation_X = tx,
                Translation_Y = ty,
                Translation_Z = tz,
                Rotation_Radians = rot,
                RotationPivot_X = host.PBP_X,
                RotationPivot_Y = host.PBP_Y,
                RotationPivot_Z = host.PBP_Z,
                EstimatedResidualError_Feet = 0.0,
                Warnings = new List<string>()
            };
        }

        public AlignmentTransformResult CalculateFromSurveyPoints(CoordinateSnapshot host, CoordinateSnapshot consultant)
        {
            if (host == null || consultant == null)
                throw new ArgumentNullException("Host and consultant snapshots cannot be null.");

            double tx = host.SP_X - consultant.SP_X;
            double ty = host.SP_Y - consultant.SP_Y;
            double tz = host.SP_Z - consultant.SP_Z;
            double rot = host.TrueNorthAngle_Radians - consultant.TrueNorthAngle_Radians;

            return new AlignmentTransformResult
            {
                Method = "SurveyPoint",
                Translation_X = tx,
                Translation_Y = ty,
                Translation_Z = tz,
                Rotation_Radians = rot,
                RotationPivot_X = host.SP_X,
                RotationPivot_Y = host.SP_Y,
                RotationPivot_Z = host.SP_Z,
                EstimatedResidualError_Feet = 0.0,
                Warnings = new List<string>()
            };
        }

        public AlignmentTransformResult CalculateFromInternalOrigin(CoordinateSnapshot host, CoordinateSnapshot consultant)
        {
            if (host == null || consultant == null)
                throw new ArgumentNullException("Host and consultant snapshots cannot be null.");

            double tx = host.SharedOrigin_EastWest - consultant.SharedOrigin_EastWest;
            double ty = host.SharedOrigin_NorthSouth - consultant.SharedOrigin_NorthSouth;
            double tz = host.SharedOrigin_Elevation - consultant.SharedOrigin_Elevation;
            double rot = host.TrueNorthAngle_Radians - consultant.TrueNorthAngle_Radians;

            return new AlignmentTransformResult
            {
                Method = "InternalOrigin",
                Translation_X = tx,
                Translation_Y = ty,
                Translation_Z = tz,
                Rotation_Radians = rot,
                RotationPivot_X = 0,
                RotationPivot_Y = 0,
                RotationPivot_Z = 0,
                EstimatedResidualError_Feet = 0.0,
                Warnings = new List<string>()
            };
        }

        public AlignmentTransformResult CalculateFromCorrespondingPoints(XYZ[] hostPoints, XYZ[] consultantPoints)
        {
            if (hostPoints == null || hostPoints.Length < 2)
                throw new ArgumentException("At least 2 host points required.", nameof(hostPoints));
            if (consultantPoints == null || consultantPoints.Length < 2)
                throw new ArgumentException("At least 2 consultant points required.", nameof(consultantPoints));

            XYZ h1 = hostPoints[0];
            XYZ h2 = hostPoints[1];
            XYZ c1 = consultantPoints[0];
            XYZ c2 = consultantPoints[1];

            // 1. Translation vector to map C1 to H1: T = H1 - C1
            double tx = h1.X - c1.X;
            double ty = h1.Y - c1.Y;
            double tz = h1.Z - c1.Z;

            // 2. Translated C2: C2' = C2 + T
            XYZ c2Prime = new XYZ(c2.X + tx, c2.Y + ty, c2.Z + tz);

            // 3. Vectors relative to H1: vHost = H2 - H1, vConsultant = C2' - H1
            XYZ vHost = new XYZ(h2.X - h1.X, h2.Y - h1.Y, h2.Z - h1.Z);
            XYZ vConsultant = new XYZ(c2Prime.X - h1.X, c2Prime.Y - h1.Y, c2Prime.Z - h1.Z);

            // 4. Rotation angle: atan2(vHost.Y, vHost.X) - atan2(vConsultant.Y, vConsultant.X)
            double angleHost = Math.Atan2(vHost.Y, vHost.X);
            double angleConsultant = Math.Atan2(vConsultant.Y, vConsultant.X);
            double rot = angleHost - angleConsultant;

            // Normalize rot to [-pi, pi]
            while (rot > Math.PI) rot -= 2 * Math.PI;
            while (rot < -Math.PI) rot += 2 * Math.PI;

            var result = new AlignmentTransformResult
            {
                Method = "CorrespondingPoints",
                Translation_X = tx,
                Translation_Y = ty,
                Translation_Z = tz,
                Rotation_Radians = rot,
                RotationPivot_X = h1.X,
                RotationPivot_Y = h1.Y,
                RotationPivot_Z = h1.Z,
                Warnings = new List<string>()
            };

            // 5. Residual error: distance between H2 and rotated C2'
            XYZ c2Rotated = ApplyTransform(c2, result);
            double residual = Math.Sqrt(Math.Pow(h2.X - c2Rotated.X, 2) + Math.Pow(h2.Y - c2Rotated.Y, 2) + Math.Pow(h2.Z - c2Rotated.Z, 2));
            result.EstimatedResidualError_Feet = residual;

            if (residual > 0.001)
            {
                result.Warnings.Add($"Corresponding points pair distance mismatch. Residual error: {residual:F4} ft.");
            }

            return result;
        }

        public XYZ ApplyTransform(XYZ point, AlignmentTransformResult transform)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            // Step 1: Rotate around pivot FIRST (matches ProjectPosition adjustment order
            // in AlignmentExecutionService.ApplySharedCoordinateAlignment).
            double px = transform.RotationPivot_X;
            double py = transform.RotationPivot_Y;
            double rad = transform.Rotation_Radians;

            double dx = point.X - px;
            double dy = point.Y - py;

            double rx = dx * Math.Cos(rad) - dy * Math.Sin(rad) + px;
            double ry = dx * Math.Sin(rad) + dy * Math.Cos(rad) + py;
            double rz = point.Z;

            // Step 2: Translate AFTER rotation.
            double finalX = rx + transform.Translation_X;
            double finalY = ry + transform.Translation_Y;
            double finalZ = rz + transform.Translation_Z;

            return new XYZ(finalX, finalY, finalZ);
        }
    }
}
