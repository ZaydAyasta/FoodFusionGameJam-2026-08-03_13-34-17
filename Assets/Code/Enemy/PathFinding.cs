using UnityEngine;
using MyCommonToolkit.Astar;

public class PathFinding : MonoBehaviour
{
    [SerializeField] float worldHeight;
    [SerializeField] float worldWidth;
    [SerializeField] float cellSize;
    [SerializeField] LayerMask obstacleLayer;
    public Pathifinding map;

    void Awake()
    {
        RebuildMap();
    }

    public void RebuildMap()
    {
        map = FollowAstar.CreateMap(worldHeight, worldWidth, cellSize);
        FollowAstar.FindWalkable(obstacleLayer, map);
    }

    private void OnDrawGizmos()
    {
        RebuildMap();
        if (map == null || map.grid == null) return;

        for (int x = 0; x < map.grid.GetWidth(); x++)
        {
            for (int y = 0; y < map.grid.GetHeight(); y++)
            {
                Vector2 center2 = map.grid.GetCenter(x, y);
                Vector3 center = new(center2.x, center2.y, 0f);
                var node = map.grid.GetValue(x, y);
                Gizmos.color = node.isWalkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(center, new Vector3(cellSize, cellSize, 0.1f));
            }
        }
    }
}
