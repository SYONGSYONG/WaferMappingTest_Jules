using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Define.DefineEnumProject.WaferMap;
using FrameOfSystem3.Work.WaferMap.MappingEngine;
using FrameOfSystem3.Work.WaferMap.WaferMapDatabase;

namespace TestConsole
{
	internal class Program
	{
		static void Main(string[] args)
		{
			// Example usage of WaferMapping.Engine with Visualization Export
			int rows = 20;
			int columns = 20;

			// Provided Map (20x20) - Use string for readability
			string[] mapLines = new string[]
			{
				"00000001111110000000",
				"00000111111111100000",
				"00001111111111110000",
				"00011111111111111000",
				"00011111111111111000",
				"00111111111111111100",
				"00111111111111111100",
				"01111111111111111110",
				"01111111111111111110",
				"11111111111111111111",
				"11111111111111111111",
				"11111111111111111111",
				"01111111111111111110",
				"01111111111111111110",
				"00111111111111111100",
				"00011111111111111000",
				"00011111111111111000",
				"00001111111111110000",
				"00001111111111110000",
				"00000001111110000000"
			};

			WaferInformation wafer = new WaferInformation("TEST_WAFER");
			wafer.ChangeArrayCount(rows, columns);

			// Join map for loading
			string strMap = string.Join("", mapLines);

			// 1. Initialize Engine
			var engine = new WaferMappingEngine();

			string cleanMap = strMap.Replace("\r", "").Replace("\n", "");

			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < columns; j++)
				{
					int index = i * columns + j;
					char c = cleanMap[index];
					int state = (c == '1') ? 1 : 0;

					// Always create chip object? Or only if state 1?
					// User requirement implies "Grid 좌표계를 복원".
					// Code sample creates chips for 0 too.

					var unit = wafer.GetUnitInformation(j + 1, i + 1); // Ensure chip object exists in wafer map
					unit.WorkingState = state; // Set state in wafer map
				}
			}

			//engine.LoadMap(strMap, rows, columns);

			//// Check for --half-moon argument
			//bool halfMoon = false;
			//foreach (var arg in args)
			//{
			//	if (arg == "--half-moon") halfMoon = true;
			//}

			//if (halfMoon)
			//{
			//	Console.WriteLine("Applying HALF-MOON defect pattern (Bottom half missing)...");
			//	for (int r = rows / 2; r < rows; r++)
			//	{
			//		for (int c = 0; c < columns; c++)
			//		{
			//			var chip = engine.Map.GetChip(c, r);
			//			if (chip != null) chip.State = 0;
			//		}
			//	}
			//}

			int refCol = 12;
			int refRow = 19;

			// Add random defects (Missing Chips)
			var rnd = new Random();
			int defectCount = 0;
			for (int r = 1; r <= rows; r++)
			{
				for (int c = 1; c <= columns; c++)
				{
					var unit = wafer.GetUnitInformation(c, r);
					if (unit != null && unit.WorkingState == 1)
					{
						if (rnd.NextDouble() < 0.1)
						{
							unit.WorkingState = 0;
							defectCount++;
						}
					}
				}
			}
			Console.WriteLine($"Injected {defectCount} random defects (State 1 -> 0).");

			// 2. Define Simulation Parameters
			double chipSizeX = 9.9;
			double sawingPitchX = 0.1;
			double effectivePitchX = chipSizeX + sawingPitchX;

			double chipSizeY = 9.9;
			double sawingPitchY = 0.1;
			double effectivePitchY = chipSizeY + sawingPitchY;

			// Define Ground Truth Transform (Affine + Non-linear Distortion)
			double theta = 5.0 * Math.PI / 180.0;
			double scale = 1.0;
			double cos = Math.Cos(theta);
			double sin = Math.Sin(theta);
			double tx = 100.0;
			double ty = 200.0;

			// Non-linear expansion
			double distortionFactor = 1e-6;

