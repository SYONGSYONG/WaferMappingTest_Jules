using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WaferMapping.Engine;

namespace TestConsole
{
	internal class Program
	{
		static void Main(string[] args)
		{
			// Example usage of WaferMapping.Engine with Visualization Export
			int rows = 20;
			int columns = 20;

			// Full map
			string strMap = new string('1', rows * columns);

			strMap = "00000001111110000000\r\n00000111111111100000\r\n00001111111111110000\r\n00011111111111111000\r\n00011111111111111000\r\n00111111111111111100\r\n00111111111111111100\r\n01111111111111111110\r\n01111111111111111110\r\n11111111111111111111\r\n11111111111111111111\r\n11111111111111111111\r\n01111111111111111110\r\n01111111111111111110\r\n00111111111111111100\r\n00011111111111111000\r\n00011111111111111000\r\n00001111111111110000\r\n00001111111111110000\r\n00000001111110000000";
			strMap = strMap.Replace("\r\n", ""); // Remove newlines for processing

			// 1. Initialize Engine
			var engine = new WaferMapEngine();
			engine.LoadMap(strMap, rows, columns);

			// 2. Define Simulation Parameters
			// Chip Size (e.g., 9.9mm) + Sawing Lane Pitch (e.g., 0.1mm) = Effective Pitch (10.0mm)
			double chipSizeX = 9.9;
			double sawingPitchX = 0.1;
			double effectivePitchX = chipSizeX + sawingPitchX;

			double chipSizeY = 9.9;
			double sawingPitchY = 0.1;
			double effectivePitchY = chipSizeY + sawingPitchY;

			// Define Ground Truth Transform (Affine + Non-linear Distortion)
			// Rotation ~5 degrees
			double theta = 5.0 * Math.PI / 180.0;
			double scale = 1.0;
			double cos = Math.Cos(theta);
			double sin = Math.Sin(theta);
			double tx = 100.0;
			double ty = 200.0;

			// Non-linear expansion factor (radial distortion simulator)
			// Expansion increases slightly with distance from center
			double distortionFactor = 1e-6; // Small factor, e.g. 1um per mm^2

			int centerCol = columns / 2;
			int centerRow = rows / 2;

			Func<int, int, (double, double)> groundTruth = (c, r) =>
			{
				// Local grid based on effective pitch
				double localX = (c - centerCol) * effectivePitchX;
				double localY = (r - centerRow) * effectivePitchY;

				// Radial distance squared
				double r2 = localX * localX + localY * localY;

				// Apply non-linear expansion
				// X' = X * (1 + k*r^2)
				// Y' = Y * (1 + k*r^2)
				double expansion = 1.0 + distortionFactor * r2;
				localX *= expansion;
				localY *= expansion;

				// Apply Global Affine (Rotation + Translation)
				// Returning to positive quadrant for convenience
				double globalX = (localX * cos - localY * sin) * scale + tx + (centerCol * effectivePitchX);
				double globalY = (localX * sin + localY * cos) * scale + ty + (centerRow * effectivePitchY);

				return (globalX, globalY);
			};

			// 3. Select Anchors
			// Reference Chip slightly off-center
			int refCol = centerCol;
			int refRow = centerRow;
			var anchors = engine.GetAnchorCandidates(refCol, refRow);
			Console.WriteLine($"Found {anchors.Count} anchor candidates.");

			// 4. Simulate Measurements with Noise
			var measuredAnchors = new List<AnchorPoint>();
			var rnd = new Random();
			var anchorSet = new HashSet<string>();

			foreach (var chip in anchors)
			{
				var (trueX, trueY) = groundTruth(chip.ColumnIndex, chip.RowIndex);

				// Add Gaussian-like noise (approx +/- 20um)
				double noiseX = (rnd.NextDouble() - 0.5) * 0.04;
				double noiseY = (rnd.NextDouble() - 0.5) * 0.04;

				measuredAnchors.Add(new AnchorPoint(chip.ColumnIndex, chip.RowIndex, trueX + noiseX, trueY + noiseY));
				anchorSet.Add($"{chip.ColumnIndex},{chip.RowIndex}");
			}

			// 5. Compute Transform
			try
			{
				// Outlier threshold 0.1mm
				engine.UpdateMapPositions(measuredAnchors, outlierThreshold: 0.1);
				Console.WriteLine("Map updated successfully.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Fitting failed: {ex.Message}");
				return;
			}

			// 6. Export Data to CSV for Visualization
			var sb = new StringBuilder();
			sb.AppendLine("Col,Row,TrueX,TrueY,PredictedX,PredictedY,IsAnchor");

			for (int r = 0; r < rows; r++)
			{
				for (int c = 0; c < columns; c++)
				{
					var (trueX, trueY) = groundTruth(c, r);
					var (predX, predY) = engine.Predict(c, r);
					bool isAnchor = anchorSet.Contains($"{c},{r}");

					sb.AppendLine($"{c},{r},{trueX},{trueY},{predX},{predY},{isAnchor}");
				}
			}

			string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "grid_data.csv");
			File.WriteAllText(csvPath, sb.ToString());
			Console.WriteLine($"Data exported to {csvPath}");
		}
	}
}
