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

			// 1. Initialize Engine
			var engine = new WaferMapEngine();
			engine.LoadMap(strMap, rows, columns);

			// 2. Define Ground Truth Transform (Distorted)
			// Rotation ~5 degrees
			double theta = 5.0 * Math.PI / 180.0;
			double scale = 1.0; // Assume perfect scale for now, or add scale error
			double cos = Math.Cos(theta);
			double sin = Math.Sin(theta);
			double tx = 100.0;
			double ty = 200.0;

			// True Function: Maps (c, r) -> (x, y)
			// Using chip pitch = 10mm
			double pitchX = 10.0;
			double pitchY = 10.0;

			Func<int, int, (double, double)> groundTruth = (c, r) =>
			{
				double localX = c * pitchX;
				double localY = r * pitchY;

				// Apply rotation & translation
				double globalX = (localX * cos - localY * sin) * scale + tx;
				double globalY = (localX * sin + localY * cos) * scale + ty;

				return (globalX, globalY);
			};

			// 3. Select Anchors
			int refCol = 10;
			int refRow = 10;
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
