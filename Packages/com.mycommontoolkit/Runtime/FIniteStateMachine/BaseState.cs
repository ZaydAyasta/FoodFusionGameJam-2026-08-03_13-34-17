using UnityEngine;

namespace MyCommonToolkit
{
    namespace FiniteStateMachine{
        public abstract class BaseState : IState
        {
            protected MonoBehaviour controller;
            protected BaseState(MonoBehaviour controller)
            {
                this.controller = controller;
            }
            public virtual void Enter()
            {
            }

            public virtual void Exit()
            {
            }

            public virtual void LogicUpdate()
            {
            }

            public virtual void PhysicsUpdate()
            {
            }
        }
    }
}
