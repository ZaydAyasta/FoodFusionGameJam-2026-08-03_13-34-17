using System.Collections.Generic;
using System;

namespace MyCommonToolkit
{
    namespace FiniteStateMachine
    {
        /// <summary>
        /// Create an object of this class. Create states that inherit from the baseState class then create objects of them.
        /// create conditions by creating object of the PredicateFunc class. Make sure to apply the LogicUpdate(Mandatory) and PhysicsUpdate(optional)
        /// </summary>
        public class StateMachine
        {
            StateNode current;
            Dictionary<Type, StateNode> nodes = new();
            HashSet<ITransition> transitions = new();
            public void LogicUpdate()
            {
                ITransition transition = GetTransition();
                if (transition != null)
                    ChangeState(transition.NextState);
                current.State?.LogicUpdate();
            }
            public void PhysicsUpdate()
            {
                current.State?.PhysicsUpdate();
            }
            public void SetInitialState(IState state)
            {
                GetOrAddNode(state);
                current = nodes[state.GetType()];
                current.State.Enter();
            }
            ITransition GetTransition()
            {
                foreach (var t in transitions)
                {
                    if (t.Predicate.Evaluate())
                        return t;
                }
                foreach (var t in current.Transitions)
                {
                    if (t.Predicate.Evaluate())
                        return t;
                }
                return null;
            }
            public void AddTransition(IState from,IState to,IPredicate condition)
            {
                GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
            }
            public void AddAnyTransition(IState to, IPredicate condition)
            {
                transitions.Add(item:new Transition(GetOrAddNode(to).State,condition));
            }
            StateNode GetOrAddNode(IState state)
            {
                var node= nodes.GetValueOrDefault(key:state.GetType());
                if(node == null)
                {
                    node = new(state);
                    nodes.Add(state.GetType(),node);
                }
                return node;
            }
            void ChangeState(IState state)
            {
                if (state == current.State) return;
                var previousState= current.State;
                var nextState= nodes[state.GetType()].State;
                previousState?.Exit();
                nextState?.Enter();
                current = nodes[state.GetType()];
            }
        }
    }
}
