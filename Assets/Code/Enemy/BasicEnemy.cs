using UnityEngine;
using MyCommonToolkit.FiniteStateMachine;

public class BasicEnemy : MonoBehaviour
{
    Rigidbody2D rb;
    [SerializeField] PatrolData patrolData;
    [SerializeField] PathFinding pathFinding;
    StateMachine machine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        machine = new StateMachine();
        Patrol patrol = new(this, rb, patrolData, pathFinding);
        machine.SetInitialState(patrol);
    }

    void Update()
    {
        machine?.LogicUpdate();
    }

    void FixedUpdate()
    {
        machine?.PhysicsUpdate();
    }
}
