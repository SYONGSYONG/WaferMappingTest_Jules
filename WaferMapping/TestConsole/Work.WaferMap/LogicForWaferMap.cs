using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Define.DefineEnumProject.WaferMap;

namespace FrameOfSystem3.Work.WaferMap
{
    class LogicForWaferMap
    {
        static int[] DirectionOffsetArrayX
        {
            get
            {
                return _directionOffsetX;
            }
        }
        static int[] DirectionOffsetArrayY
        {
            get
            {
                return _directionOffsetY;
            }
        }

        static int[] _directionOffsetX = new int[] { -1, 0, 1, 0, -1, 1, -1, 1};
        static int[] _directionOffsetY = new int[] { 0, 1, 0, -1, -1, -1, 1, 1 };

        public enum NeighboringUnitType
        {
            Left = 0,
            Bottom = 1,// Top = 1,
            Right = 2,
            Top =3,// Bottom = 3,

            TopLeft = 4,
            TopRight = 5,
            BottomLeft = 6,
            BottomRight = 7,
        }

        //public delegate void DelegateSetUnitPosition<T>(T targetObject, DPointXY position);
        //public delegate bool DelegateSetAlignResultToNeighboringUnit<T>(NeighboringUnitType neighboringType, T targetObject, DPointXYT alignResult);


        /// <summary>
        /// 특정 유닛 인덱스의 좌우상하 유닛을 찾는다.
        /// </summary>
        public static bool FindNeighboringUnit<T, TArray2D>(TArray2D array, IPointXY referenceUnitIndex, NeighboringUnitType neighboringUnitType, ref T targetObject)
        {
            //int[] dX = { -1, 0, 1, 0 }; // 행에 대한 이동 방향
            //int[] dY = { 0, 1, 0, -1 }; // 열에 대한 이동 방향

            int[] dX = DirectionOffsetArrayX; // 행에 대한 이동 방향
            int[] dY = DirectionOffsetArrayY; // 열에 대한 이동 방향

            return GetTargetObject(referenceUnitIndex.x + dX[(int)neighboringUnitType], referenceUnitIndex.y + dY[(int)neighboringUnitType], array, ref targetObject);
        }

        #region PositionLogic

        /// <summary>
        /// 특정 인덱스의 상하좌우 유닛을 찾아 조건(조건에 대한 연산은 콜백함수에서 수행)에 따라 얼라인 결과를 함께 적용한다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TArray2D"></typeparam>
        /// <param name="referenceUnitIndex"></param>
        /// <param name="alignResult"></param>
        /// <param name="dictionary2DArray"></param>
        /// <param name="callbackApplyAlignResult"></param>
        public static void UpdateNeighboringUnitPosition<T, TArray2D>(IPointXY referenceUnitIndex, TArray2D dictionary2DArray
                                                    , Action<T, NeighboringUnitType> applyFunction)
        {
            int[] dX = { -1, 0, 1, 0 }; // 행에 대한 이동 방향
            int[] dY = { 0, -1, 0, 1 }; // 열에 대한 이동 방향

            T targetObject = default(T);
            if (true == GetTargetObject(referenceUnitIndex.x + dX[(int)NeighboringUnitType.Left], referenceUnitIndex.y, dictionary2DArray, ref targetObject))
            {
                applyFunction(targetObject, NeighboringUnitType.Left);
            }
            if (true == GetTargetObject(referenceUnitIndex.x + dX[(int)NeighboringUnitType.Right], referenceUnitIndex.y, dictionary2DArray, ref targetObject))
            {
                applyFunction(targetObject, NeighboringUnitType.Right);
            }
            if (true == GetTargetObject(referenceUnitIndex.x + dX[(int)NeighboringUnitType.Top], referenceUnitIndex.y, dictionary2DArray, ref targetObject))
            {
                applyFunction(targetObject, NeighboringUnitType.Top);
            }
            if (true == GetTargetObject(referenceUnitIndex.x + dX[(int)NeighboringUnitType.Bottom], referenceUnitIndex.y, dictionary2DArray, ref targetObject))
            {
                applyFunction(targetObject, NeighboringUnitType.Bottom);
            }


            //int[] dX = { -1, 0, 1, 0 }; // 행에 대한 이동 방향
            //int[] dY = { 0, 1, 0, -1 }; // 열에 대한 이동 방향

            //T targetObject = default(T);
            //if (true == GetTargetObject(referenceUnitIndex.x + dX[(int)NeighboringUnitType.Left], referenceUnitIndex.y, dictionary2DArray, ref targetObject))
            //{
            //    applyFunction(targetObject, NeighboringUnitType.Left);
            //}
            //if (true == GetTargetObject(referenceUnitIndex.x + dX[(int)NeighboringUnitType.Right], referenceUnitIndex.y, dictionary2DArray, ref targetObject))
            //{
            //    applyFunction(targetObject, NeighboringUnitType.Right);
            //}
            //if (true == GetTargetObject(referenceUnitIndex.x + dX[(int)NeighboringUnitType.Top], referenceUnitIndex.y, dictionary2DArray, ref targetObject))
            //{
            //    applyFunction(targetObject, NeighboringUnitType.Top);
            //}
            //if (true == GetTargetObject(referenceUnitIndex.x + dX[(int)NeighboringUnitType.Bottom], referenceUnitIndex.y, dictionary2DArray, ref targetObject))
            //{
            //    applyFunction(targetObject, NeighboringUnitType.Bottom);
            //}
        }

