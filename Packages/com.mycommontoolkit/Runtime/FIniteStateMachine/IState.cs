namespace MyCommonToolkit
{
    namespace FiniteStateMachine{
        public interface IState
        {
            public void Enter();
            public void Exit();
            public void LogicUpdate();
            public void PhysicsUpdate();
        }
    }
}
