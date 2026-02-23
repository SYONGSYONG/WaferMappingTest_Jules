using System;
using System.Text;

namespace WaferMapping.Engine
{
    public class Matrix
    {
        private double[,] _data;

        public int Rows { get; }
        public int Cols { get; }

        public Matrix(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            _data = new double[rows, cols];
        }

        public double this[int r, int c]
        {
            get => _data[r, c];
            set => _data[r, c] = value;
        }

        public Matrix Transpose()
        {
            var res = new Matrix(Cols, Rows);
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    res[c, r] = _data[r, c];
                }
            }
            return res;
        }

        public static Matrix Multiply(Matrix A, Matrix B)
        {
            if (A.Cols != B.Rows)
                throw new ArgumentException("Matrix dimensions mismatch for multiplication.");

            var res = new Matrix(A.Rows, B.Cols);
            for (int r = 0; r < A.Rows; r++)
            {
                for (int c = 0; c < B.Cols; c++)
                {
                    double sum = 0;
                    for (int k = 0; k < A.Cols; k++)
                    {
                        sum += A[r, k] * B[k, c];
                    }
                    res[r, c] = sum;
                }
            }
            return res;
        }

        public static Matrix operator *(Matrix A, Matrix B) => Multiply(A, B);

        public Matrix Inverse()
        {
            if (Rows != Cols)
                throw new InvalidOperationException("Only square matrices can be inverted.");

            int n = Rows;
            double[,] result = new double[n, n];
            double[,] input = (double[,])_data.Clone(); // Copy so we don't modify original

            // Initialize result as identity matrix
            for (int i = 0; i < n; i++)
                result[i, i] = 1.0;

            // Gaussian elimination with partial pivoting
            for (int i = 0; i < n; i++)
            {
                // Find pivot
                double pivotVal = input[i, i];
                int pivotRow = i;
                for (int j = i + 1; j < n; j++)
                {
                    if (Math.Abs(input[j, i]) > Math.Abs(pivotVal))
                    {
                        pivotVal = input[j, i];
                        pivotRow = j;
                    }
                }

                if (Math.Abs(pivotVal) < 1e-10)
                    throw new InvalidOperationException("Matrix is singular or ill-conditioned (determinant close to zero). Cannot compute inverse.");

                // Swap rows if needed
                if (pivotRow != i)
                {
                    for (int k = 0; k < n; k++)
                    {
                        double temp = input[i, k];
                        input[i, k] = input[pivotRow, k];
                        input[pivotRow, k] = temp;

                        temp = result[i, k];
                        result[i, k] = result[pivotRow, k];
                        result[pivotRow, k] = temp;
                    }
                }

                // Scale row i to make pivot 1
                double scale = 1.0 / input[i, i];
                for (int k = 0; k < n; k++)
                {
                    input[i, k] *= scale;
                    result[i, k] *= scale;
                }

                // Eliminate other rows
                for (int j = 0; j < n; j++)
                {
                    if (i != j)
                    {
                        double factor = input[j, i];
                        for (int k = 0; k < n; k++)
                        {
                            input[j, k] -= factor * input[i, k];
                            result[j, k] -= factor * result[i, k];
                        }
                    }
                }
            }

            // Copy result back to Matrix object
            var resMatrix = new Matrix(n, n);
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    resMatrix[r, c] = result[r, c];

            return resMatrix;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int r = 0; r < Rows; r++)
            {
                sb.Append("[ ");
                for (int c = 0; c < Cols; c++)
                {
                    sb.Append($"{_data[r, c]:F4} ");
                }
                sb.AppendLine("]");
            }
            return sb.ToString();
        }
    }
}
