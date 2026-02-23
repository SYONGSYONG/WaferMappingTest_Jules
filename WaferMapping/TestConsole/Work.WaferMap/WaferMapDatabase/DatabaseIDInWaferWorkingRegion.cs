namespace FrameOfSystem3.Work.WaferMap.WaferMapDatabase
{
    public class DatabaseIDInWaferWorkingRegion
    {
        DatabaseIDInWaferWorkingRegion()
        {

        }

        public DatabaseIDInWaferWorkingRegion(string regionName, string waferInformationDatabaseID = "")
        {
            RegionName = regionName;
            WaferInformationDatabaseID = waferInformationDatabaseID;
        }

        public string RegionName { get; private set; }
        
        public string WaferInformationDatabaseID { get; set; }
    }
}
