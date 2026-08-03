using System.Collections.Generic;
namespace MyCommonToolkit
{
    namespace FiniteStateMachine{
        public class StateNode
        {
            public IState State { get; }
            public HashSet<ITransition> Transitions { get; }
            public StateNode(IState state)
            {
                State = state;
                Transitions = new();
            }
            public void AddTransition(IState state,IPredicate predicate)=>Transitions.Add(item:new Transition(state,predicate));
        }
    }
}
