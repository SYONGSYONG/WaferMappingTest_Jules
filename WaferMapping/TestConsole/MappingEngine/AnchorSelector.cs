using Define.DefineEnumProject.WaferMap;
using FrameOfSystem3.Work.WaferMap.WaferMapDatabase;
using FrameOfSystem3.Work.WaferMap.WaferMapDatabase.InnerDocument;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrameOfSystem3.Work.WaferMap.MappingEngine
{
    public class AnchorSelector
    {
        public List<UnitInformation> SelectAnchors(WaferInformation map, int refCol, int refRow)
        {
            var result = new HashSet<UnitInformation>(); // Use HashSet to avoid duplicates

            // 1. Collect all valid chips (State == 1)
            var validChips = new List<UnitInformation>();

            for(int row = 1; row <= map.ArrayCount.y; row++)
            {
                for(int col = 1; col <= map.ArrayCount.x; col++)
                {
                    var chip = map.GetUnitInformation(col, row);

                    // Ready나 NotWork가 칩이 있는 것으로 간주
                    if(chip != null
                        && (chip.WorkingState == (int)UNIT_WORKING_STATE.READY || chip.WorkingState == (int)UNIT_WORKING_STATE.NOT_WORK))
                    {
                        validChips.Add(chip);
                    }
                }
			}

			if (validChips.Count == 0) return result.ToList();

            // Always try to include the Reference Chip if it exists and is valid
            var refChip = map.GetUnitInformation(refCol, refRow);
            if (refChip != null && (refChip.WorkingState == (int)UNIT_WORKING_STATE.READY || refChip.WorkingState == (int)UNIT_WORKING_STATE.NOT_WORK))
            {
                result.Add(refChip);
            }

            // 2. Define 8 Sectors (0, 45, 90, ..., 315)
            // Target angles in degrees
            double[] sectorAngles = { 0, 45, 90, 135, 180, 225, 270, 315 };
            double sectorWidth = 15.0; // +/- 15 degrees

            // Store candidates for each sector to ensure distribution
            var sectorCandidates = new Dictionary<int, List<UnitInformation>>();
            for (int i = 0; i < 8; i++) sectorCandidates[i] = new List<UnitInformation>();

            // 3. Classify chips into sectors
            foreach (var chip in validChips)
            {
                if (chip == refChip) continue; // Skip reference chip for sector logic (distance 0)

                // dx, dy는 index 거리를 의미?
                double dx = chip.UnitIndex.x - refCol;
                double dy = chip.UnitIndex.y - refRow;

                double dist = Math.Sqrt(dx*dx + dy*dy);
                if (dist < 1.0) continue; // Too close to be meaningful

                double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                // Normalize to [0, 360)
                if (angle < 0) angle += 360.0;

                // Check which sector it falls into
                for (int i = 0; i < 8; i++)
                {
                    double target = sectorAngles[i];
                    double diff = Math.Abs(angle - target);

                    // Handle wrap-around for 0/360
                    if (diff > 180) diff = 360 - diff;

                    if (diff <= sectorWidth)
                    {
                        sectorCandidates[i].Add(chip);
                        break; // Assign to the first matching sector
                    }
                }
            }

            // 4. Select Edge and Midpoint for each sector
            foreach (var kvp in sectorCandidates)
            {
                var candidates = kvp.Value;
                if (candidates.Count == 0) continue;

                // Sort by distance descending (farthest first)
                candidates.Sort((a, b) =>
                {
                    double distA = GetDist(a, refCol, refRow);
                    double distB = GetDist(b, refCol, refRow);
                    return distB.CompareTo(distA);
                });

                // Edge Chip: Farthest
                var edgeChip = candidates[0];
                result.Add(edgeChip);

                // Midpoint Chip: Closest to distance/2
                if (candidates.Count > 1)
                {
                    double edgeDist = GetDist(edgeChip, refCol, refRow);
                    double targetDist = edgeDist * 0.5;

					UnitInformation bestMid = null;
                    double minDiff = double.MaxValue;

                    foreach (var c in candidates)
                    {
                        if (c == edgeChip) continue;
                        double d = GetDist(c, refCol, refRow);
                        double diff = Math.Abs(d - targetDist);
                        if (diff < minDiff)
                        {
                            minDiff = diff;
                            bestMid = c;
                        }
                    }

                    if (bestMid != null)
                    {
                        result.Add(bestMid);
                    }
                }
            }

            // 5. Minimum Anchor Guarantee (Total >= 8)
            // If we have fewer than 8, fill with remaining valid chips, prioritizing farthest from center (to expand coverage)
            if (result.Count < 8 && validChips.Count > result.Count)
            {
                // Sort all valid chips by distance descending
                var remaining = validChips
                    .Where(c => !result.Contains(c))
                    .OrderByDescending(c => GetDist(c, refCol, refRow))
                    .ToList();

                foreach (var c in remaining)
                {
                    result.Add(c);
                    if (result.Count >= 8) break;
                }
            }

            return result.ToList();
        }

        private double GetDist(UnitInformation c, int refCol, int refRow)
        {
            double dx = c.UnitIndex.x - refCol;
            double dy = c.UnitIndex.y - refRow;
            return Math.Sqrt(dx*dx + dy*dy);
        }
    }
}
