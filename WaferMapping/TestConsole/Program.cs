using System;
using System.Collections.Generic;
using WaferMapping.Engine;

namespace TestConsole
{
	internal class Program
	{
		static void Main(string[] args)
		{
			// Example usage of WaferMapping.Engine
			int rows = 5;
			int columns = 6;

			// Original map string
			string strMap = "001100\r\n011110\r\n111111\r\n011110\r\n001100";

			// 1. Initialize Engine
			var engine = new WaferMapEngine();
			try
			{
				engine.LoadMap(strMap, rows, columns);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading map: {ex.Message}");
				return;
			}

			// 2. Select Anchors
			// Assuming reference at (Col=4, Row=3) based on original variables
			int refCol = 4;
			int refRow = 3;

			// In a real scenario, you would confirm the reference chip exists
			var refChip = engine.Map.GetChip(refCol, refRow);
			if (refChip == null || refChip.State == 0)
			{
				Console.WriteLine("Warning: Reference chip does not exist or is empty.");
			}

			var anchors = engine.GetAnchorCandidates(refCol, refRow);
			Console.WriteLine($"Found {anchors.Count} anchor candidates.");

			// 3. Simulate Measurements (In real machine, move stage to these chips and get Encoder positions)
			// Mocking a transform: X = Col * 11.0, Y = Row * 16.0
			var measuredAnchors = new List<AnchorPoint>();
			foreach (var chip in anchors)
			{
				// Simulated measurement
				double measuredX = chip.ColumnIndex * 11.0;
				double measuredY = chip.RowIndex * 16.0;

				measuredAnchors.Add(new AnchorPoint(chip.ColumnIndex, chip.RowIndex, measuredX, measuredY));
				Console.WriteLine($"Measured Anchor [{chip.ColumnIndex},{chip.RowIndex}] at ({measuredX}, {measuredY})");
			}

			// 4. Compute Transform and Update Map
			// Outlier threshold 0.1mm (100um)
			engine.UpdateMapPositions(measuredAnchors, outlierThreshold: 0.1);

			// 5. Inspect Results
			Console.WriteLine("\nMap Update Complete. Predicted Positions:");

			// Check a few chips
			for(int r=0; r<rows; r++)
			{
				for(int c=0; c<columns; c++)
				{
					var chip = engine.Map.GetChip(c, r);
					if (chip.State == 1)
					{
						Console.WriteLine($"Chip[{c},{r}]: X={chip.PositionX:F2}, Y={chip.PositionY:F2}");
					}
				}
			}

			// Predict specific coordinate
			var prediction = engine.Predict(0, 0);
			Console.WriteLine($"\nPrediction for (0,0): X={prediction.x:F2}, Y={prediction.y:F2}");

			Console.ReadLine();
		}
	}
}
