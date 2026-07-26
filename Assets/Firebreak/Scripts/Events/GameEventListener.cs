using UnityEngine;
using UnityEngine.Events;

namespace Firebreak
{
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent _event;
        [SerializeField] private UnityEvent _response;

        private void OnEnable() => _event?.Register(OnRaised);
        private void OnDisable() => _event?.Unregister(OnRaised);

        private void OnRaised() => _response?.Invoke();
    }
}
