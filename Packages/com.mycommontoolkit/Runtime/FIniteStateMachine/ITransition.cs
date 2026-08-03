namespace MyCommonToolkit
{
    namespace FiniteStateMachine{
        public interface ITransition
        {
            IState NextState { get; }
            IPredicate Predicate { get; }
        }
    }
}
