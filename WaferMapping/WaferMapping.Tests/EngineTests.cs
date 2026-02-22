using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using WaferMapping.Engine;

namespace WaferMapping.Tests
{
    public class EngineTests
    {
        [Fact]
        public void Test_FullFlow_PerfectAlignment()
        {
            // 1. Setup Map (10x10)
            int rows = 10;
            int cols = 10;
            string mapStr = new string('1', rows * cols); // All exist

            var engine = new WaferMapEngine();
            engine.LoadMap(mapStr, rows, cols);

            // 2. Define Ground Truth Transform
            // Simple: Scale=10, Offset=(100, 200)
            double trueA = 10.0, trueB = 0.0, trueTx = 100.0;
            double trueC = 0.0, trueD = 10.0, trueTy = 200.0;

            Func<int, int, (double, double)> groundTruth = (c, r) =>
            {
                double x = trueA * c + trueB * r + trueTx;
                double y = trueC * c + trueD * r + trueTy;
                return (x, y);
            };

            // 3. Select Anchors
            // Reference at center (5, 5)
            var anchors = engine.GetAnchorCandidates(5, 5);
            Assert.NotEmpty(anchors);

            // 4. Simulate Measurement
            var measured = new List<AnchorPoint>();
            foreach (var chip in anchors)
            {
                var (mx, my) = groundTruth(chip.ColumnIndex, chip.RowIndex);
                measured.Add(new AnchorPoint(chip.ColumnIndex, chip.RowIndex, mx, my));
            }

            // 5. Run Fitting
            engine.UpdateMapPositions(measured);

            // 6. Verify Prediction
            // Check a random point (e.g. 0,0)
            var (predX, predY) = engine.Predict(0, 0);
            var (actX, actY) = groundTruth(0, 0);

            Assert.Equal(actX, predX, precision: 3);
            Assert.Equal(actY, predY, precision: 3);

            // Check Chip object update
            var chip00 = engine.Map.GetChip(0, 0);
            Assert.Equal(actX, chip00.PositionX, precision: 3);
            Assert.Equal(actY, chip00.PositionY, precision: 3);
        }

        [Fact]
        public void Test_RotationAndNoise()
        {
            // 1. Setup Map (20x20)
            int rows = 20;
            int cols = 20;
            // Create a ring pattern? Or just full map. Full is easier.
            string mapStr = new string('1', rows * cols);

            var engine = new WaferMapEngine();
            engine.LoadMap(mapStr, rows, cols);

            // 2. Define Ground Truth Transform (Rotation ~1 degree)
            double theta = 1.0 * Math.PI / 180.0; // 1 degree
            double cos = Math.Cos(theta);
            double sin = Math.Sin(theta);
            double scale = 1.0;

            // X = Scale*(Cos*C - Sin*R) + Tx
            // Y = Scale*(Sin*C + Cos*R) + Ty
            double trueA = scale * cos;
            double trueB = scale * -sin;
            double trueTx = 50.0;

            double trueC = scale * sin;
            double trueD = scale * cos;
            double trueTy = 50.0;

            Func<int, int, (double, double)> groundTruth = (c, r) =>
            {
                double x = trueA * c + trueB * r + trueTx;
                double y = trueC * c + trueD * r + trueTy;
                return (x, y);
            };

            // 3. Select Anchors
            var anchors = engine.GetAnchorCandidates(10, 10);

            // 4. Simulate Measurement with Noise
            var measured = new List<AnchorPoint>();
            var rnd = new Random(123);
            foreach (var chip in anchors)
            {
                var (mx, my) = groundTruth(chip.ColumnIndex, chip.RowIndex);
                // Add noise +/- 0.005 (5um)
                mx += (rnd.NextDouble() - 0.5) * 0.01;
                my += (rnd.NextDouble() - 0.5) * 0.01;
                measured.Add(new AnchorPoint(chip.ColumnIndex, chip.RowIndex, mx, my));
            }

            // Add an outlier
            var outlierChip = anchors.Last();
            measured.RemoveAll(p => p.Col == outlierChip.ColumnIndex && p.Row == outlierChip.RowIndex);
            // Re-add with huge error
            measured.Add(new AnchorPoint(outlierChip.ColumnIndex, outlierChip.RowIndex,
                groundTruth(outlierChip.ColumnIndex, outlierChip.RowIndex).Item1 + 1.0, // 1mm error
                groundTruth(outlierChip.ColumnIndex, outlierChip.RowIndex).Item2 + 1.0));

            // 5. Run Fitting with outlier removal
            engine.UpdateMapPositions(measured, outlierThreshold: 0.1);

            // 6. Verify Prediction (excluding outlier)
            // Check center
            var (predX, predY) = engine.Predict(10, 10);
            var (actX, actY) = groundTruth(10, 10);

            // Precision might be lower due to noise
            Assert.Equal(actX, predX, precision: 1);
            Assert.Equal(actY, predY, precision: 1);
        }
    }
}
