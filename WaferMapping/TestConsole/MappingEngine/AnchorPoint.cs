namespace FrameOfSystem3.Work.WaferMap.MappingEngine
{
    public class AnchorPoint
    {
        public AnchorPoint(int col, int row, double x, double y)
        {
            Col = col;
            Row = row;
            X = x;
            Y = y;
        }

        public int Col { get; set; }
        public int Row { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