        /// <summary>
        /// Wafer(or Stage)의 중심 절대 위치를 기준으로 각 Unit의 물리적 절대 위치를 계산한다.
        /// isSelfMovingX, Y
        /// Unit 자체가 움직이는 경우 (Ex. 스테이지에 올라간 Wafer) => 1
        /// 바라보는 대상이 움직이는 경우 (Ex. Camera) => -1
        /// </summary>
        /// <param name="isSelfMovingX"></param>
        /// <param name="isSelfMovingY"></param>
        /// <returns></returns>
        public static bool CalculateUnitPositionByCenterPosition<T, TArray2D>(IPointXY arrayCount
                                                                , DPointXY unitSize
                                                                , DPointXY unitPitch
                                                                , DPointXY centerPosition
                                                                , TArray2D dictionary2DArray
                                                                , Action<T, DPointXY> callbackSetUnitPosition   // , DelegateSetUnitPosition<T> callbackSetUnitPosition
                                                                , bool isSelfMovingX = true
                                                                , bool isSelfMovingY = true)
        {
            double reverseFactorX = (false == isSelfMovingX) ? -1 : 1;
            double reverseFactorY = (false == isSelfMovingY) ? -1 : 1;

            DPointXY firstUnitPosition;

            if (arrayCount.x % 2 != 0)
            {
                firstUnitPosition.x = centerPosition.x
                    + (unitSize.x * (arrayCount.x - 1) + unitPitch.x * (arrayCount.x - 1)) / 2.0 * reverseFactorX;
            }
            else
            {
                firstUnitPosition.x = centerPosition.x
                    + (unitSize.x + unitPitch.x) * (arrayCount.x - 1) / 2.0 * reverseFactorX;
            }

            if (arrayCount.y % 2 != 0)
            {
                firstUnitPosition.y = centerPosition.y
                    - (unitSize.y * (arrayCount.y - 1) + unitPitch.y * (arrayCount.y - 1)) / 2.0 * reverseFactorY;
            }
            else
            {
				// 2024.03.21. [MOD] 문병철 과장이 수정 bug fix
                firstUnitPosition.y = centerPosition.y
                    - (unitSize.y + unitPitch.y) * (arrayCount.y - 1) / 2.0 * reverseFactorY;
				// 2024.03.21. [END] 문병철 과장이 수정 bug fix
            }

            for (int columnIndex = 1; columnIndex <= arrayCount.x; columnIndex++)
            {
                for (int rowIndex = 1; rowIndex <= arrayCount.y; rowIndex++)
                {
                    DPointXY calculatedPosition;

                    calculatedPosition.x = firstUnitPosition.x - (unitSize.x + unitPitch.x) * (columnIndex - 1) * reverseFactorX;
                    calculatedPosition.y = firstUnitPosition.y + (unitSize.y + unitPitch.y) * (rowIndex - 1) * reverseFactorY;
                    // calculatedPosition.t = centerPosition.t;

                    T targetObject = default(T);
                    if (false == GetTargetObject<T, TArray2D>(columnIndex, rowIndex, dictionary2DArray, ref targetObject))
                    {
                        return false;
                    }
                    callbackSetUnitPosition(targetObject, calculatedPosition);
                }
            }

            return true;
        }

        /// <summary>
        /// 특정 인덱스의 위치를 기준으로 각 유닛의 위치를 계산한다.
        /// isSelfMovingX, Y
        /// Unit 자체가 움직이는 경우 (Ex. 스테이지에 올라간 Wafer) => 1
        /// 바라보는 대상이 움직이는 경우 (Ex. Camera) => -1
        /// </summary>
        public static bool CalculateUnitPositionByReferenceIndexPosition<T, TArray2D>(IPointXY arrayCount
            , DPointXY unitSize
            , DPointXY unitPitch
            , IPointXY referenceIndex
            , DPointXY referencePosition
            , TArray2D dictionary2DArray
            , Action<T, DPointXY> callbackSetUnitPosition
            , bool isSelfMovingX = true
            , bool isSelfMovingY = true
            )
        {
            // Unit 자체가 움직이는 경우 (Ex. 스테이지에 올라간 Wafer) => 1
            // 바라보는 대상이 움직이는 경우 (Ex. Camera) => -1
            double reverseFactorX = (false == isSelfMovingX) ? -1 : 1;
            double reverseFactorY = (false == isSelfMovingY) ? -1 : 1;

            DPointXY firstUnitPosition;
            firstUnitPosition.x = referencePosition.x
                    + (unitSize.x + unitPitch.x) * (referenceIndex.x - 1) * reverseFactorX;
					// 2024.03.21. [MOD] 문병철 과장이 수정 bug fix
            firstUnitPosition.y = referencePosition.y
                    - (unitSize.y + unitPitch.y) * (referenceIndex.y - 1) * reverseFactorY;
					// 2024.03.21. [END] 문병철 과장이 수정 bug fix

            for (int columnIndex = 1; columnIndex <= arrayCount.x; columnIndex++)
            {
                for (int rowIndex = 1; rowIndex <= arrayCount.y; rowIndex++)
                {
                    DPointXY calculatedPosition;

                    calculatedPosition.x = firstUnitPosition.x - (unitSize.x + unitPitch.x) * (columnIndex - 1) * reverseFactorX;
                    calculatedPosition.y = firstUnitPosition.y + (unitSize.y + unitPitch.y) * (rowIndex - 1) * reverseFactorY;
                    
                    T targetObject = default(T);
                    if (false == GetTargetObject<T, TArray2D>(columnIndex, rowIndex, dictionary2DArray, ref targetObject))
                    {
                        return false;
                    }
                    callbackSetUnitPosition(targetObject, calculatedPosition);
                }
            }

            return true;
        }

        #endregion /PositionLogic

        #region FindLogic

        struct CurrentToTargetIndexDistanceInformation
        {
            public CurrentToTargetIndexDistanceInformation(IPointXY arrayIndex, double distance)
            {
                _arrayIndex = arrayIndex;
                _distance = distance;
            }

            public IPointXY ArrayIndex { get => _arrayIndex; set => _arrayIndex = value; }
            public double Distance { get => _distance; set => _distance = value; }

            IPointXY _arrayIndex;
            double _distance;
        }

        #region 삭제예정_2024.07.30. 사용안함
//public static bool SearchPathIncludingWaypointsWithinLimitDistanceToTarget<T, TArray2D>(TArray2D dictionary2DArray
        //    , IPointXY currentIndex
        //    , IPointXY targetIndex
        //    , ref List<IPointXY> wayPoints
        //    , int limitDistance
        //    , DelegateFindCallback<T> callbackFindCondition)
        //{
        //    if (wayPoints == null)
        //    {
        //        wayPoints = new List<IPointXY>();
        //    }

        //    double calculatedDistance = 0.0;

        //    IPointXY roiArrayStartIndex;
        //    IPointXY roiArrayEndIndex;

        //    bool isTargetIndexXDirectionRight = false;
        //    bool isTargetIndexYDirectionBottom = false;

        //    calculatedDistance = Math.Sqrt(Math.Pow(currentIndex.x - targetIndex.x, 2) + Math.Pow(currentIndex.y - targetIndex.y, 2));
        //    if (calculatedDistance <= limitDistance) // 이 조건이 안되니까 여기로 들어왔을텐데...
        //    {
        //        wayPoints.Add(targetIndex);
        //        return true;
        //    }

        //    //IPointXY fathestPoint;

