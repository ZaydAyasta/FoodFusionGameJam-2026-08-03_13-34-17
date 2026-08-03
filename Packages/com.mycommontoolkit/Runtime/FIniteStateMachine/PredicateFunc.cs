using System;

namespace MyCommonToolkit
{
    namespace FiniteStateMachine{
        public class PredicateFunc:IPredicate
        {
            readonly Func<bool> func;
            public PredicateFunc(Func<bool> func){
                this.func = func;
            }
            public bool Evaluate()=>func.Invoke();
        }
    }
}
