using UnityEngine;
using UnityEngine.SceneManagement;

namespace Firebreak
{
    public enum RunPhase { Playing, Won, Lost }

    public class RunManager : MonoBehaviour
    {
        [SerializeField] private EscalationCurve _escalation;
        [SerializeField] private int _requiredSprinklers = 2;
        [SerializeField] private GameEvent _onRunWon;
        [SerializeField] private GameEvent _onRunLost;

        [Header("Wire these scene events to the Notify* handlers via listeners")]
        [SerializeField] private float _retryDelaySeconds = 1.5f;

        public RunPhase Phase { get; private set; } = RunPhase.Playing;

        private float _elapsed;
        private int _sprinklersActive;

        public float Intensity => _escalation != null ? _escalation.Evaluate(_elapsed) : 0f;

        private void Update()
        {
            if (Phase == RunPhase.Playing)
                _elapsed += Time.deltaTime;
        }

        public void NotifySprinklerActivated()
        {
            if (Phase != RunPhase.Playing)
                return;

            _sprinklersActive++;
            if (_sprinklersActive >= _requiredSprinklers)
                Win();
        }

        public void NotifyFarmerStruck()
        {
            if (Phase != RunPhase.Playing)
                return;

            Lose();
        }

        public void NotifyFireReachedHouse()
        {
            if (Phase != RunPhase.Playing)
                return;

            Lose();
        }

        public void Retry()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Win()
        {
            Phase = RunPhase.Won;
            _onRunWon?.Raise();
        }

        private void Lose()
        {
            Phase = RunPhase.Lost;
            _onRunLost?.Raise();
            Invoke(nameof(Retry), _retryDelaySeconds);
        }

        public void ConfigureForTests(EscalationCurve curve, int requiredSprinklers, GameEvent won, GameEvent lost)
        {
            _escalation = curve;
            _requiredSprinklers = requiredSprinklers;
            _onRunWon = won;
            _onRunLost = lost;
        }

        public void SetElapsedForTests(float elapsed) => _elapsed = elapsed;
    }
}
