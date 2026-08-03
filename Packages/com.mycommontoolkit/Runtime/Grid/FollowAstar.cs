using System.Collections.Generic;
using UnityEngine;

namespace MyCommonToolkit
{
    namespace Astar
    {
        ///<summary>
        ///The Astar namespace makes finds a path from point A to point B using the grid based A* algorithm.
        ///To use it, create a map first using FollowAstar.CreateMap().All enemies in the same map/level will use that map.
        ///Give the map's height and width and cell size.The heigher the cell size the smoother the path will be.
        ///Then, if you want the path to detect obstacles, use FollowAstar.FindWalkable() and give the layer of the obstacles with the path you just created.
        ///You can finally receive the path as a queue of points with FollowAstar.GetWorldPath()
        ///</summary>
        public class FollowAstar
        {
            public static Pathifinding CreateMap(float worldHeight, float worldWidth, float cellSize)
            {
                Vector3 originPosition = Vector3.zero - new Vector3(worldWidth/ 2, worldWidth*cellSize / 2, 0);
                int mapHeight =Mathf.FloorToInt(worldHeight/cellSize);
                int mapWidth = Mathf.FloorToInt(worldWidth / cellSize);
                Pathifinding path = new(mapHeight, mapWidth, cellSize, originPosition);
                return path;
            }
            public static void FindWalkable(LayerMask obstacleLayer, Pathifinding path)
            {
                for (int x = 0; x < path.grid.GetWidth(); x++)
                {
                    for (int y = 0; y < path.grid.GetHeight(); y++)
                    {
                        Vector3 worldPos = path.grid.GetWorldPos(x, y);
                        Vector3 center = new(worldPos.x + path.grid.GetCellSize() / 2, worldPos.y + path.grid.GetCellSize() / 2);
                        Collider2D hit = Physics2D.OverlapBox(center, new Vector2(path.grid.GetCellSize(), path.grid.GetCellSize()), 0, obstacleLayer);
                        if (hit)
                            path.SetWalkable(worldPos, false);
                    }
                }
            }
            public static Queue<Vector3> GetWorldPath(Pathifinding path, Vector3 myPosition, Vector3 targetPosition)
            {
                path.grid.GetCell(myPosition, out int startX, out int startY);
                path.grid.GetCell(targetPosition, out int endX, out int endY);
                List<PathNode> pathNodes = new();
                pathNodes = path.FindPath(startX, startY, endX, endY);
                Queue<Vector3> worldPath = new();
                if (pathNodes == null)
                {
                    return null;
                }
                foreach (PathNode pathNode in pathNodes)
                    worldPath.Enqueue(path.grid.GetWorldPos(pathNode.x, pathNode.y));
                worldPath.Enqueue(targetPosition);
                return worldPath;
            }
        }
    }
}
