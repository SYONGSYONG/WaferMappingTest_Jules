using System;
using System.Collections.Generic;
using System.Linq;

namespace WaferMapping.Engine
{
    public class WaferMapEngine
    {
        private WaferInformation _waferInfo;
        private AffineSolver _solver;
        private AffineSolver.Transform _currentTransform;

        public WaferInformation Map => _waferInfo;

        public WaferMapEngine()
        {
            _solver = new AffineSolver();
        }

        public void LoadMap(string mapData, int rows, int columns)
        {
            _waferInfo = new WaferInformation(rows, columns);

            // Remove newlines if present
            string cleanMap = mapData.Replace("\r", "").Replace("\n", "");

            if (cleanMap.Length != rows * columns)
                throw new ArgumentException($"Map data length ({cleanMap.Length}) does not match Rows*Columns ({rows}*{columns}).");

            // Assuming mapData is row-major order: Row 1 (Col 1 to Cols), then Row 2, etc.
            // 1-based indexing: Rows 1..M, Cols 1..N
            for (int r = 1; r <= rows; r++)
            {
                for (int c = 1; c <= columns; c++)
                {
                    // Calculate 0-based index for string access
                    int index = (r - 1) * columns + (c - 1);
                    char charValue = cleanMap[index];
                    int state = (charValue == '1') ? 1 : 0;

                    var chip = new Chip
                    {
                        RowIndex = r,      // 1-based
                        ColumnIndex = c,   // 1-based
                        State = state,
                        PositionX = 0,
                        PositionY = 0
                    };
                    _waferInfo.AddChip(chip);
                }
            }
        }

        public List<Chip> GetAnchorCandidates(int refCol, int refRow)
        {
            if (_waferInfo == null)
                throw new InvalidOperationException("Map not loaded.");

            var selector = new AnchorSelector();
            return selector.SelectAnchors(_waferInfo, refCol, refRow);
        }

        /// <summary>
        /// Calculates the affine transform based on measured anchors and updates all chip positions.
        /// </summary>
        /// <param name="measuredAnchors">List of anchors with measured X, Y coordinates.</param>
        /// <param name="outlierThreshold">Threshold for outlier removal (default 0.1 mm = 100um).</param>
        public void UpdateMapPositions(List<AnchorPoint> measuredAnchors, double outlierThreshold = 0.1)
        {
            if (_waferInfo == null)
                throw new InvalidOperationException("Map not loaded.");

            // Calculate transform with outlier removal
            _currentTransform = _solver.FitWithOutlierRemoval(measuredAnchors, outlierThreshold);

            // Update all chips
            foreach (var colPair in _waferInfo.Map)
            {
                foreach (var rowPair in colPair.Value)
                {
                    Chip chip = rowPair.Value;
                    var (x, y) = _currentTransform.TransformPoint(chip.ColumnIndex, chip.RowIndex);
                    chip.PositionX = x;
                    chip.PositionY = y;
                }
            }
        }

        public (double x, double y) Predict(int col, int row)
        {
            if (_currentTransform == null)
                throw new InvalidOperationException("Transform not calculated. Call UpdateMapPositions first.");

            return _currentTransform.TransformPoint(col, row);
        }
    }
}
