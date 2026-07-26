using System;
using System.Collections.Generic;
using UnityEngine;

namespace Firebreak
{
    [CreateAssetMenu(fileName = "New Game Event", menuName = "Firebreak/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<Action> _listeners = new();

        public void Register(Action listener)
        {
            if (listener != null && !_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unregister(Action listener)
        {
            _listeners.Remove(listener);
        }

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke();
        }
    }
}
