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
        /// <summary>
        /// Selects anchor chips using Longest Span Search (Axis) + Sector-based Edge Search (Distribution).
        /// </summary>
        /// <param name="map">Wafer Information</param>
        /// <param name="refCol">Reference Column Index</param>
        /// <param name="refRow">Reference Row Index</param>
        /// <param name="startOffset">Start Index for loop (default 0). For 1-based WaferMap, pass 1.</param>
        /// <returns>List of selected UnitInformation</returns>
        public List<UnitInformation> SelectAnchors(WaferInformation map, int refCol, int refRow, int startOffset = 0)
        {
            var result = new HashSet<UnitInformation>(); // Use HashSet to avoid duplicates

            // 1. Collect all valid chips
            var validChips = new List<UnitInformation>();

            int endCol = map.ArrayCount.x + startOffset;
            int endRow = map.ArrayCount.y + startOffset;

            // Map valid chips by Row and Column for efficient span calculation
            var rows = new Dictionary<int, List<UnitInformation>>();
            var cols = new Dictionary<int, List<UnitInformation>>();

            for (int row = startOffset; row < endRow; row++)
            {
                for (int col = startOffset; col < endCol; col++)
                {
                    var chip = map.GetUnitInformation(col, row);
                    
                    if (chip != null 
                        && (chip.WorkingState == (int)UNIT_WORKING_STATE.READY || chip.WorkingState == (int)UNIT_WORKING_STATE.NOT_WORK))
                    {
                        validChips.Add(chip);

                        if (!rows.ContainsKey(row)) rows[row] = new List<UnitInformation>();
                        rows[row].Add(chip);

                        if (!cols.ContainsKey(col)) cols[col] = new List<UnitInformation>();
                        cols[col].Add(chip);
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

            // 2. Longest Span Search (Global Axis Search)
            // Find the Row with the maximum span (max X - min X)
            // Find the Column with the maximum span (max Y - min Y)
            
            // X-Axis (Row Span)
            if (rows.Count > 0)
            {
                var maxSpanRow = rows.OrderByDescending(kvp => 
                {
                    if (kvp.Value.Count < 2) return 0;
                    return kvp.Value.Max(c => c.UnitIndex.x) - kvp.Value.Min(c => c.UnitIndex.x);
                }).FirstOrDefault();

                if (maxSpanRow.Value != null && maxSpanRow.Value.Count > 0)
                {
                    // Add Start and End of this longest row
                    var sorted = maxSpanRow.Value.OrderBy(c => c.UnitIndex.x).ToList();
                    result.Add(sorted.First());
                    result.Add(sorted.Last());
                    // Add Midpoint of longest row if long enough
                    if (sorted.Count > 2) result.Add(sorted[sorted.Count / 2]);
                }
            }

            // Y-Axis (Column Span)
            if (cols.Count > 0)
            {
                var maxSpanCol = cols.OrderByDescending(kvp => 
                {
                    if (kvp.Value.Count < 2) return 0;
                    return kvp.Value.Max(c => c.UnitIndex.y) - kvp.Value.Min(c => c.UnitIndex.y);
                }).FirstOrDefault();

                if (maxSpanCol.Value != null && maxSpanCol.Value.Count > 0)
                {
                    // Add Start and End of this longest column
                    var sorted = maxSpanCol.Value.OrderBy(c => c.UnitIndex.y).ToList();
                    result.Add(sorted.First());
                    result.Add(sorted.Last());
                    // Add Midpoint of longest col if long enough
                    if (sorted.Count > 2) result.Add(sorted[sorted.Count / 2]);
                }
            }

            // 3. Sector-based Edge Search (Fill Gaps & Ensure Distribution)
            // We use Reference Chip as the center for sectors, even if it's off-center.
            
            double[] sectorAngles = { 0, 45, 90, 135, 180, 225, 270, 315 };
            double sectorWidth = 20.0; // Slightly wider to catch diagonals better
            
            var sectorCandidates = new Dictionary<int, List<UnitInformation>>();
            for (int i = 0; i < 8; i++) sectorCandidates[i] = new List<UnitInformation>();

            foreach (var chip in validChips)
            {
                if (result.Contains(chip)) continue; // Skip already selected

                double dx = chip.UnitIndex.x - refCol;
                double dy = chip.UnitIndex.y - refRow;
                double dist = Math.Sqrt(dx*dx + dy*dy);
                if (dist < 1.0) continue;

                double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                if (angle < 0) angle += 360.0;

                for (int i = 0; i < 8; i++)
                {
                    double diff = Math.Abs(angle - sectorAngles[i]);
                    if (diff > 180) diff = 360 - diff;

                    if (diff <= sectorWidth)
                    {
                        sectorCandidates[i].Add(chip);
                        break; 
                    }
                }
            }

            // Select Farthest from each sector
            foreach (var kvp in sectorCandidates)
            {
                var candidates = kvp.Value;
                if (candidates.Count == 0) continue;

                // Sort by distance descending
                candidates.Sort((a, b) =>
                {
                    double distA = GetDist(a, refCol, refRow);
                    double distB = GetDist(b, refCol, refRow);
                    return distB.CompareTo(distA);
                });

                // Add farthest (Edge)
                result.Add(candidates[0]);
                
                // Add Midpoint (Density)
                if (candidates.Count > 1)
                {
                     double edgeDist = GetDist(candidates[0], refCol, refRow);
                     double targetDist = edgeDist * 0.5;
                     
                     var bestMid = candidates.OrderBy(c => Math.Abs(GetDist(c, refCol, refRow) - targetDist)).First();
                     if (bestMid != candidates[0]) result.Add(bestMid);
                }
            }

            // 4. Minimum Anchor Guarantee (Total >= 8) with Quadrant Distribution
            // If we have fewer than 8, or if some quadrants are empty, try to fill them.
            
            while (result.Count < 8 && result.Count < validChips.Count)
            {
                int[] qCounts = new int[4];
                foreach(var c in result)
                {
                    int q = GetQuadrant(c, refCol, refRow);
                    if (q >= 0) qCounts[q]++;
                }

                int targetQ = -1;
                int minCount = int.MaxValue;
                for(int i=0; i<4; i++)
                {
                    if (qCounts[i] < minCount)
                    {
                        minCount = qCounts[i];
                        targetQ = i;
                    }
                }

                var candidatesInQ = validChips
                    .Where(c => !result.Contains(c) && GetQuadrant(c, refCol, refRow) == targetQ)
                    .OrderByDescending(c => GetDist(c, refCol, refRow))
                    .ToList();

                if (candidatesInQ.Count > 0)
                {
                    result.Add(candidatesInQ[0]);
                }
                else
                {
                    // Fallback to any farthest available
                    var fallback = validChips
                        .Where(c => !result.Contains(c))
                        .OrderByDescending(c => GetDist(c, refCol, refRow))
                        .FirstOrDefault();
                    
                    if (fallback != null) result.Add(fallback);
                    else break;
                }
            }

            // 5. Sort by distance from Reference (Ascending)
            // This ensures we jump to closer anchors first, minimizing expansion error.
            var sortedResult = result.OrderBy(c => GetDist(c, refCol, refRow)).ToList();

            return sortedResult;
        }

        private double GetDist(UnitInformation c, int refCol, int refRow)
        {
            double dx = c.UnitIndex.x - refCol;
            double dy = c.UnitIndex.y - refRow;
            return Math.Sqrt(dx*dx + dy*dy);
        }

        private int GetQuadrant(UnitInformation c, int refCol, int refRow)
        {
            int dx = c.UnitIndex.x - refCol;
            int dy = c.UnitIndex.y - refRow;

            if (dx >= 0 && dy >= 0) return 0; // NE
            if (dx < 0 && dy >= 0) return 1;  // NW
            if (dx < 0 && dy < 0) return 2;   // SW
            if (dx >= 0 && dy < 0) return 3;  // SE
            return 0;
        }
    }
}
