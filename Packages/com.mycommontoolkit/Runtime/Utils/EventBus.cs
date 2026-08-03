using System.Collections.Generic;
using System;
namespace MyCommonToolkit
{
    namespace Events
    {
        public static class EventBus
        {
            static readonly Dictionary<Type, List<Delegate>> subs = new();
            public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
            {
                Type t=typeof(T);
                if(!subs.TryGetValue(t, out List<Delegate> list))
                {
                    list = new();
                    subs[t]=list;
                }
                list.Add(handler);
            }
            public static void UnSubscribe<T>(Action<T> handler) where T : IGameEvent
            {
                Type t = typeof(T);
                if (subs.TryGetValue(t, out List<Delegate> list))
                {
                    list.Remove(handler);
                    if(list.Count==0) 
                        subs.Remove(t);
                }
                list.Add(handler);
            }
            public static void Publish<T>(T myEvent) where T : IGameEvent
            {
                Type t = typeof(T);
                if (subs.TryGetValue(t, out List<Delegate> list))
                {
                    Delegate[] copy= list.ToArray();
                    foreach(Delegate d in copy)
                    {
                        ((Action<T>)d)(myEvent);
                    }
                }
            }
        }
        public interface IGameEvent{}
    }
}