			int centerCol = columns / 2;
			int centerRow = rows / 2;

			Func<int, int, (double, double)> groundTruth = (c, r) =>
			{
				double localX = (c - centerCol) * effectivePitchX;
				double localY = (r - centerRow) * effectivePitchY;

				double r2 = localX * localX + localY * localY;

				double expansion = 1.0 + distortionFactor * r2;
				localX *= expansion;
				localY *= expansion;

				double globalX = (localX * cos - localY * sin) * scale + tx + (centerCol * effectivePitchX);
				double globalY = (localX * sin + localY * cos) * scale + ty + (centerRow * effectivePitchY);

				return (globalX, globalY);
			};

			// 3. Select Anchors
			// Reference Chip at center

			Console.WriteLine($"Using Reference: ({refCol}, {refRow})");

			// Ensure center chip exists (if defect, find nearest)
			// var centerChip = engine.Map.GetChip(refCol, refRow);
			var centerChip = wafer.GetUnitInformation(refCol + 1, refRow + 1); // +1 for 1-based indexing in wafer map
			if (centerChip == null || centerChip.WorkingState == 0)
			{
				Console.WriteLine("Warning: Center reference chip is missing/defect. Anchors might be skewed.");
			}

			var anchors = engine.GetAnchorCandidates(wafer, refCol, refRow);
			Console.WriteLine($"Found {anchors.Count} anchor candidates (State=1 only).");

			// 4. Simulate Measurements with Noise
			var measuredAnchors = new List<AnchorPoint>();
			var anchorSet = new HashSet<string>();

			foreach (var chip in anchors)
			{
				var (trueX, trueY) = groundTruth(chip.UnitIndex.x, chip.UnitIndex.y);

				// Add noise +/- 20um
				double noiseX = (rnd.NextDouble() - 0.5) * 0.04;
				double noiseY = (rnd.NextDouble() - 0.5) * 0.04;

				measuredAnchors.Add(new AnchorPoint(chip.UnitIndex.x, chip.UnitIndex.y, trueX + noiseX, trueY + noiseY));
				anchorSet.Add($"{chip.UnitIndex.x},{chip.UnitIndex.y}");
			}

			// 5. Compute Transform
			try
			{
				if (anchors.Count < 3)
				{
					Console.WriteLine("Insufficient anchors found. Mapping aborted.");
				}
				else
				{
					engine.CalculateFitTransform(measuredAnchors, outlierThreshold: 0.1);
					for (int r = 1; r <= rows; r++)
					{
						for (int c = 1; c <= columns; c++)
						{
							var unit = wafer.GetUnitInformation(c, r);
							var (x, y) = engine.CurrentTransform.TransformPoint(c, r);
							unit.AxisPositionForDryRun = new FrameOfSystem3.DPointXY(x, y);
							unit.AxisPositionAppiedAlignResult = new FrameOfSystem3.DPointXY(x, y);
						}
					}
					Console.WriteLine("Map updated successfully.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Fitting failed: {ex.Message}");
				return;
			}

			// 6. Export Data to CSV for Visualization
			var sb = new StringBuilder();
			sb.AppendLine("Col,Row,State,TrueX,TrueY,PredictedX,PredictedY,IsAnchor");

			for (int r = 1; r <= rows; r++)
			{
				for (int c = 1; c <= columns; c++)
				{
					var chip = wafer.GetUnitInformation(c, r);
					int state = (chip != null) ? chip.WorkingState : 0;

					var (trueX, trueY) = groundTruth(c, r);
					var (predX, predY) = engine.Predict(c, r);
					bool isAnchor = anchorSet.Contains($"{c},{r}");

					sb.AppendLine($"{c},{r},{state},{trueX},{trueY},{predX},{predY},{isAnchor}");
				}
			}

			string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "grid_data.csv");
			File.WriteAllText(csvPath, sb.ToString());
			Console.WriteLine($"Data exported to {csvPath}");
		}
	}
}