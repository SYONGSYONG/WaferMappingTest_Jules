using FrameOfSystem3.Work.WaferMap.WaferMapDatabase;
using FrameOfSystem3.Work.WaferMap.WaferMapDatabase.InnerDocument;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrameOfSystem3.Work.WaferMap.MappingEngine
{
    public class WaferMappingEngine
    {
		//private WaferMap _waferMap;
		//public WaferMap Map => _waferMap;

		public AffineSolver.Transform CurrentTransform
        {
            get
            {
                return _currentTransform;
			}
		}

		AffineSolver _solver;
        AffineSolver.Transform _currentTransform;

        public WaferMappingEngine()
        {
            _solver = new AffineSolver();
        }

        //public void LoadMap(string mapData, int rows, int columns)
        //{
        //    _waferMap = new WaferMap(rows, columns);

        //    // Remove newlines if present
        //    string cleanMap = mapData.Replace("\r", "").Replace("\n", "");

        //    if (cleanMap.Length != rows * columns)
        //        throw new ArgumentException($"Map data length ({cleanMap.Length}) does not match Rows*Columns ({rows}*{columns}).");

        //    for (int i = 0; i < rows; i++)
        //    {
        //        for (int j = 0; j < columns; j++)
        //        {
        //            int index = i * columns + j;
        //            char c = cleanMap[index];
        //            int state = (c == '1') ? 1 : 0;

        //            // Always create chip object? Or only if state 1?
        //            // User requirement implies "Grid 좌표계를 복원".
        //            // Code sample creates chips for 0 too.

        //            var chip = new Chip
        //            {
        //                RowIndex = i,
        //                ColumnIndex = j,
        //                State = state,
        //                PositionX = 0,
        //                PositionY = 0
        //            };
        //            _waferMap.AddChip(chip);
        //        }
        //    }
        //}

        public List<UnitInformation> GetAnchorCandidates(WaferInformation wafer, int refCol, int refRow)
        {
            var selector = new AnchorSelector();
            // Start Offset is 1 for 1-based WaferInformation
            return selector.SelectAnchors(wafer, refCol, refRow, 1);
        }

        public void CalculateFitTransform(List<AnchorPoint> measuredAnchors, double outlierThreshold = 0.1, AnchorPoint referenceAnchor = null)
        {
			_currentTransform = _solver.FitWithOutlierRemoval(measuredAnchors, outlierThreshold, referenceAnchor);
		}

		///// <summary>
		///// Calculates the affine transform based on measured anchors and updates all chip positions.
		///// </summary>
		///// <param name="measuredAnchors">List of anchors with measured X, Y coordinates.</param>
		///// <param name="outlierThreshold">Threshold for outlier removal (default 0.1 mm = 100um).</param>
		//public void UpdateMapPositions(List<AnchorPoint> measuredAnchors, double outlierThreshold = 0.1)
		//{
		//    // Calculate transform with outlier removal
		//    _currentTransform = _solver.FitWithOutlierRemoval(measuredAnchors, outlierThreshold);

		//    // Update all chips
		//    foreach (var colPair in _waferMap.Map)
		//    {
		//        foreach (var rowPair in colPair.Value)
		//        {
		//            Chip chip = rowPair.Value;
		//            var (x, y) = _currentTransform.TransformPoint(chip.ColumnIndex, chip.RowIndex);
		//            chip.PositionX = x;
		//            chip.PositionY = y;
		//        }
		//    }
		//}

		public (double x, double y) Predict(int col, int row)
        {
            if (_currentTransform == null)
                throw new InvalidOperationException("Transform not calculated. Call UpdateMapPositions first.");

            return _currentTransform.TransformPoint(col, row);
        }
    }
}
