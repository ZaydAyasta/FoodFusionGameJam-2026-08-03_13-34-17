using System.Collections.Generic;
using UnityEngine;

namespace MyCommonToolkit.Astar
{
    public static class PathDebugger
    {
        static List<PathNode> path;
        static Grid<PathNode> grid;

        public static void SetGrid(Grid<PathNode> debugGrid)
        {
            grid = debugGrid;
        }

        public static void SetPath(List<PathNode> debugPath)
        {
            path = debugPath;
        }

        public static void Draw()
        {
            if (grid == null) return;

            float cellSize = grid.GetCellSize();
            Vector3 origin = grid.GetOrigin();

            // Draw grid
            for (int x = 0; x < grid.GetWidth(); x++)
            {
                for (int y = 0; y < grid.GetHeight(); y++)
                {
                    PathNode node = grid.GetValue(x, y);

                    Gizmos.color = node.isWalkable ? Color.white : Color.red;

                    Vector3 pos = origin +
                                  new Vector3(x * cellSize + cellSize * 0.5f,
                                              y * cellSize + cellSize * 0.5f,
                                              0);

                    Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
                }
            }

            // Draw path
            if (path == null || path.Count == 0)
                return;

            Gizmos.color = Color.green;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 current =
                    origin +
                    new Vector3(path[i].x * cellSize + cellSize * 0.5f,
                                path[i].y * cellSize + cellSize * 0.5f,
                                0);

                Gizmos.DrawSphere(current, cellSize * 0.15f);

                if (i < path.Count - 1)
                {
                    Vector3 next =
                        origin +
                        new Vector3(path[i + 1].x * cellSize + cellSize * 0.5f,
                                    path[i + 1].y * cellSize + cellSize * 0.5f,
                                    0);

                    Gizmos.DrawLine(current, next);
                }
            }
        }
    }
}
