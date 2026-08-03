namespace MyCommonToolkit
{
    namespace Astar
    {
        public class PathNode
        {
            public int x;
            public int y;
            public int gCost;
            public int hCost;
            public int fCost;

            public PathNode pastNode;
            public bool isWalkable = true;
            public PathNode(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
            public void CalculateFCost() => fCost = gCost + hCost;
        }
    }
}
