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

        /// <summary>
        /// Fits an affine transform with outlier removal.
        /// </summary>
        /// <param name="points">List of anchor points.</param>
        /// <param name="threshold">Residual threshold for outlier detection.</param>
        /// <param name="fixedPoint">Optional reference point that must NOT be removed.</param>
        /// <returns>The best fit transform.</returns>
        public Transform FitWithOutlierRemoval(List<AnchorPoint> points, double threshold, AnchorPoint fixedPoint = null)
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
                    // Skip fixed point (reference)
                    if (fixedPoint != null && p.Col == fixedPoint.Col && p.Row == fixedPoint.Row)
                        continue;

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

        /// <summary>
        /// Updates only the translation (Tx, Ty) based on the provided points, keeping A, B, C, D fixed from the reference transform.
        /// Useful for single-point updates.
        /// </summary>
        public Transform FitTranslation(List<AnchorPoint> points, Transform refTransform)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("At least 1 point is required for translation update.");

            double sumTx = 0;
            double sumTy = 0;
            int n = points.Count;

            foreach (var p in points)
            {
                // Calculate idealized linear part
                double linearX = refTransform.A * p.Col + refTransform.B * p.Row;
                double linearY = refTransform.C * p.Col + refTransform.D * p.Row;

                // Residual becomes the translation
                sumTx += (p.X - linearX);
                sumTy += (p.Y - linearY);
            }

            return new Transform
            {
                A = refTransform.A,
                B = refTransform.B,
                C = refTransform.C,
                D = refTransform.D,
                Tx = sumTx / n,
                Ty = sumTy / n
            };
        }

        /// <summary>
        /// Fits a Similarity Transform (Translation + Rotation + Uniform Scale).
        /// Solves for u, v, Tx, Ty where A=u, B=-v, C=v, D=u.
        /// </summary>
        public Transform FitSimilarity(List<AnchorPoint> points)
        {
            if (points == null || points.Count < 2)
                throw new ArgumentException("At least 2 points are required for similarity fitting.");

            int n = points.Count;
            // Design Matrix M: [2n x 4]
            // [ c, -r, 1, 0 ]
            // [ r,  c, 0, 1 ]
            Matrix M = new Matrix(2 * n, 4);
            Matrix Y = new Matrix(2 * n, 1);

            for (int i = 0; i < n; i++)
            {
                int row1 = 2 * i;
                int row2 = 2 * i + 1;

                // X equation: X = u*c - v*r + Tx
                M[row1, 0] = points[i].Col;   // u
                M[row1, 1] = -points[i].Row;  // v
                M[row1, 2] = 1.0;             // Tx
                M[row1, 3] = 0.0;             // Ty
                Y[row1, 0] = points[i].X;

                // Y equation: Y = v*c + u*r + Ty -> Y = u*r + v*c + Ty
                M[row2, 0] = points[i].Row;   // u
                M[row2, 1] = points[i].Col;   // v
                M[row2, 2] = 0.0;             // Tx
                M[row2, 3] = 1.0;             // Ty
                Y[row2, 0] = points[i].Y;
            }

            // Solve M * Beta = Y
            // Beta = (M^T * M)^-1 * M^T * Y
            Matrix Mt = M.Transpose();
            Matrix MtM = Mt * M;
            Matrix MtM_Inv = MtM.Inverse();
            Matrix Beta = MtM_Inv * Mt * Y;

            double u = Beta[0, 0];
            double v = Beta[1, 0];
            double tx = Beta[2, 0];
            double ty = Beta[3, 0];

            return new Transform
            {
                A = u,
                B = -v,
                C = v,
                D = u,
                Tx = tx,
                Ty = ty
            };
        }
    }
}
