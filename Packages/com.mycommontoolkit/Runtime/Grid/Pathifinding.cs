using System.Collections.Generic;
using UnityEngine;

namespace MyCommonToolkit
{
    namespace Astar
    {
        public class Pathifinding
        {
            const int STRAIGHT_COST = 10;
            const int DIAGONAL_COST = 14;
            public Grid<PathNode> grid;
            List<PathNode> openList;
            List<PathNode> closedList;
            public Pathifinding(int height, int width, float cellSize, Vector3 originPos)
            {
                grid = new(height, width, cellSize, originPos, (x, y) => new PathNode(x, y));
            }
            public List<PathNode> FindPath(int StartX, int StartY, int EndX, int EndY)
            {
                PathNode startNode = grid.GetValue(StartX, StartY);
                PathNode endNode = grid.GetValue(EndX, EndY);
                openList = new() { startNode };
                closedList = new();
                for (int x = 0; x < grid.GetWidth(); x++)
                {
                    for (int y = 0; y < grid.GetHeight(); y++)
                    {
                        PathNode pathNode = grid.GetValue(x, y);
                        pathNode.gCost = int.MaxValue;
                        pathNode.CalculateFCost();
                        pathNode.pastNode = null;
                    }
                }
                startNode.gCost = 0;
                startNode.hCost = CalculateDistance(startNode, endNode);
                startNode.CalculateFCost();
                while (openList.Count > 0)
                {
                    PathNode currentNode = LowestFCost(openList);
                    if (currentNode == endNode)
                    {
                        //Complete
                        return CalculatePath(endNode);
                    }
                    openList.Remove(currentNode);
                    closedList.Add(currentNode);
                    foreach (PathNode neighbour in GetNeighbours(currentNode))
                    {
                        if (closedList.Contains(neighbour)) continue;
                        int tentativeGCost = currentNode.gCost + CalculateDistance(currentNode, neighbour);
                        if (tentativeGCost < neighbour.gCost)
                        {
                            neighbour.pastNode = currentNode;
                            neighbour.gCost = tentativeGCost;
                            neighbour.hCost = CalculateDistance(neighbour, endNode);
                            neighbour.CalculateFCost();

                            if (!openList.Contains(neighbour))
                                openList.Add(neighbour);
                        }
                    }
                }
                return null;
            }
            int CalculateDistance(PathNode a, PathNode b)
            {
                int xDistance = Mathf.Abs(a.x - b.x);
                int yDistance = Mathf.Abs(a.y - b.y);
                int remaining = Mathf.Abs(xDistance - yDistance);
                return remaining * STRAIGHT_COST + Mathf.Min(xDistance, yDistance) * DIAGONAL_COST;
            }
            PathNode LowestFCost(List<PathNode> nodeList)
            {
                PathNode lowestCost = nodeList[0];
                for (int i = 0; i < nodeList.Count; i++)
                {
                    if (nodeList[i].fCost < lowestCost.fCost)
                    {
                        lowestCost = nodeList[i];
                    }
                }
                return lowestCost;
            }
            List<PathNode> CalculatePath(PathNode end)
            {
                List<PathNode> path = new() { end };
                PathNode current = end;
                while (current.pastNode != null)
                {
                    path.Add(current.pastNode);
                    current = current.pastNode;
                }
                path.Reverse();
                return path;
            }
            List<PathNode> GetNeighbours(PathNode node)
            {
                List<PathNode> neighbours = new();
                if (node.x - 1 >= 0 && grid.GetValue(node.x - 1, node.y).isWalkable == true) //Left
                {
                    neighbours.Add(grid.GetValue(node.x - 1, node.y));
                    if (node.y - 1 >= 0)//LeftDown
                    {
                        if (grid.GetValue(node.x - 1, node.y - 1).isWalkable == true && grid.GetValue(node.x, node.y - 1).isWalkable == true)
                            neighbours.Add(grid.GetValue(node.x - 1, node.y - 1));
                    }
                    if (node.y + 1 < grid.GetHeight())//LeftUp
                    {
                        if (grid.GetValue(node.x - 1, node.y + 1).isWalkable == true && grid.GetValue(node.x, node.y + 1).isWalkable == true)
                            neighbours.Add(grid.GetValue(node.x - 1, node.y + 1));
                    }
                }
                if (node.x + 1 < grid.GetWidth() && grid.GetValue(node.x + 1, node.y).isWalkable == true)//Right
                {
                    neighbours.Add(grid.GetValue(node.x + 1, node.y));
                    if (node.y - 1 >= 0)//RightDown
                    {
                        if (grid.GetValue(node.x + 1, node.y - 1).isWalkable == true && grid.GetValue(node.x, node.y - 1).isWalkable == true)
                            neighbours.Add(grid.GetValue(node.x + 1, node.y - 1));
                    }
                    if (node.y + 1 < grid.GetHeight())//RightUp
                    {
                        if (grid.GetValue(node.x + 1, node.y + 1).isWalkable == true && grid.GetValue(node.x, node.y + 1).isWalkable == true)
                            neighbours.Add(grid.GetValue(node.x + 1, node.y + 1));
                    }
                }
                if (node.y - 1 >= 0 && grid.GetValue(node.x, node.y - 1).isWalkable == true)//Down
                    neighbours.Add(grid.GetValue(node.x, node.y - 1));
                if (node.y + 1 < grid.GetHeight() && grid.GetValue(node.x, node.y + 1).isWalkable == true)//Up
                    neighbours.Add(grid.GetValue(node.x, node.y + 1));
                return neighbours;
            }
            public void SetWalkable(int x, int y, bool isWalkable)
            {
                grid.GetValue(x, y).isWalkable = isWalkable;
            }

            public void SetWalkable(Vector3 worldPosition, bool isWalkable)
            {
                PathNode node = grid.GetValue(worldPosition);
                node.isWalkable = isWalkable;
            }
        }
    }
}
