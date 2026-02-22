using System;
using System.Collections.Concurrent;

namespace FrameOfSystem3.Work.WaferMap.WaferMapDatabase.InnerDocument
{
    [Serializable]
    public class UnitInformation
    {
        public enum VisitedStatusType
        {
            NotVisited,
            IndirectlyVisited,
            Visited,
        }

        UnitInformation()
        {
            //AlignsForPickUp = new ConcurrentDictionary<int, AlignForPickUp>();
            
            PickerIndex = -1;
        }

        public UnitInformation(string waferID, IPointXY unitIndex)
            : this()
        {
            WaferID = waferID;
            UnitIndex = unitIndex;
        }

        public string WaferID { get; set; }

        /// <summary>
        /// Supply Stage : Dry Run에 사용됨(실제로는 AxisPositionAppiedAlignResult을 보지만, 얼라인 결과를 적용하지 않기 때문에)
        /// Sorting Stage : 실제 작업에 사용됨
        /// </summary>
        public DPointXY AxisPositionForDryRun { get; set; }

        /// <summary>
        /// Supply Stage : 자재 로딩 직후에는 AxisPositionForDryRun와 동일한 값을 가지며, 각 Chip을 거쳐간 이후에는 Align 결과가 반영되어있다.
        /// </summary>
        public DPointXY AxisPositionAppiedAlignResult { get; set; }

        public VisitedStatusType VisitedStatus {get;set;}
        
        public DPointXYT OriginWaferVisionResult { get; set; }

        /// <summary>
        /// Camera Angle 보상이 적용된 WaferVision Result
        /// </summary>
        public DPointXYT AlignResultForPickUp { get; set; }

        public IPointXY UnitIndex { get; private set; }
        public bool IsReference { get; private set; }

        public int WorkingState { get; set; }
        public string Bincode { get;  set; }

        /// <summary>
        /// 2shot등을 위해 만든 것. (2024.05.28. 현재 사용안함)
        /// </summary>
        //public ConcurrentDictionary<int, AlignForPickUp> AlignsForPickUp { get; set; }

        public int PickerIndex { get; set; }

        ///// <summary>
        ///// 1. Picker로부터 Picker이 완료되는 순간 생성 및 업데이트
        ///// </summary>
        //public PickUp PickUp { get; set; }

        ///// <summary>
        ///// 1. Picker로부터 Bonding이 완료되는 순간 생성 및 업데이트
        ///// </summary>
        //public Bonding Bonding { get; set; }

		/// <summary>
		/// 2025.03.19. by shkim. [ADD] MotionProfiling Data ID 추가 (Wafer 전체로 생각하면 문서 하나의 크기가 너무 커질 수 있기에 ID로 관리한다.)
		/// </summary>
		public string MotionProfileDataID { get; set; }

		public void SetReference(bool isReference)
        {
            IsReference = isReference;
        }

        public void ChangeUnitIndex(IPointXY index)
        {
            UnitIndex = index;
        }

        public static void SetAxisPositionForDryRun(UnitInformation unitInformation, DPointXY position)
        {
            unitInformation.AxisPositionForDryRun = position;
            unitInformation.AxisPositionAppiedAlignResult = position;
        }

        //public static void CopyPickupInformationToBondingUnit(ref UnitInformation pickedUnit, ref UnitInformation bondingUnit)
        //{
        //    if(pickedUnit == null || bondingUnit == null)
        //    {
        //        return;
        //    }

        //    bondingUnit.AlignResultForPickUp = pickedUnit.AlignResultForPickUp;
        //    bondingUnit.AlignsForPickUp = ObjectSerializer.DeepCopySerializableObject(pickedUnit.AlignsForPickUp);
        //    bondingUnit.PickUp = ObjectSerializer.DeepCopySerializableObject(pickedUnit.PickUp);

        //    pickedUnit.Bonding = ObjectSerializer.DeepCopySerializableObject(bondingUnit.Bonding);
        //}
    }
}
