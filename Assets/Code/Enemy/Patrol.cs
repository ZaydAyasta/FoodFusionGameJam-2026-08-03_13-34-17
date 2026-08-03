using UnityEngine;
using MyCommonToolkit.FiniteStateMachine;
using MyCommonToolkit.Astar;
using System.Collections.Generic;

public class Patrol : BaseState
{
    Rigidbody2D rb;
    PatrolData data;
    PathFinding pathFinding;
    Queue<Vector3> points = new();
    int attemptCounter;

    public Patrol(MonoBehaviour controller, Rigidbody2D rb, PatrolData data, PathFinding pathFinding) : base(controller)
    {
        this.rb = rb;
        this.data = data;
        this.pathFinding = pathFinding;
    }

    public override void PhysicsUpdate()
    {
        if (rb == null || data == null || pathFinding == null || pathFinding.map == null)
            return;

        if (points.Count == 0)
            points = FindPatrolPoint();

        if (points.Count == 0)
            return;

        GoToPoint();
    }

    void GoToPoint()
    {
        Vector3 point = points.Peek();
        rb.MovePosition(Vector2.MoveTowards(rb.position, point, data.speed * Time.fixedDeltaTime));
        if (Vector2.Distance(rb.position, point) < data.toleranceDistance)
            points.Dequeue();
    }

    Queue<Vector3> FindPatrolPoint()
    {
        while (attemptCounter <= data.maxAttempts)
        {
            attemptCounter++;
            Vector2 offset = Random.insideUnitCircle * data.range;
            Vector2 patrolPoint = (Vector2)controller.transform.position + offset;
            if (!IsInsideMap(patrolPoint))
                continue;

            Queue<Vector3> path = FollowAstar.GetWorldPath(pathFinding.map, controller.transform.position, patrolPoint);
            if (path != null && path.Count > 0)
            {
                attemptCounter = 0;
                return path;
            }
        }

        attemptCounter = 0;
        return new Queue<Vector3>();
    }

    bool IsInsideMap(Vector2 worldPosition)
    {
        pathFinding.map.grid.GetCell(worldPosition, out int x, out int y);
        return pathFinding.map.grid.IsInsideGrid(x, y);
    }
}
