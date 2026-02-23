using System;
using System.Collections.Generic;
using System.Linq;

namespace FrameOfSystem3.Work.WaferMap.MappingEngine
{
    public class AffineSolver
    {
        public class Transform
        {
            public double A { get; set; }
            public double B { get; set; }
            public double Tx { get; set; }
            public double C { get; set; }
            public double D { get; set; }
            public double Ty { get; set; }

            public (double x, double y) TransformPoint(int col, int row)
            {
                double x = A * col + B * row + Tx;
                double y = C * col + D * row + Ty;
                return (x, y);
            }
        }

        public Transform Fit(List<AnchorPoint> points)
        {
            if (points == null || points.Count < 3)
                throw new ArgumentException("At least 3 points are required for affine fitting.");

            int n = points.Count;

            // Design Matrix M: [Col, Row, 1]
            Matrix M = new Matrix(n, 3);
            // Vector X coordinates
            Matrix Yx = new Matrix(n, 1);
            // Vector Y coordinates
            Matrix Yy = new Matrix(n, 1);

            for (int i = 0; i < n; i++)
            {
                M[i, 0] = points[i].Col;
                M[i, 1] = points[i].Row;
                M[i, 2] = 1.0;

                Yx[i, 0] = points[i].X;
                Yy[i, 0] = points[i].Y;
            }

            // Normal Equation: beta = (M^T * M)^-1 * M^T * Y
            Matrix Mt = M.Transpose();
            Matrix MtM = Mt * M;

            // Add regularization for numerical stability if needed, but usually not for simple affine
            // If singular, Inverse will throw.
            Matrix MtM_Inv = MtM.Inverse();
            Matrix PseudoInverse = MtM_Inv * Mt;

            Matrix BetaX = PseudoInverse * Yx;
            Matrix BetaY = PseudoInverse * Yy;

            return new Transform
            {
                A = BetaX[0, 0],
                B = BetaX[1, 0],
                Tx = BetaX[2, 0],
                C = BetaY[0, 0],
                D = BetaY[1, 0],
                Ty = BetaY[2, 0]
            };
        }

        public Transform FitWithOutlierRemoval(List<AnchorPoint> points, double threshold)
        {
            var currentPoints = new List<AnchorPoint>(points);
            Transform transform = null;
            bool removed = true;

            while (removed && currentPoints.Count >= 3)
            {
                removed = false;
                transform = Fit(currentPoints);

                // Calculate residuals
                double maxResidual = -1.0;
                AnchorPoint outlier = null;

                foreach (var p in currentPoints)
                {
                    var (predX, predY) = transform.TransformPoint(p.Col, p.Row);
                    double dx = p.X - predX;
                    double dy = p.Y - predY;
                    double residual = Math.Sqrt(dx * dx + dy * dy);

                    if (residual > threshold && residual > maxResidual)
                    {
                        maxResidual = residual;
                        outlier = p;
                    }
                }

                if (outlier != null)
                {
                    currentPoints.Remove(outlier);
                    removed = true;
                }
            }

            if (transform == null) // Should not happen if initial points >= 3
                transform = Fit(currentPoints);

            return transform;
        }
    }
}