        //    isTargetIndexXDirectionRight = currentIndex.x <= targetIndex.x;
        //    isTargetIndexYDirectionBottom = currentIndex.y <= targetIndex.y;

        //    roiArrayStartIndex.x = isTargetIndexXDirectionRight ? currentIndex.x : targetIndex.x;
        //    roiArrayStartIndex.y = isTargetIndexYDirectionBottom ? currentIndex.y : targetIndex.y;

        //    roiArrayEndIndex.x = isTargetIndexXDirectionRight ? targetIndex.x : currentIndex.x;
        //    roiArrayEndIndex.y = isTargetIndexYDirectionBottom ? targetIndex.y : currentIndex.y;

        //    CurrentToTargetIndexDistanceInformation maxDistancePoint = new CurrentToTargetIndexDistanceInformation(new IPointXY(currentIndex), 0);

        //    for (int columnIndex = roiArrayStartIndex.x; columnIndex <= roiArrayEndIndex.x; columnIndex++)
        //    {
        //        for (int rowIndex = roiArrayStartIndex.y; rowIndex <= roiArrayEndIndex.y; rowIndex++)
        //        {
        //            T targetObject = default(T);
        //            if (false == GetTargetObject(columnIndex, rowIndex, dictionary2DArray, ref targetObject))
        //            {
        //                return false;
        //            }
        //            // 적어도 칩이 존재하는 위치여야 경유가 가능하다.
        //            if (callbackFindCondition(targetObject))
        //            {
        //                double distance = Math.Sqrt(Math.Pow(columnIndex - targetIndex.x, 2) + Math.Pow(rowIndex - targetIndex.y, 2));
        //                if (distance < limitDistance && maxDistancePoint.Distance <= distance)
        //                {
        //                    maxDistancePoint.ArrayIndex = new IPointXY(columnIndex, rowIndex);
        //                    maxDistancePoint.Distance = distance;
        //                }
        //            }
        //        }
        //    }

        //    return false;
        //}

        //static bool FindFarthestWayPoints<T, TArray2D>(TArray2D dictionary2DArray
        //    , IPointXY currentIndex
        //    , IPointXY targetIndex
        //    , double limitDistance
        //    , out IPointXY fathestPoint
        //    , DelegateFindCallback<T> callbackFindCondition
        //    , ref List<IPointXY> wayPoints)
        //{
        //    CurrentToTargetIndexDistanceInformation maxDistancePoint = new CurrentToTargetIndexDistanceInformation(new IPointXY(currentIndex), 0);

        //    IPointXY roiArrayEndIndex;
        //    IPointXY roiArrayStartIndex;

        //    bool isTargetIndexXDirectionRight = false;
        //    bool isTargetIndexYDirectionBottom = false;

        //    fathestPoint = targetIndex;

        //    isTargetIndexXDirectionRight = currentIndex.x <= targetIndex.x;
        //    isTargetIndexYDirectionBottom = currentIndex.y <= targetIndex.y;

        //    roiArrayStartIndex.x = isTargetIndexXDirectionRight ? currentIndex.x : targetIndex.x;
        //    roiArrayStartIndex.y = isTargetIndexYDirectionBottom ? currentIndex.y : targetIndex.y;

        //    roiArrayEndIndex.x = isTargetIndexXDirectionRight ? targetIndex.x : currentIndex.x;
        //    roiArrayEndIndex.y = isTargetIndexYDirectionBottom ? targetIndex.y : currentIndex.y;

        //    for (int columnIndex = roiArrayStartIndex.x; columnIndex <= roiArrayEndIndex.x; columnIndex++)
        //    {
        //        for (int rowIndex = roiArrayStartIndex.y; rowIndex <= roiArrayEndIndex.y; rowIndex++)
        //        {
        //            double distance = Math.Sqrt(Math.Pow(columnIndex - targetIndex.x, 2) + Math.Pow(rowIndex - targetIndex.y, 2));
        //            if (distance < limitDistance && maxDistancePoint.Distance <= distance && columnIndex != currentIndex.x && rowIndex != currentIndex.y)
        //            {
        //                maxDistancePoint.ArrayIndex = new IPointXY(columnIndex, rowIndex);
        //                maxDistancePoint.Distance = distance;
        //            }
        //        }
        //    }

        //    // wayPoints.Add(maxDistancePoint.ArrayIndex);

        //    if (maxDistancePoint.ArrayIndex.x == targetIndex.x && maxDistancePoint.ArrayIndex.y == targetIndex.y)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return FindFarthestWayPoints(dictionary2DArray, currentIndex, targetIndex, limitDistance, out fathestPoint, callbackFindCondition, ref wayPoints);
        //    }
        //}
#endregion


        /// <summary>
        /// Array 시작좌표 (0,0 or 1,1) 부터 순차탐색하며 조건에 맞을 때 Action을 수행한다.
        /// </summary>
        public static bool SequentialSearch<T, TArray2D>(TArray2D dictionary2DArray
            , int dimension2D_X
            , int dimension2D_Y
            , Action<T>[] actionForTarget
            , IPointXY arrayIndexStartOffset = new IPointXY()
           , params Func<T, bool>[] conditions)
        {
            IPointXY arrayCountXY;
            arrayCountXY.x = dimension2D_X;
            arrayCountXY.y = dimension2D_Y;

            int actionCount = actionForTarget == null ? 0 : actionForTarget.Length;

            for (int row = 0; row < arrayCountXY.y; row++)
            {
                for (int col = 0; col < arrayCountXY.x; col++)
                {
                    T targetObject = default(T);
                    if (false == GetTargetObject(col + arrayIndexStartOffset.x, row + arrayIndexStartOffset.y, dictionary2DArray, ref targetObject))
                    {
                        return false;
                    }

                    if (true == CheckConditions(targetObject, conditions))
                    {
                        // 2025.01.14. by shkim. [MOD] Parallel -> For로 변경 (원인을 알 수 없이 Upload된 Map에 Unit이 1개 비어있는 경우가 있어서 임시조치)
                        for (int actionIndex = 0; actionIndex < actionCount; actionIndex++)
                        {
                            if (actionForTarget[actionIndex] != null)
                            {
                                actionForTarget[actionIndex](targetObject);
                            }
                        }

                        //Parallel.For(0, actionCount, (i) =>
                        //{
                        //    if (actionForTarget[i] != null)
                        //    {
                        //        actionForTarget[i](targetObject);
                        //    }
                        //});

                        // 2025.01.14. by shkim. [END]
                    }
                }
            }

            return true;
        }

        #region <ForTraverseZigZag>

