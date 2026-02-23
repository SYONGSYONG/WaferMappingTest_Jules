using System;
using System.Collections.Concurrent;
using Define.DefineEnumProject.WaferMap;
using FrameOfSystem3.Work.WaferMap.WaferMapDatabase.InnerDocument;

namespace FrameOfSystem3.Work.WaferMap.WaferMapDatabase
{
    [Serializable]
    public class WaferInformation
    {
        WaferInformation()
        {
            SplitedCoreChips = new ConcurrentDictionary<int, int>();
            UnitInformations = new ConcurrentDictionary<int, ConcurrentDictionary<int, UnitInformation>>();
            CreatedTimeOfDocument = DateTime.Now;
        }

        public WaferInformation(string waferID)
            : this()
        {
            WaferID = waferID;
        }

        #region <Properties>

        public DateTime CreatedTimeOfDocument { get; set; }

        public string WaferID {
            get
            {
                return _waferId;
            }
            set
            {
                if (_waferId != value)
                {
                    _waferId = value;
                }
            }
        }

        // 2024.06.16. [MOD] 자재 투입될 때, 배출 시간이 초기값으로 초기화되도록.
        public DateTime InTime
        {
            get => _inTime;
            set
            {
                if (value.Kind != DateTimeKind.Local)
                {
                    _inTime = DateTime.SpecifyKind(value, DateTimeKind.Local);
                }
                else
                {
                    _inTime = value;
                }

                if (OutTime != DateTime.MinValue)
                {
                    OutTime = DateTime.MinValue;
                }
            }
        }

        public DateTime OutTime
        {
            get => _outTime;
            set
            {
                if (value.Kind != DateTimeKind.Local)
                {
                    _outTime = DateTime.SpecifyKind(value, DateTimeKind.Local);
                }
                else
                {
                    _outTime = value;
                }
            }
        }


        public DateTime ProcessRegionInTime
        {
            get
            {
                return _porcessReigionInTime;
            }
            set
            {
                if (value.Kind != DateTimeKind.Local)
                {
                    _porcessReigionInTime = DateTime.SpecifyKind(value, DateTimeKind.Local);
                }
                else
                {
                    _porcessReigionInTime = value;
                }
            }
        }
        public DateTime ProcessRegionOutTime
        {
            get
            {
                return _porcessReigionOutTime;
            }
            set
            {
                if (value.Kind != DateTimeKind.Local)
                {
                    _porcessReigionOutTime = DateTime.SpecifyKind(value, DateTimeKind.Local);
                }
                else
                {
                    _porcessReigionOutTime = value;
                }
            }
        }

        public IPointXY ArrayCount
        {
            get => _arrayCount;
            private set
            {
                if (_arrayCount != value)
                {
                    _arrayCount = value;
                }
            }
        }

        public string STEPID { get; set; }
        public string EQPID { get; set; }
        public string PARTID { get; set; }

        public string RingId { get; set; }

        /// <summary>
        /// EFEM으로부터 수신되었던 RingId
        /// </summary>
        public string BackupRingId { get; set; }

        public string PortId { get; set; }
        public string SlotId { get; set; }
        public string LotID { get; set; }


        /// <summary>
        /// SEMI05 사양상으로는 Wafer 하단 Notch 기준으로 0도로 하고, 시계방향으로 회전한다.
        /// 고객사 별로 상이할 수 있음! 설비는 SEMI를 따라가고, 고객사에 맞춰 컨버팅하자.
        /// 1) 2024.07.11. 삼성 PMS 기준
        ///     - Ring Notch 상단 기준으로 Wafer Notch 하단이 0도. 시계 반대 방향으로 각도 변경된다.
        /// </summary>
        public int WorkAngle { get; set; }

        /// <summary>
        /// 2024.07.18. [ADD] PMS 기록에 활용된다. Core Wafer는 WorkAngle과 동일하고, Bin Wafer의 경우 Core Wafer의 정보를 입력한다.
        /// </summary>
        public int CoreWorkAngle { get; set; }


        public string RecipeID { get; set; }
        public SubstrateType SubstrateType { get; set; }

