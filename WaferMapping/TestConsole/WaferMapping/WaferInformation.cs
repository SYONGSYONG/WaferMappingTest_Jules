using System.Collections.Generic;

namespace WaferMapping.Engine
{
    public class WaferInformation
    {
        public WaferInformation(int rows, int columns)
        {
            this.Rows = rows;
            this.Columns = columns;
            this.Map = new Dictionary<int, Dictionary<int, Chip>>();
        }

        public int Rows { get; set; }
        public int Columns { get; set; }

        /// <summary>
        /// Key : Col, Value : Dictionary with key as Row and value as Chip
        /// </summary>
        public Dictionary<int, Dictionary<int, Chip>> Map { get; set; }

        public Chip GetChip(int col, int row)
        {
            if (Map.ContainsKey(col) && Map[col].ContainsKey(row))
            {
                return Map[col][row];
            }
            return null;
        }

        public void AddChip(Chip chip)
        {
            if (!Map.ContainsKey(chip.ColumnIndex))
            {
                Map[chip.ColumnIndex] = new Dictionary<int, Chip>();
            }
            Map[chip.ColumnIndex][chip.RowIndex] = chip;
        }
    }
}
