using System.Collections.Generic;
using System.ComponentModel.Design;

namespace TestConsole
{
	internal class Program
	{
		public class Chip
		{
			/// <summary>
			/// Map Row Index
			/// </summary>
			public int RowIndex { get; set; }

			/// <summary>
			/// Map Column Index
			/// </summary>
			public int ColumnIndex { get; set; }
			public int State { get; set; } // 0 : Not Exist, 1: Exist

			/// <summary>
			/// EncoderPositionX
			/// </summary>
			public double PositionX { get; set; }

			/// <summary>
			/// EncoderPositionY
			/// </summary>
			public double PositionY { get; set; }
		}

		public class WaferMap
		{
			public WaferMap(int rows, int columns)
			{
				this.Rows = rows;
				this.Columns = columns;
				this.Map = new Dictionary<int, Dictionary<int, Chip>>();
			}

			public int Rows { get; set; }
			public int Columns { get; set; }

			/// <summary>
			/// Key : Col, Value : Dictionary with key as Row and value as Chip State(0 : Not Exist, 1: Exist)
			/// </summary>
			public Dictionary<int, Dictionary<int, Chip>> Map { get; set; }
		}

		static double CalculateChipPositionX(double referenceChipPositionX, int columnIndex, int referenceIndexY, double chipSizeX, double idealPitchX)
		{
			return referenceChipPositionX + (columnIndex - referenceIndexY) * (chipSizeX + idealPitchX);
		}

		static double CalculateChipPositionY(double referenceChipPositionY, int rowIndex, int referenceIndexX, double chipSizeY, double idealPitchY)
		{
			return referenceChipPositionY + (rowIndex - referenceIndexX) * (chipSizeY + idealPitchY);
		}

		static void Main(string[] args)
		{
			int rows = 5;
			int columns = 6;

			// Mapping 이후 찾은 Reference Chip의 Encoder PositionX
			double referenceChipPositionX = 0.0;

			// Mapping 이후 찾은 Reference Chip의 Encoder PositionY
			double referenceChipPositionY = 0.0;

			int referenceIndexX = 3;
			int referenceIndexY = 4;

			double chipSizeX = 10.0;
			double chipSizeY = 15.0;

			double idealPitchX = 1.0;
			double idealPitchY = 1.0;

			string strMap = "001100\r\n011110\r\n111111\r\n011110\r\n001100";

			strMap = strMap.Replace("\r\n", string.Empty);

			WaferMap waferMap = new WaferMap(rows, columns);
			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < columns; j++)
				{
					int state = int.Parse(strMap[i * columns + j].ToString());
					if (!waferMap.Map.ContainsKey(j))
					{
						waferMap.Map[j] = new Dictionary<int, Chip>();
					}
					waferMap.Map[j][i] = new Chip
					{
						RowIndex = i,
						ColumnIndex = j,
						State = state,

						PositionX = CalculateChipPositionX(referenceChipPositionX, j, referenceIndexY, chipSizeX, idealPitchX),
						PositionY = CalculateChipPositionY(referenceChipPositionY, i, referenceIndexX, chipSizeY, idealPitchY)
					};
				}
			}
		}
	}
}