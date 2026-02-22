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

					var unit = wafer.GetUnitInformation(j + 1, i + 1); // Ensure chip object exists in wafer map
					unit.WorkingState = state; // Set state in wafer map
				}
			}

			int refCol = 12;
			int refRow = 19;

			// Add random defects (Missing Chips), ensuring Reference is kept
			var rnd = new Random();
			int defectCount = 0;
			for (int r = 1; r <= rows; r++)
			{
				for (int c = 1; c <= columns; c++)
				{
					if (c == refCol && r == refRow) continue; // Skip Reference Chip

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
			Console.WriteLine($"Injected {defectCount} random defects (State 1 -> 0). Reference kept.");

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
			var centerChip = wafer.GetUnitInformation(refCol, refRow); // +1 for 1-based indexing in wafer map is already handled by caller if input is 1-based
            // Note: refCol/refRow are 1-based here as per previous context? Let's check.
            // In SelectAnchors call: selector.SelectAnchors(wafer, refCol, refRow, 1);
            // In random defect loop: if (c == refCol && r == refRow) continue;
            // It seems refCol/refRow are used as 1-based indices consistently.

			//var centerChip = wafer.GetUnitInformation(refCol + 1, refRow + 1);
            // Correcting to use variables directly as they seem 1-based
            centerChip = wafer.GetUnitInformation(refCol, refRow);

			if (centerChip == null || centerChip.WorkingState == 0)
			{
                // Force Reference Chip to exist
                if (centerChip != null) centerChip.WorkingState = 1;
				Console.WriteLine("Warning: Reference chip was missing/defect. Forced to State 1.");
			}

			var anchors = engine.GetAnchorCandidates(wafer, refCol, refRow);
			Console.WriteLine($"Found {anchors.Count} anchor candidates (State=1 only).");

			// 4. Simulate Measurements with Incremental Update
			Console.WriteLine("\n--- Starting Incremental Update Simulation ---");

            // Initial Setup: Calculate Nominal Positions relative to Reference
            // We assume we have found the Reference Chip at its Ground Truth position initially.
            var (refTrueX, refTrueY) = groundTruth(refCol, refRow);

            // Set Initial Transform using Reference Position
            // Tx, Ty will be such that Predict(refCol, refRow) == (refTrueX, refTrueY)
            // Initial Scale/Rot is Nominal (Design Pitch)
            engine.SetInitialTransform(effectivePitchX, effectivePitchY);

            // Update initial AxisPositionForDryRun for all chips based on this nominal transform + Ref offset
            // We need to shift the nominal transform so that Ref aligns with its measured position.
            // Nominal Transform: X = Col * Pitch, Y = Row * Pitch
            // We want: X' = (Col - RefCol) * Pitch + RefTrueX

            // Let's effectively "Measure" the Reference Chip first to initialize the transform properly
            var measuredAnchors = new List<AnchorPoint>();
            measuredAnchors.Add(new AnchorPoint(refCol, refRow, refTrueX, refTrueY));
            engine.UpdateTransform(measuredAnchors); // This will set Tx, Ty based on Ref

            // Apply this initial transform to all chips
			for (int r = 1; r <= rows; r++)
			{
				for (int c = 1; c <= columns; c++)
				{
					var unit = wafer.GetUnitInformation(c, r);
					var (x, y) = engine.Predict(c, r);
					unit.AxisPositionForDryRun = new FrameOfSystem3.DPointXY(x, y);
				}
			}

			var anchorSet = new HashSet<string>();
            anchorSet.Add($"{refCol},{refRow}");

			try
			{
				if (anchors.Count == 0)
				{
					Console.WriteLine("No anchors found.");
				}
				else
				{
					foreach (var chip in anchors)
					{
                        if (chip.UnitIndex.x == refCol && chip.UnitIndex.y == refRow) continue; // Skip Ref (already measured)

                        // 1. Move to Target (Predict)
                        // In real scenario, we move stage to chip.AxisPositionForDryRun
                        double targetX = chip.AxisPositionForDryRun.x;
                        double targetY = chip.AxisPositionForDryRun.y;

                        // 2. Measure (Ground Truth)
						var (trueX, trueY) = groundTruth(chip.UnitIndex.x, chip.UnitIndex.y);

                        // Calculate Distance (Error)
						double err = Math.Sqrt(Math.Pow(targetX - trueX, 2) + Math.Pow(targetY - trueY, 2));
						Console.WriteLine($"Jumping to Anchor ({chip.UnitIndex.x}, {chip.UnitIndex.y}). Prediction Error: {err:F4}");

                        // Add noise +/- 20um to Measurement
						double noiseX = (rnd.NextDouble() - 0.5) * 0.04;
						double noiseY = (rnd.NextDouble() - 0.5) * 0.04;

						var newAnchor = new AnchorPoint(chip.UnitIndex.x, chip.UnitIndex.y, trueX + noiseX, trueY + noiseY);
						measuredAnchors.Add(newAnchor);
						anchorSet.Add($"{chip.UnitIndex.x},{chip.UnitIndex.y}");

						// 3. Update Transform incrementally
						engine.UpdateTransform(measuredAnchors, outlierThreshold: 0.1);

                        // 4. Apply new Transform to REMAINING chips (Update Path)
                        // This ensures next jumps benefit from the update
                        for (int r = 1; r <= rows; r++)
                        {
                            for (int c = 1; c <= columns; c++)
                            {
                                var unit = wafer.GetUnitInformation(c, r);
                                var (x, y) = engine.Predict(c, r);
                                unit.AxisPositionForDryRun = new FrameOfSystem3.DPointXY(x, y);
                            }
                        }
					}

					Console.WriteLine("--- Incremental Update Complete ---\n");

					// Update final map positions
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