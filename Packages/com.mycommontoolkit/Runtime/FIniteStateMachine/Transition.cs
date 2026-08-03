namespace MyCommonToolkit
{
    namespace FiniteStateMachine{
        public class Transition : ITransition
        {
            public IState NextState { get; }
            public IPredicate Predicate { get; }
            public Transition(IState nextState, IPredicate predicate)
            {
                NextState = nextState;
                Predicate = predicate;
            }
        }
    }
}
