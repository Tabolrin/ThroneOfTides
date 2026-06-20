using System;
using UnityEngine;
using DG.Tweening;

namespace ThroneOfTides.Systems.VFX
{
    public class KrakenTentacleAnimator : MonoBehaviour
    {
        [Header("Angles (degrees)")]
        [Tooltip("How far back from initial rotation the tentacle cocks before striking.")]
        [SerializeField] private float _windUpDegrees = 45f;

        [Tooltip("How far forward from initial rotation the tentacle ends up after striking. " +
                 "Net visible arc = windUp + this value.")]
        [SerializeField] private float _strikeDegrees = 110f;  // 45 back + 140 forward = 95 net from start

        [Header("Timing")]
        [SerializeField] private float _windUpDuration = 0.35f;
        [SerializeField] private float _strikeDuration = 0.2f;

        [Header("Eases")]
        [SerializeField] private Ease _windUpEase = Ease.OutQuad;
        [SerializeField] private Ease _strikeEase = Ease.InQuart;

        // Total duration the controller must wait before starting sink
        public float TotalAttackDuration => _windUpDuration + _strikeDuration;

        private float    _initialZ;
        private float    _trackedZ;
        private Sequence _sequence;

        private void Awake()
        {
            // Store initial Z in signed range so offset arithmetic never wraps
            _initialZ = SignedAngle(transform.localEulerAngles.z);
            _trackedZ = _initialZ;
        }

        private void OnDestroy() => _sequence?.Kill(false);

        public void PlayAttack(Action onStrikeComplete)
        {
            _sequence?.Kill(false);

            // Always start from the true initial rotation
            _trackedZ = _initialZ;
            ApplyZ(_trackedZ);

            float windUpTarget = _initialZ + _windUpDegrees;   // cock back
            float strikeTarget = _initialZ - _strikeDegrees;   // slam forward past start

            _sequence = DOTween.Sequence();

            // Phase 1 — wind up
            _sequence.Append(
                DOTween.To(() => _trackedZ, ApplyZ, windUpTarget, _windUpDuration)
                    .SetEase(_windUpEase)
            );

            // Phase 2 — strike, fires callback on completion, then holds forever
            _sequence.Append(
                DOTween.To(() => _trackedZ, ApplyZ, strikeTarget, _strikeDuration)
                    .SetEase(_strikeEase)
                    .OnComplete(() => onStrikeComplete?.Invoke())
            );

            _sequence.Play();
        }

        public void ResetState()
        {
            _sequence?.Kill(false);
            _trackedZ = _initialZ;
            ApplyZ(_trackedZ);
        }

        private void ApplyZ(float z)
        {
            _trackedZ = z;
            Vector3 angles = transform.localEulerAngles;
            angles.z = z;
            transform.localEulerAngles = angles;
        }

        // Converts 0-360 to signed -180..180 to prevent wrap-around arithmetic errors
        private static float SignedAngle(float angle)
        {
            while (angle > 180f)  angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }
    }
}