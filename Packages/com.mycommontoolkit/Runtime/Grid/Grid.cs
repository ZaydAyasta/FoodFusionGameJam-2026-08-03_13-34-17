using UnityEngine;
namespace MyCommonToolkit
{
    public class Grid<T>
    {
        int height;
        int width;
        float cellSize;
        Vector3 originPos;
        T[,] gridArray;
        public Grid(int height, int width, float cellSize, Vector3 originPos, System.Func<int, int, T> createGridObject)
        {
            this.height = height;
            this.width = width;
            if (cellSize >= 0)
                this.cellSize = cellSize;
            else Debug.LogError("cellSize cannot be negative");
            this.originPos = originPos;
            gridArray = new T[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gridArray[x, y] = createGridObject(x, y);
                }
            }
        }
        public int GetHeight() => height;
        public int GetWidth() => width;
        public float GetCellSize() => cellSize;
        public Vector3 GetOrigin() => originPos;
        public Vector3 SetOrigin(Vector3 origin) => originPos = origin;
        public Vector2 GetWorldPos(int x, int y) => new(originPos.x + x * cellSize, originPos.y + y * cellSize);
        public Vector2 GetCenter(Vector2 cellPos)
        {
            GetCell(cellPos, out int x, out int y);
            Vector2 pos = GetWorldPos(x, y);
            return new(pos.x + cellSize / 2, pos.y + cellSize / 2);
        }
        public Vector2 GetCenter(int x, int y)
        {
            Vector2 pos = GetWorldPos(x, y);
            return new(pos.x + cellSize / 2, pos.y + cellSize / 2);
        }
        public void SetOriginToWorldCenter()
        {
            originPos = new Vector3(-height * cellSize / 2, -width * cellSize / 2);
        }
        public Vector3 WorldSpaceToGrid(Vector3 worldPos) => worldPos + originPos * cellSize;
        public Vector3 GridToWorldSpace(Vector3 pos) => pos - originPos * cellSize;
        public void GetCell(Vector2 WorldPos, out int x, out int y)
        {
            x = Mathf.FloorToInt((WorldPos.x - originPos.x) / cellSize);
            y = Mathf.FloorToInt((WorldPos.y - originPos.y) / cellSize);
        }
        public bool IsInsideGrid(int x, int y)
        {
            bool isInside = x < width && x >= 0 && y >= 0 && y < height;
            return isInside;
        }
        public void SetValue(int x, int y, T value)
        {
            gridArray[x, y] = value;
        }
        public T GetValue(int x, int y)
        {
            return gridArray[x, y];
        }
        public void SetValue(Vector3 worldPos, T value)
        {
            int x, y;
            GetCell(worldPos, out x, out y);
            SetValue(x, y, value);
        }
        public T GetValue(Vector3 worldPos)
        {
            int x, y;
            GetCell(worldPos, out x, out y);
            return GetValue(x, y);
        }
    }
}