        static bool CheckConditions<T>(T targetObject, params Func<T, bool>[] conditions)
        {
            int conditionCount = conditions == null ? 0 : conditions.Length;

            if (conditionCount > 0)
            {
                foreach (var condition in conditions)
                {
                    if (false == condition(targetObject))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// T의 타입이 TArray2D 타입에 담겨 있을 때 callbackFindCondition 조건에 충족하는 column, row를 찾는다.
        /// arrayIndexStartOffset => Map이 1,1부터 시작하면 1,1 을 넣어준다.
        /// </summary>
        public static bool TraverseZigZagToFindTarget<T, TArray2D>(TArray2D dictionary2DArray
           , int dimension2D_X
           , int dimension2D_Y
           , ref IPointXY findIndexXY
           , SEARCH_START_DIRECTION startSeachDirection
           , IPointXY arrayIndexStartOffset = new IPointXY()
           , params Func<T, bool>[] conditions)
        {
            IPointXY arrayCountXY;
            arrayCountXY.x = dimension2D_X;
            arrayCountXY.y = dimension2D_Y;

            IPointXY startIndexXY = new IPointXY(0, 0);
            IPointXY movingIndexOffsetXY = new IPointXY(0, 0);
            IPointXY outOfBoundCoordinateXY = new IPointXY();

            SetSearchingConditionForTraversingZigZag(startSeachDirection, ref startIndexXY, ref arrayCountXY, ref movingIndexOffsetXY, ref outOfBoundCoordinateXY);
            IPointXY travelIndexXY = new IPointXY(startIndexXY);

            // 다음 탐색 좌표가 맵 영역을 벗어나지 않을 때까지 반복
            while (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
            {
                // 맵은 1,1 보부터 시작하기에 dictionary 2d arrya의 값을 비교할 때나 최종 결과 좌표를 넘겨줄 때는 x, y 인덱스에 1을 더해준다.

                T targetObject = default(T);
                if (false == GetTargetObject(travelIndexXY.x + arrayIndexStartOffset.x, travelIndexXY.y + arrayIndexStartOffset.y, dictionary2DArray, ref targetObject))
                {
                    return false;
                }
                
                if (false == CheckConditions(targetObject, conditions))
                {
                    // 조건에 부합하지 않았기에 다음 인덱스 탐색하기 위해 movingIndexOffsetXY를 더했고,
                    travelIndexXY.x += movingIndexOffsetXY.x;
                    travelIndexXY.y += movingIndexOffsetXY.y;

                    // 다음 탐색 좌표가 맵 영역을 완전히 벗어나지 않았지만(벗어났다면 전체 맵 탐색 종료를 의미),
                    // 진행 방향의 맵을 벗어난다면, 다음 라인으로 변경
                    if (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
                    {
                        // 다음 Row로 넘어가는 조건에 해당
                        bool isLineMovedToNextLine = CheckAndSetNextLineForTraversingZigZag(startSeachDirection, arrayCountXY, ref travelIndexXY, ref movingIndexOffsetXY);
                    }
                }
                else
                {
                    findIndexXY.x = travelIndexXY.x + arrayIndexStartOffset.x;
                    findIndexXY.y = travelIndexXY.y + arrayIndexStartOffset.y;
                    return true;
                }
            }

            return false;
        }




        /// <summary>
        /// 2024.05.28. by shkim. 작업 방향의 역방향 탐색!! 마지막 작업 위치를 찾기 위함이다.
        /// T의 타입이 TArray2D 타입에 담겨 있을 때 callbackFindCondition 조건에 충족하는 column, row를 찾는다.
        /// arrayIndexStartOffset => Map이 1,1부터 시작하면 1,1 을 넣어준다.
        /// </summary>
        public static bool ReverseTraverseZigZagToFindTarget<T, TArray2D>(TArray2D dictionary2DArray
           , int dimension2D_X
           , int dimension2D_Y
           , ref IPointXY findIndexXY
           , SEARCH_START_DIRECTION startSeachDirection
           , IPointXY arrayIndexStartOffset = new IPointXY()
            , params Func<T, bool>[] targetConditions)
        {
            IPointXY arrayCountXY;
            arrayCountXY.x = dimension2D_X;
            arrayCountXY.y = dimension2D_Y;

            IPointXY startIndexXY = new IPointXY(0, 0);
            IPointXY movingIndexOffsetXY = new IPointXY(0, 0);
            IPointXY outOfBoundCoordinateXY = new IPointXY();

            SetSearchingConditionForTraversingZigZag(startSeachDirection, ref startIndexXY, ref arrayCountXY, ref movingIndexOffsetXY, ref outOfBoundCoordinateXY);
            IPointXY travelIndexXY = new IPointXY(startIndexXY);

            bool isLastWorkFindSuccess = false;

            // 다음 탐색 좌표가 맵 영역을 벗어나지 않을 때까지 반복
            while (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
            {
                // 맵은 1,1 보부터 시작하기에 dictionary 2d arrya의 값을 비교할 때나 최종 결과 좌표를 넘겨줄 때는 x, y 인덱스에 1을 더해준다.

                T targetObject = default(T);
                if (false == GetTargetObject(travelIndexXY.x + arrayIndexStartOffset.x, travelIndexXY.y + arrayIndexStartOffset.y, dictionary2DArray, ref targetObject))
                {
                    // return false;
                    return isLastWorkFindSuccess;
                }

                if (false == CheckConditions(targetObject, targetConditions))
                {
                    // 조건에 부합하지 않았기에 다음 인덱스 탐색하기 위해 movingIndexOffsetXY를 더했고,
                    travelIndexXY.x += movingIndexOffsetXY.x;
                    travelIndexXY.y += movingIndexOffsetXY.y;

                    // 다음 탐색 좌표가 맵 영역을 완전히 벗어나지 않았지만(벗어났다면 전체 맵 탐색 종료를 의미),
                    // 진행 방향의 맵을 벗어난다면, 다음 라인으로 변경
                    if (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
                    {
                        // 다음 Row로 넘어가는 조건에 해당
                        bool isLineMovedToNextLine = CheckAndSetNextLineForTraversingZigZag(startSeachDirection, arrayCountXY, ref travelIndexXY, ref movingIndexOffsetXY);
                    }
                }
                else
                {
                    findIndexXY.x = travelIndexXY.x + arrayIndexStartOffset.x;
                    findIndexXY.y = travelIndexXY.y + arrayIndexStartOffset.y;

                    travelIndexXY.x += movingIndexOffsetXY.x;
                    travelIndexXY.y += movingIndexOffsetXY.y;

                    isLastWorkFindSuccess = true;
                    // return true;

                    // 다음 탐색 좌표가 맵 영역을 완전히 벗어나지 않았지만(벗어났다면 전체 맵 탐색 종료를 의미),
                    // 진행 방향의 맵을 벗어난다면, 다음 라인으로 변경
                    if (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
                    {
                        // 다음 Row로 넘어가는 조건에 해당
                        bool isLineMovedToNextLine2 = CheckAndSetNextLineForTraversingZigZag(startSeachDirection, arrayCountXY, ref travelIndexXY, ref movingIndexOffsetXY);
                    }
                }
            }

            return isLastWorkFindSuccess;
        }



        /// <summary>
        /// T의 타입이 TArray2D 타입에 담겨 있을 때 callbackFindCondition 조건에 충족하는 잔여 수량을 찾는다. (탐색 순서에 맞춰 끝까지 탐색)
        /// arrayIndexStartOffset => Map이 1,1부터 시작하면 1,1 을 넣어준다.
        /// </summary>
        public static bool TraverseZigZagToFindTargetAndCount<T, TArray2D>(TArray2D dictionary2DArray
           , int dimension2D_X
           , int dimension2D_Y
           , ref IPointXY findIndexXY
           , SEARCH_START_DIRECTION startSeachDirection
           , ref int satisfiedCount
            , IPointXY arrayIndexStartOffset = new IPointXY()
            , params Func<T, bool>[] conditions)
        {
            IPointXY arrayCountXY;
            arrayCountXY.x = dimension2D_X;
            arrayCountXY.y = dimension2D_Y;

            IPointXY startIndexXY = new IPointXY(0, 0);
            IPointXY movingIndexOffsetXY = new IPointXY(0, 0);
            IPointXY outOfBoundCoordinateXY = new IPointXY();

            SetSearchingConditionForTraversingZigZag(startSeachDirection, ref startIndexXY, ref arrayCountXY, ref movingIndexOffsetXY, ref outOfBoundCoordinateXY);
            IPointXY travelIndexXY = new IPointXY(startIndexXY);

            satisfiedCount = 0;

            bool isFirstIndexFindSucess = false;

            // 다음 탐색 좌표가 맵 영역을 벗어나지 않을 때까지 반복
            while (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
            {
                // 맵은 1,1 보부터 시작하기에 dictionary 2d arrya의 값을 비교할 때나 최종 결과 좌표를 넘겨줄 때는 x, y 인덱스에 1을 더해준다.

                // 1을 더해준건 offset에 해당하는데...
                T targetObject = default(T);
                if (false == GetTargetObject<T, TArray2D>(travelIndexXY.x + arrayIndexStartOffset.x, travelIndexXY.y + arrayIndexStartOffset.y, dictionary2DArray, ref targetObject))
                {
                    return false;
                }
                
                if (false == isFirstIndexFindSucess && false == CheckConditions(targetObject, conditions))
                {
                    // 조건에 부합하지 않았기에 다음 인덱스 탐색하기 위해 movingIndexOffsetXY를 더했고,
                    travelIndexXY.x += movingIndexOffsetXY.x;
                    travelIndexXY.y += movingIndexOffsetXY.y;

                    // 다음 탐색 좌표가 맵 영역을 완전히 벗어나지 않았지만(벗어났다면 전체 맵 탐색 종료를 의미),
                    // 진행 방향의 맵을 벗어난다면, 다음 라인으로 변경
                    if (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
                    {
                        // 다음 Row로 넘어가는 조건에 해당
                        bool isLineMovedToNextLine = CheckAndSetNextLineForTraversingZigZag(startSeachDirection, arrayCountXY, ref travelIndexXY, ref movingIndexOffsetXY);
                    }
                }
                else
                {
                    if (false == isFirstIndexFindSucess)
                    {
                        findIndexXY.x = travelIndexXY.x + arrayIndexStartOffset.x;
                        findIndexXY.y = travelIndexXY.y + arrayIndexStartOffset.y;
                        isFirstIndexFindSucess = true;

                        // 조건 충족
                        ++satisfiedCount;

                        // 목표 좌표를 이미 찾았으나 남은 수량을 찾기 위해,
                        // 다음 인덱스 탐색하기 위해 movingIndexOffsetXY를 더했고, 
                        travelIndexXY.x += movingIndexOffsetXY.x;
                        travelIndexXY.y += movingIndexOffsetXY.y;

                        // 다음 탐색 좌표가 맵 영역을 완전히 벗어나지 않았지만(벗어났다면 전체 맵 탐색 종료를 의미),
                        // 진행 방향의 맵을 벗어난다면, 다음 라인으로 변경
                        if (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
                        {
                            // 다음 Row로 넘어가는 조건에 해당
                            bool isLineMovedToNextLine = CheckAndSetNextLineForTraversingZigZag(startSeachDirection, arrayCountXY, ref travelIndexXY, ref movingIndexOffsetXY);
                        }
                    }
                    else
                    {
                        if(CheckConditions(targetObject, conditions))
                        {
                            // 조건 충족
                            ++satisfiedCount;
                        }

                        // 2024.03.29 by shkim. [MOD] 주석 보완
                        // 목표 좌표를 이미 찾은 상태이고, 전체에 남은 수량을 찾기 위해
                        // 다음 인덱스 탐색하기 위해 movingIndexOffsetXY를 더했고, 
                        travelIndexXY.x += movingIndexOffsetXY.x;
                        travelIndexXY.y += movingIndexOffsetXY.y;

                        // 다음 탐색 좌표가 맵 영역을 완전히 벗어나지 않았지만(벗어났다면 전체 맵 탐색 종료를 의미),
                        // 진행 방향의 맵을 벗어난다면, 다음 라인으로 변경
                        if (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
                        {
                            // 다음 Row로 넘어가는 조건에 해당
                            bool isLineMovedToNextLine = CheckAndSetNextLineForTraversingZigZag(startSeachDirection, arrayCountXY, ref travelIndexXY, ref movingIndexOffsetXY);
                        }
                    }

                    continue;
                }
            }

            return (satisfiedCount != 0);
        }



        /// <summary>
        /// 지그재그로 탐색하며 목표(Target) 이전의 인덱스의 상태를 변경한다.
        /// </summary>
        public static bool TraverseZigZagToChangeStatusBeforeTarget<T, TArray2D>(TArray2D dictionary2DArray
           , int dimension2D_X
           , int dimension2D_Y
           , IPointXY targetIndexXY
           , SEARCH_START_DIRECTION startSeachDirection
           , Action<T> changeAction
           , IPointXY arrayIndexStartOffset = new IPointXY()
           , params Func<T, bool>[] conditions)
        {
            IPointXY arrayCountXY;
            arrayCountXY.x = dimension2D_X;
            arrayCountXY.y = dimension2D_Y;

            IPointXY startIndexXY = new IPointXY(0, 0);
            IPointXY movingIndexOffsetXY = new IPointXY(0, 0);
            IPointXY outOfBoundCoordinateXY = new IPointXY();

            SetSearchingConditionForTraversingZigZag(startSeachDirection, ref startIndexXY, ref arrayCountXY, ref movingIndexOffsetXY, ref outOfBoundCoordinateXY);
            IPointXY travelIndexXY = new IPointXY(startIndexXY);

            // 다음 탐색 좌표가 맵 영역을 벗어나지 않을 때까지 반복
            while (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
            {
                // 맵은 1,1 보부터 시작하기에 dictionary 2d arrya의 값을 비교할 때나 최종 결과 좌표를 넘겨줄 때는 x, y 인덱스에 1을 더해준다.

                T targetObject = default(T);
                if (false == GetTargetObject(travelIndexXY.x + arrayIndexStartOffset.x, travelIndexXY.y + arrayIndexStartOffset.y, dictionary2DArray, ref targetObject))
                {
                    return false;
                }

                if(travelIndexXY.x + arrayIndexStartOffset.x == targetIndexXY.x
                    && travelIndexXY.y + arrayIndexStartOffset.y == targetIndexXY.y)
                {
                    return true;
                }
                else
                {
                    if (true == CheckConditions(targetObject, conditions))
                    {
                        changeAction(targetObject);
                    }

                    travelIndexXY.x += movingIndexOffsetXY.x;
                    travelIndexXY.y += movingIndexOffsetXY.y;
                    // 다음 탐색 좌표가 맵 영역을 완전히 벗어나지 않았지만(벗어났다면 전체 맵 탐색 종료를 의미),
                    // 진행 방향의 맵을 벗어난다면, 다음 라인으로 변경
                    if (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
                    {
                        // 다음 Row로 넘어가는 조건에 해당
                        bool isLineMovedToNextLine = CheckAndSetNextLineForTraversingZigZag(startSeachDirection, arrayCountXY, ref travelIndexXY, ref movingIndexOffsetXY);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 지그재그로 탐색하며 조건에 충족하는 Target에 대한 Action을 취한다.
        /// </summary>
        public static bool TraverseZigZagForActionToSatisfiedConditionTarget<T, TArray2D>(TArray2D dictionary2DArray
           , int dimension2D_X
           , int dimension2D_Y
           , SEARCH_START_DIRECTION startSeachDirection
           , Action<T>[] actionForTarget
           , IPointXY arrayIndexStartOffset = new IPointXY()
           , params Func<T, bool>[] conditions)
        {
            IPointXY arrayCountXY;
            arrayCountXY.x = dimension2D_X;
            arrayCountXY.y = dimension2D_Y;

            IPointXY startIndexXY = new IPointXY(0, 0);
            IPointXY movingIndexOffsetXY = new IPointXY(0, 0);
            IPointXY outOfBoundCoordinateXY = new IPointXY();

            SetSearchingConditionForTraversingZigZag(startSeachDirection, ref startIndexXY, ref arrayCountXY, ref movingIndexOffsetXY, ref outOfBoundCoordinateXY);
            IPointXY travelIndexXY = new IPointXY(startIndexXY);

            int actionCount = actionForTarget == null ? 0 : actionForTarget.Length;

            // 다음 탐색 좌표가 맵 영역을 벗어나지 않을 때까지 반복
            while (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
            {
                // 맵은 1,1 보부터 시작하기에 dictionary 2d arrya의 값을 비교할 때나 최종 결과 좌표를 넘겨줄 때는 x, y 인덱스에 1을 더해준다.

                T targetObject = default(T);
                if (false == GetTargetObject(travelIndexXY.x + arrayIndexStartOffset.x, travelIndexXY.y + arrayIndexStartOffset.y, dictionary2DArray, ref targetObject))
                {
                    return false;
                }

                if (true == CheckConditions(targetObject, conditions))
                {
                    Parallel.For(0, actionCount, (i) =>
                    {
                        actionForTarget[i](targetObject);
                    });
                    // actionForTarget(targetObject);
                }

                travelIndexXY.x += movingIndexOffsetXY.x;
                travelIndexXY.y += movingIndexOffsetXY.y;
                // 다음 탐색 좌표가 맵 영역을 완전히 벗어나지 않았지만(벗어났다면 전체 맵 탐색 종료를 의미),
                // 진행 방향의 맵을 벗어난다면, 다음 라인으로 변경
                if (false == (travelIndexXY.x == outOfBoundCoordinateXY.x && travelIndexXY.y == outOfBoundCoordinateXY.y))
                {
                    // 다음 Row로 넘어가는 조건에 해당
                    bool isLineMovedToNextLine = CheckAndSetNextLineForTraversingZigZag(startSeachDirection, arrayCountXY, ref travelIndexXY, ref movingIndexOffsetXY);
                }
            }

            return true;
        }





        /// <summary>
        /// T의 타입이 TArray2D 타입에 담겨 있을 때 column, row에 해당하는 T를 찾아 반환한다.
        /// TArray2D 타입 : ConcurrentDictionary<int, ConcurrentDictionary<int, T>>, T[,] ...
        /// </summary>
        static bool GetTargetObject<T, TArray2D>(int columnIndex, int rowIndex, TArray2D tArray2D, ref T targetObject)
        {
            Type type = typeof(TArray2D);

            if (typeof(ConcurrentDictionary<int, ConcurrentDictionary<int, T>>).Equals(type))
            {
                var dictionary2DArray = tArray2D as ConcurrentDictionary<int, ConcurrentDictionary<int, T>>;

                ConcurrentDictionary<int, T> rowDictionary;
                if (false == dictionary2DArray.TryGetValue(columnIndex, out rowDictionary))
                {
                    return false;
                }
                if (false == rowDictionary.TryGetValue(rowIndex, out targetObject))
                {
                    return false;
                }

                return true;
            }
            else if (typeof(T[,]).Equals(type))
            {
                var array2D = tArray2D as T[,];

                array2D.GetLength(0);
                array2D.GetLength(1);

                targetObject = array2D[columnIndex, rowIndex];

                return true;
            }

            return false;
        }

        static void SetSearchingConditionForTraversingZigZag(SEARCH_START_DIRECTION startSeachDirection
            , ref IPointXY startIndexXY
            , ref IPointXY arrayCountXY
            , ref IPointXY movingIndexOffsetXY
            , ref IPointXY outOfBoundCoordinateXY)
        {
            #region <탐색조건설정>
            switch (startSeachDirection)
            {
                #region <가로방향작업>
                case SEARCH_START_DIRECTION.TOP_LEFT:
                    startIndexXY.x = 0;
                    startIndexXY.y = 0;

                    movingIndexOffsetXY.x = 1;
                    movingIndexOffsetXY.y = 0;

                    outOfBoundCoordinateXY.x = (arrayCountXY.y % 2 == 0) ? -1 : arrayCountXY.x;
                    outOfBoundCoordinateXY.y = arrayCountXY.y - 1;
                    break;

                case SEARCH_START_DIRECTION.TOP_RIGHT:
                    startIndexXY.x = arrayCountXY.x - 1;
                    startIndexXY.y = 0;

                    movingIndexOffsetXY.x = -1;
                    movingIndexOffsetXY.y = 0;

                    outOfBoundCoordinateXY.x = (arrayCountXY.y % 2 == 0) ? arrayCountXY.x : -1;
                    outOfBoundCoordinateXY.y = arrayCountXY.y - 1;
                    break;

                case SEARCH_START_DIRECTION.BOTTOM_LEFT:
                    startIndexXY.x = 0;
                    startIndexXY.y = arrayCountXY.y - 1; ;

                    movingIndexOffsetXY.x = 1;
                    movingIndexOffsetXY.y = 0;

                    outOfBoundCoordinateXY.x = (arrayCountXY.y % 2 == 0) ? -1 : arrayCountXY.x;
                    outOfBoundCoordinateXY.y = 0;
                    break;

                case SEARCH_START_DIRECTION.BOTTOM_RIGHT:
                    startIndexXY.x = arrayCountXY.x - 1;
                    startIndexXY.y = arrayCountXY.y - 1; ;

                    movingIndexOffsetXY.x = -1;
                    movingIndexOffsetXY.y = 0;

                    outOfBoundCoordinateXY.x = (arrayCountXY.y % 2 == 0) ? arrayCountXY.x : -1;
                    outOfBoundCoordinateXY.y = 0;
                    break;
                #endregion </가로방향작업>

                #region <세로방향작업>
                case SEARCH_START_DIRECTION.LEFT_TOP:
                    startIndexXY.x = 0;
                    startIndexXY.y = 0;

                    movingIndexOffsetXY.x = 0;
                    movingIndexOffsetXY.y = 1;

                    outOfBoundCoordinateXY.x = arrayCountXY.x - 1;
                    outOfBoundCoordinateXY.y = (arrayCountXY.x % 2 == 0) ? -1 : arrayCountXY.y;
                    break;

                case SEARCH_START_DIRECTION.LEFT_BOTTOM:
                    startIndexXY.x = 0;
                    startIndexXY.y = arrayCountXY.y - 1;

                    movingIndexOffsetXY.x = 0;
                    movingIndexOffsetXY.y = -1;

                    outOfBoundCoordinateXY.x = arrayCountXY.x - 1;
                    outOfBoundCoordinateXY.y = (arrayCountXY.x % 2 == 0) ? arrayCountXY.y : -1;
                    break;

                case SEARCH_START_DIRECTION.RIGHT_TOP:
                    startIndexXY.x = arrayCountXY.x - 1;
                    startIndexXY.y = 0;

                    movingIndexOffsetXY.x = 0;
                    movingIndexOffsetXY.y = 1;

                    outOfBoundCoordinateXY.x = 0;
                    outOfBoundCoordinateXY.y = (arrayCountXY.x % 2 == 0) ? -1 : arrayCountXY.y;
                    break;

                case SEARCH_START_DIRECTION.RIGHT_BOTTOM:
                    startIndexXY.x = arrayCountXY.x - 1;
                    startIndexXY.y = arrayCountXY.y - 1;

                    movingIndexOffsetXY.x = 0;
                    movingIndexOffsetXY.y = -1;

                    outOfBoundCoordinateXY.x = 0;
                    outOfBoundCoordinateXY.y = (arrayCountXY.x % 2 == 0) ? arrayCountXY.y : -1;
                    break;
                    #endregion </세로방향작업>
            }
            #endregion </탐색조건설정>
        }

        static bool CheckAndSetNextLineForTraversingZigZag(SEARCH_START_DIRECTION startSeachDirection
            , IPointXY arrayCountXY
            , ref IPointXY travelIndexXY
            , ref IPointXY movingIndexOffsetXY)

        {
            #region <탐색조건설정>
            switch (startSeachDirection)
            {
                case SEARCH_START_DIRECTION.TOP_LEFT:
                case SEARCH_START_DIRECTION.TOP_RIGHT:
                    if (travelIndexXY.x < 0 || travelIndexXY.x == arrayCountXY.x)
                    {
                        movingIndexOffsetXY.x *= -1;

                        travelIndexXY.x += movingIndexOffsetXY.x;
                        travelIndexXY.y += 1;

                        return true;
                    }
                    break;

                case SEARCH_START_DIRECTION.BOTTOM_LEFT:
                case SEARCH_START_DIRECTION.BOTTOM_RIGHT:
                    if (travelIndexXY.x < 0 || travelIndexXY.x == arrayCountXY.x)
                    {
                        movingIndexOffsetXY.x *= -1;

                        travelIndexXY.x += movingIndexOffsetXY.x;
                        travelIndexXY.y -= 1;

                        return true;
                    }
                    break;

                case SEARCH_START_DIRECTION.LEFT_TOP:
                case SEARCH_START_DIRECTION.LEFT_BOTTOM:
                    if (travelIndexXY.y < 0 || travelIndexXY.y == arrayCountXY.y)
                    {
                        movingIndexOffsetXY.y *= -1;

                        travelIndexXY.y += movingIndexOffsetXY.y;
                        travelIndexXY.x += 1;

                        return true;
                    }
                    break;

                case SEARCH_START_DIRECTION.RIGHT_TOP:
                case SEARCH_START_DIRECTION.RIGHT_BOTTOM:
                    if (travelIndexXY.y < 0 || travelIndexXY.y == arrayCountXY.y)
                    {
                        movingIndexOffsetXY.y *= -1;

                        travelIndexXY.y += movingIndexOffsetXY.y;
                        travelIndexXY.x -= 1;

                        return true;
                    }
                    break;
            }
            #endregion </탐색조건설정>

            return false;
        }
        #endregion </ForTraverseZigZag>

        //static void TraverseSpiral(int[,] array, int startX, int startY, int directionX, int directionY)
        //{
        //    int rows = array.GetLength(0);
        //    int cols = array.GetLength(1);

        //    int x = startX;
        //    int y = startY;

        //    for (int i = 0; i < rows * cols; i++)
        //    {
        //        //Console.WriteLine($"({x}, {y}): {array[y, x]}");

        //        // 다음 위치 계산
        //        x += directionX;
        //        y += directionY;

        //        // 배열 경계 확인 및 방향 전환
        //        if (x < 0 || x >= cols || y < 0 || y >= rows || array[y, x] == -1)
        //        {
        //            // 배열 경계를 벗어나거나 이미 방문한 경우 방향 전환
        //            x -= directionX;
        //            y -= directionY;

        //            // 방문한 위치를 표시하려면 필요한 경우 값을 변경할 수 있음
        //            array[y, x] = -1;

        //            // 방향 전환
        //            int temp = directionX;
        //            directionX = directionY;
        //            directionY = -temp;

        //            // 다시 다음 위치 계산
        //            x += directionX;
        //            y += directionY;
        //        }
        //    }
        //}

        #endregion  /FindLogic

        
        static bool Switch2DArrayElement<T, TArray2D>(int switchArrayColCount, int switchArrayRowCount,
            int switchCol, int switchRow, ref T element, ref TArray2D switchedArray)
        {
            Type type = typeof(TArray2D);

            if (typeof(ConcurrentDictionary<int, ConcurrentDictionary<int, T>>).Equals(type))
            {
                var newDictionary2DArray = switchedArray as ConcurrentDictionary<int, ConcurrentDictionary<int, T>>;

                if (newDictionary2DArray == null)
                {
                    newDictionary2DArray = new ConcurrentDictionary<int, ConcurrentDictionary<int, T>>();
                }
                ConcurrentDictionary<int, T> rowDictionary = null;
                if (false == newDictionary2DArray.TryGetValue(switchCol, out rowDictionary)
                    || rowDictionary == null)
                {
                    rowDictionary = new ConcurrentDictionary<int, T>();
                    newDictionary2DArray.TryAdd(switchCol, rowDictionary);
                }
                rowDictionary.TryAdd(switchRow, element);

                switchedArray = (TArray2D)(object)newDictionary2DArray;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Map을 시계반대방향(CCW)로 회전한다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TArray2D"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static bool Rotate<T, TArray2D>(ref TArray2D array, ref int colCount, ref int rowCount, int currentAngle, int targetAngle, Action<T, int, int> indexChangeAction = null) 
        {
            int rotateAngle = 0;

            rotateAngle = currentAngle - targetAngle;

            int originColCount = colCount;
            int originRowCount = rowCount;

            int rotatedColCount = 0;
            int rotatedRowCount = 0;

            TArray2D rotatedArray = default(TArray2D);

            try
            {
                switch (rotateAngle)
                {
                    case -270:
                    case 90:
                        rotatedColCount = originRowCount;
                        rotatedRowCount = originColCount;

                        for (int col = 1; col <= rotatedColCount; col++)
                        {
                            for (int row = 1; row <= rotatedRowCount; row++)
                            {
                                T element = default(T);
                                if(false == GetTargetObject(row, originRowCount - col + 1, array, ref element))
                                {
                                    return false;
                                }
                                if(false == Switch2DArrayElement(rotatedColCount, rotatedRowCount, col, row, ref element, ref rotatedArray))
                                {
                                    return false;
                                }
                                if(indexChangeAction != null)
                                {
                                    indexChangeAction(element, col, row);
                                }
                            }
                        }
                        break;

                    case -180:
                    case 180:
                        rotatedColCount = originColCount;
                        rotatedRowCount = originRowCount;

                        for (int col = 1; col <= rotatedColCount; col++)
                        {
                            for (int row = 1; row <= rotatedRowCount; row++)
                            {
                                T element = default(T);
                                if (false == GetTargetObject(originColCount - col + 1, originRowCount - row + 1, array, ref element))
                                {
                                    return false;
                                }
                                if (false == Switch2DArrayElement(rotatedColCount, rotatedRowCount, col, row, ref element, ref rotatedArray))
                                {
                                    return false;
                                }
                                if (indexChangeAction != null)
                                {
                                    indexChangeAction(element, col, row);
                                }
                            }
                        }
                        break;

                    case -90:
                    case 270:
                        rotatedColCount = originRowCount;
                        rotatedRowCount = originColCount;

                        for (int col = 1; col <= rotatedColCount; col++)
                        {
                            for (int row = 1; row <= rotatedRowCount; row++)
                            {
                                T element = default(T);
                                if (false == GetTargetObject(originColCount - row + 1, col, array, ref element))
                                {
                                    return false;
                                }
                                if (false == Switch2DArrayElement(rotatedColCount, rotatedRowCount, col, row, ref element, ref rotatedArray))
                                {
                                    return false;
                                }
                                if (indexChangeAction != null)
                                {
                                    indexChangeAction(element, col, row);
                                }
                            }
                        }
                        break;

                    case 0:
                        //rotatedMapData = null;
                        return true;

                    default:
                        //rotatedMapData = null;
                        return false;
                }
            }
            catch
            {

            }

            colCount = rotatedColCount;
            rowCount = rotatedRowCount;

            array = rotatedArray;

            return true;
        }


        /// <summary>
        /// Wafer의 작업 반경이 안전한지 확인한다. (Ejector 등과의 충돌 예방)
        /// </summary>
        /// <param name="targetNextPositionXY"></param>
        /// <param name="waferCenterPositionXY"></param>
        /// <param name="safetyRadius"></param>
        /// <returns></returns>
        public static bool IsNextPositionMovingSafe(DPointXY targetNextPositionXY, DPointXY waferCenterPositionXY, double safetyRadius)
        {
            double distanceFromCenter = Math.Sqrt(Math.Pow(targetNextPositionXY.x - waferCenterPositionXY.x, 2) + Math.Pow(targetNextPositionXY.y - waferCenterPositionXY.y, 2));

            if (distanceFromCenter < safetyRadius)
            {
                return true;
            }

            return false;
        }
    }
}
