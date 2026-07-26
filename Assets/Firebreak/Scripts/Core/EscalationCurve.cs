using UnityEngine;

namespace Firebreak
{
    [CreateAssetMenu(fileName = "New Escalation Curve", menuName = "Firebreak/Escalation Curve")]
    public class EscalationCurve : ScriptableObject
    {
        [SerializeField] private AnimationCurve _intensityOverNormalizedTime = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Total run length in seconds. Compressed for greybox tuning.")]
        [SerializeField] private float _runLength = 300f;

        public float RunLength => _runLength;

        public float Evaluate(float elapsedSeconds)
        {
            float t = _runLength <= 0f ? 1f : Mathf.Clamp01(elapsedSeconds / _runLength);
            return Mathf.Clamp01(_intensityOverNormalizedTime.Evaluate(t));
        }

        public void SetForTests(AnimationCurve curve, float runLength)
        {
            _intensityOverNormalizedTime = curve;
            _runLength = runLength;
        }
    }
}