        public double UnitPitchX { get; set; }
        public double UnitPitchY { get; set; }
        public double UnitSizeX { get; set; }
        public double UnitSizeY { get; set; }

        /// <summary>
        /// 다음으로 이동할 위치
        /// </summary>
        public int NextMoveIndexX { get; set; } = -1;
        public int NextMoveIndexY { get; set; } = -1;

        /// <summary>
        /// 마지막으로 이동이 완료된 위치
        /// </summary>
        public double LastPositionX { get; set; }
        public double LastPositionY { get; set; }
        public double LastPositionT { get; set; }

        /// <summary>
        /// 현재 작업할 Unit Index (마지막으로 이동된 위치와 동일)
        /// </summary>
        public int WorkingIndexX { get; set; } = -1;
        public int WorkingIndexY { get; set; } = -1;

        /// <summary>
        /// 마지막 작업이 완료된 Unit Index (Supply : PickUp, Sorting : Bonding)
        /// </summary>
        public int LastWorkedIndexX { get; set; }
        public int LastWorkedIndexY { get; set; }

        public DPointXY CalculatedPitch { get; set; }

        public string BincodeTableName { get; set; }

        public DPointXY FirstFindPositionForCalculatingLoadingAngle { get; set; }
        public DPointXY SecondFindPositionForCalculatingLoadingAngle { get; set; }

        public DPointXY CalculatedChipPitchXY { get; set; }

        /// <summary>
        /// Key : Split 순번 Index, Value : 해당 순번의 Split 수량
        /// </summary>
        public ConcurrentDictionary<int, int> SplitedCoreChips { get; set; }

        /// <summary>
        /// Key = Column, Value = Row 값을 key로 갖는 Dictionary
        /// </summary>
        public ConcurrentDictionary<int, ConcurrentDictionary<int, UnitInformation>> UnitInformations { get => _unitInformations; set => _unitInformations = value; }

        public IPointXY NotchIndex { get => _notchIndex; set => _notchIndex = value; }

        public string Bincode { get; set; }

        /// <summary>
        /// PickUpMiss 발생했을 때 Skip할 제한 수량
        /// </summary>
        public int PickUpMissSkipCount { get; set; }

        /// <summary>
        /// PBI Gap Error가 발생했을 때 Skip할 제한 수량
        /// </summary>
        public int PBIGapErrorSkipCount { get; set; }


		#region PitchMonitoringData
		public double LeftTopPitchX { get; set; }
        public double LeftTopPitchY { get; set; }
        public double RightTopPitchX { get; set; }
        public double RightTopPitchY { get; set; }
        public double LeftBottomPitchX { get; set; }
        public double LeftBottomPitchY { get; set; }
        public double RightBottomPitchX { get; set; }
        public double RightBottomPitchY { get; set; }
        public double CenterPitchX { get; set; }
        public double CenterPitchY { get; set; }
		#endregion  /PitchMonitoringData

		#endregion </Properties>

		string _waferId = string.Empty;
        DateTime _inTime = DateTime.MinValue;
        DateTime _outTime = DateTime.MinValue;

        DateTime _porcessReigionInTime = DateTime.MinValue;
        DateTime _porcessReigionOutTime = DateTime.MinValue;

        IPointXY _arrayCount;
        IPointXY _notchIndex = new IPointXY(-1, -1);
        ConcurrentDictionary<int, ConcurrentDictionary<int, UnitInformation>> _unitInformations = null;

