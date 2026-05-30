using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gamelib.EventSystem
{
    public class GameEvent
    {
        //비어있는 클래스    
    }
    
    [CreateAssetMenu(fileName = "Event channel", menuName = "Events/Channel", order = 0)]
    public class EventChannelSO : ScriptableObject
    {
        private Dictionary<Type, Action<GameEvent>> _events = new();
        private Dictionary<Delegate, Action<GameEvent>> _lookup = new(); //이미 구독중인 매서드인지 확인하기 위함.

        public void AddListener<T>(Action<T> handler) where T : GameEvent
        {
            if (_lookup.ContainsKey(handler)) return; //이미 구독중인 매서드가 또 구독되지 않게 막는다.
            
            Action<GameEvent> wrappedHandler = e => handler(e as T);
            _lookup[handler] = wrappedHandler;
            
            Type evtType = typeof(T);
            if (!_events.TryAdd(evtType, wrappedHandler)) //이미 누군가 구독중인 이벤트라면
            {
                _events[evtType] += wrappedHandler; //추가 구독
            }
        }

        public void RemoveListener<T>(Action<T> handler) where T : GameEvent
        {
            Type evtType = typeof(T);
            if (!_lookup.TryGetValue(handler, out Action<GameEvent> wrappedHandler)) return;

            if (_events.TryGetValue(evtType, out Action<GameEvent> evtHandler))
            {
                evtHandler -= wrappedHandler;
                if(evtHandler == null)
                    _events.Remove(evtType);
                else
                    _events[evtType] = evtHandler;
            }
            _lookup.Remove(handler);
        }

        public void RaiseEvent(GameEvent evt)
        {
            if(_events.TryGetValue(evt.GetType(), out Action<GameEvent> evtHandler))
                evtHandler?.Invoke(evt);
        }

        public void Clear()
        {
            _events.Clear();
            _lookup.Clear();
        }
    }
}