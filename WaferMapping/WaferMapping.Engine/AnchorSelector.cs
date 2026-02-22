using System;
using System.Collections.Generic;

namespace WaferMapping.Engine
{
    public class AnchorSelector
    {
        public List<Chip> SelectAnchors(WaferMap map, int refCol, int refRow)
        {
            var result = new List<Chip>();
            // Radius estimation
            int maxRadius = Math.Min(map.Rows, map.Columns) / 2;

            // 8 directions: (dx, dy)
            int[][] directions = new int[][]
            {
                new int[] { 0, 1 },   // Up
                new int[] { 0, -1 },  // Down
                new int[] { 1, 0 },   // Right
                new int[] { -1, 0 },  // Left
                new int[] { 1, 1 },   // Up-Right
                new int[] { 1, -1 },  // Down-Right
                new int[] { -1, 1 },  // Up-Left
                new int[] { -1, -1 }  // Down-Left
            };

            double[] ratios = { 0.3, 0.7 };

            foreach (var dir in directions)
            {
                foreach (var ratio in ratios)
                {
                    int targetDist = (int)(maxRadius * ratio);
                    int targetCol = refCol + dir[0] * targetDist;
                    int targetRow = refRow + dir[1] * targetDist;

                    // Find nearest chip to (targetCol, targetRow)
                    Chip nearest = FindNearestChip(map, targetCol, targetRow);
                    if (nearest != null && !result.Contains(nearest))
                    {
                        result.Add(nearest);
                    }
                }
            }

            return result;
        }

        private Chip FindNearestChip(WaferMap map, int targetCol, int targetRow)
        {
            // BFS search for nearest State==1 chip
            var visited = new HashSet<string>(); // "col,row" key
            var queue = new Queue<Tuple<int, int>>();

            queue.Enqueue(Tuple.Create(targetCol, targetRow));
            visited.Add($"{targetCol},{targetRow}");

            // Limit search to prevent infinite loops or long search
            int maxSearchRadius = Math.Max(map.Rows, map.Columns) / 4;
            if (maxSearchRadius < 10) maxSearchRadius = 10;

            // To prioritize closest, we process level by level?
            // Queue naturally does BFS, so first found is closest (in Manhattan/path distance).
            // Euclidean distance might differ slightly but for grid BFS is good approximation.

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int c = current.Item1;
                int r = current.Item2;

                // Check bounds
                if (c >= 0 && c < map.Columns && r >= 0 && r < map.Rows)
                {
                    Chip chip = map.GetChip(c, r);
                    if (chip != null && chip.State == 1)
                    {
                        return chip;
                    }
                }

                // Stop if too far from target
                if (Math.Abs(c - targetCol) > maxSearchRadius || Math.Abs(r - targetRow) > maxSearchRadius)
                    continue;

                // Neighbors (8-connectivity)
                int[][] neighbors = {
                    new int[]{0,1}, new int[]{0,-1}, new int[]{1,0}, new int[]{-1,0},
                    new int[]{1,1}, new int[]{1,-1}, new int[]{-1,1}, new int[]{-1,-1}
                };

                foreach (var n in neighbors)
                {
                    int nc = c + n[0];
                    int nr = r + n[1];
                    string key = $"{nc},{nr}";

                    if (!visited.Contains(key))
                    {
                        visited.Add(key);
                        queue.Enqueue(Tuple.Create(nc, nr));
                    }
                }
            }

            return null;
        }
    }
}