        public bool ChangeArrayCount(int arrayCountX, int arrayCountY, bool forcedUpdateMap = false, string backupWaferID = "")
        {
            if (ArrayCount.x != arrayCountX || ArrayCount.y != arrayCountY || forcedUpdateMap)
            {
                _arrayCount.x = arrayCountX;
                _arrayCount.y = arrayCountY;


                //if(UnitInformations == null)  // 2024.03.25. by shkim. [DEL] 칩 어레이가 변경되었으니 Unitinformation을 초기화하고 재생성해줘야한다.
                {
                    UnitInformations = new ConcurrentDictionary<int, ConcurrentDictionary<int, UnitInformation>>();
                }

                for (int columnIndex = 1; columnIndex <= arrayCountX; columnIndex++)
                {
                    var rowDictionary = new ConcurrentDictionary<int, UnitInformation>();
                    UnitInformations.AddOrUpdate(columnIndex, rowDictionary, (k, v) => v = rowDictionary);

                    for (int rowIndex = 1; rowIndex <= arrayCountY; rowIndex++)
                    {
                        UnitInformation unit;
                        if (true == forcedUpdateMap && false == string.IsNullOrEmpty(backupWaferID))
                        {
                            unit = new UnitInformation(backupWaferID, new IPointXY(columnIndex, rowIndex));
                        }
                        else
                        {
                            unit = new UnitInformation(WaferID, new IPointXY(columnIndex, rowIndex));
                        }

                        rowDictionary.AddOrUpdate(rowIndex, unit, (k, v) => v = unit);
                    }
                }

                return true;
            }

            return false;
        }

        public bool GetWorkingUnitInformation(ref UnitInformation workingUnit)
        {
            if (null == UnitInformations
                || WorkingIndexX <= 0    // 2024.07.03. by shkim. [MOD] 버그 Fix. Map 좌표는 1,1부터 시작
                || WorkingIndexY <= 0    // 2024.07.03. by shkim. [MOD] 버그 Fix. Map 좌표는 1,1부터 시작
                || UnitInformations.Count < WorkingIndexX
                || UnitInformations[WorkingIndexX].Count < WorkingIndexY)
            {
                return false;
            }
            workingUnit = UnitInformations[WorkingIndexX][WorkingIndexY];

            return (workingUnit != null);
        }

        public bool GetLastWorkedUnitInformation(ref UnitInformation lastWorkedUnit)
        {
            if (null == UnitInformations
                || LastWorkedIndexX <= 0    // Map 좌표는 1,1 부터 시작
                || LastWorkedIndexY <= 0
                || UnitInformations.Count < LastWorkedIndexX
                || UnitInformations[LastWorkedIndexX].Count < LastWorkedIndexY)
            {
                return false;
            }
            lastWorkedUnit = UnitInformations[LastWorkedIndexX][LastWorkedIndexY];

            return (lastWorkedUnit != null);
        }

        public UnitInformation GetUnitInformation(int columnIndex, int rowIndex)
        {
            if (null == UnitInformations
                || UnitInformations.Count < columnIndex
                || columnIndex <= 0 || rowIndex <= 0    // 2024.03.29. by shkim. [ADD] 예외처리추가
                || UnitInformations[columnIndex].Count < rowIndex)
            {
                return null;
            }

            UnitInformation unitInformation = UnitInformations[columnIndex][rowIndex];

            return unitInformation;
        }

        public bool ChangeJobState(int columnIndex, int rowIndex, int jobType)
        {
            if (null == UnitInformations
                || columnIndex <= 0     // 2024.07.03. by shkim. [ADD] 버그 Fix. Map 좌표는 1,1부터 시작
                || rowIndex <= 0        // 2024.07.03. by shkim. [ADD] 버그 Fix. Map 좌표는 1,1부터 시작
                || UnitInformations.Count < columnIndex
                || UnitInformations[columnIndex].Count < rowIndex)
            {
                return false;
            }

            UnitInformations[columnIndex][rowIndex].WorkingState = jobType;

            return true;
        }

        public bool IsValidNextIndexForJog(int offsetIndexX, int offsetIndexY)
        {
            int targetIndexX = WorkingIndexX + offsetIndexX;
            int targetIndexY = WorkingIndexY + offsetIndexY;

            bool isValidX = targetIndexX > 0 && targetIndexX <= ArrayCount.x;
            bool isValidY = targetIndexY > 0 && targetIndexY <= ArrayCount.y;

            return isValidX && isValidY;
        }

        public void SetDefaultUnitInformation(string recipeId, double unitSizeX, double unitSizeY, double unitPitchX, double unitPitchY)
        {
            RecipeID = recipeId;
            UnitSizeX = unitSizeX;
            UnitSizeY = unitSizeY;
            UnitPitchX = unitPitchX;
            UnitPitchY = unitPitchY;
        }

