namespace WaferMapping.Engine
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

        /// <summary>
        /// 0 : Not Exist, 1: Exist
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// EncoderPositionX
        /// </summary>
        public double PositionX { get; set; }

        /// <summary>
        /// EncoderPositionY
        /// </summary>
        public double PositionY { get; set; }

        public override string ToString()
        {
            return $"Chip[{ColumnIndex},{RowIndex}] State={State} Pos=({PositionX:F3}, {PositionY:F3})";
        }
    }
}