        public void SetWorkAngle(int workAngle, int coreWaferWorkAngle)
        {
            WorkAngle = workAngle;
            CoreWorkAngle = coreWaferWorkAngle;
        }

        public bool RotateMap(int currentAngle, int targetAngle)
        {
            int colCount = ArrayCount.x, rowCount = ArrayCount.y;

            Action<UnitInformation, int, int> indexChangeAction = new Action<UnitInformation, int, int>((unit, col, row) =>
            {
                unit.ChangeUnitIndex(new IPointXY(col, row));
            });


            if (false == LogicForWaferMap.Rotate<UnitInformation, ConcurrentDictionary<int, ConcurrentDictionary<int, UnitInformation>>>(ref _unitInformations
                , ref colCount, ref rowCount, currentAngle, targetAngle, indexChangeAction))
            {
                return false;
            }

            ArrayCount = new IPointXY(colCount, rowCount);

            return true;
        }

        public bool AddRowMap(string bincode, int workingState)
        {
            int newRowCount = ArrayCount.y + 1;
            for (int col = 1; col <= ArrayCount.x; col++)
            {
                ConcurrentDictionary<int, UnitInformation> rowUnits = null;
                if (false == _unitInformations.TryGetValue(col, out rowUnits)
                    || rowUnits == null)
                {
                    return false;
                }
                UnitInformation unit = null;
                if (true == rowUnits.TryGetValue(newRowCount, out unit))
                {
                    return false;
                }
                unit = new UnitInformation(_waferId, new IPointXY(col, newRowCount));
                unit.Bincode = bincode;
                unit.WorkingState = workingState;
                rowUnits.TryAdd(newRowCount, unit);
            }
            ArrayCount = new IPointXY(ArrayCount.x, newRowCount);
            return true;
        }
        public bool RemoveRowMap()
        {
            for (int col = 1; col <= ArrayCount.x; col++)
            {
                ConcurrentDictionary<int, UnitInformation> rowUnits = null;
                if (false == _unitInformations.TryGetValue(col, out rowUnits)
                    || rowUnits == null)
                {
                    return false;
                }
                UnitInformation unit = null;
                if (false == rowUnits.TryRemove(ArrayCount.y, out unit))
                {
                    return false;
                }
            }
            ArrayCount = new IPointXY(ArrayCount.x, ArrayCount.y - 1);
            return true;
        }
        public bool AddColMap(string bincode, int workingState)
        {
            int newColCount = ArrayCount.x + 1;
            ConcurrentDictionary<int, UnitInformation> rowUnits = null;
            if (true == _unitInformations.TryGetValue(newColCount, out rowUnits))
            {
                return false;
            }
            rowUnits = new ConcurrentDictionary<int, UnitInformation>();
            _unitInformations.TryAdd(newColCount, rowUnits);

            for (int row = 1; row < ArrayCount.y; row++)
            {
                UnitInformation unit = new UnitInformation(_waferId, new IPointXY(newColCount, row));
                unit.Bincode = bincode;
                unit.WorkingState = workingState;
                rowUnits.TryAdd(row, unit);
            }
            ArrayCount = new IPointXY(newColCount, ArrayCount.y);
            return true;
        }

        public bool ChangeWaferIDWithChangingUnitWaferID(string waferID)
        {
            for (int columnIndex = 1; columnIndex <= _arrayCount.x; columnIndex++)
            {
                ConcurrentDictionary<int, UnitInformation> rowDictionary = null;
                if (false == UnitInformations.TryGetValue(columnIndex, out rowDictionary))
                {
                    continue;
                }

                for (int rowIndex = 1; rowIndex <= _arrayCount.y; rowIndex++)
                {
                    UnitInformation unit;
                    unit = GetUnitInformation(columnIndex, rowIndex);
                    if(unit != null)
                    {
                        unit.WaferID = waferID;
                    }
                }
            }
            WaferID = waferID;

            return true;
        }
    }
}
