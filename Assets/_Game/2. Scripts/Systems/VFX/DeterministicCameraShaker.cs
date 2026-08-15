// Assets/_Game/2. Scripts/Systems/VFX/DeterministicCameraShaker.cs
using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Replaces MoreMountains.Feedbacks.MMCameraShaker as the camera's MMCameraShakeEvent
    /// listener. MMCameraShaker drives its shake through MMWiggle's Noise wiggle type, which
    /// randomizes its OWN effective amplitude once per trigger - uniformly between -amplitude
    /// and +amplitude on each axis (see MMWiggle.InitializeRandomValues' RandomizeVector3 call)
    /// - so a shake asking for amplitude 2.7 could just as easily land near 0 as near 2.7 on any
    /// given hit. Measured live: five consecutive identical Level5 triggers produced camera
    /// displacements of 0.008, 1.6, 1.0, 1.7, and 1.8 units - wildly inconsistent, which is what
    /// read as "screen shake sometimes doesn't work."
    /// This still listens on the same MMCameraShakeEvent (so any MMF_CameraShake feedback step
    /// elsewhere keeps working unmodified) but applies the full requested amplitude
    /// deterministically every time - only the noise's per-frame texture varies, never the
    /// overall strength - easing out over the shake's duration.
    /// </summary>
    public class DeterministicCameraShaker : MonoBehaviour
    {
        [Tooltip("Shake intensity over the course of the shake - 1 at the start, 0 at the end.")]
        [SerializeField] private AnimationCurve _falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private Vector3    _restPosition;
        private Coroutine  _shakeRoutine;

        private void Awake() => _restPosition = transform.localPosition;

        private void OnEnable()  => MMCameraShakeEvent.Register(OnCameraShakeEvent);
        private void OnDisable() => MMCameraShakeEvent.Unregister(OnCameraShakeEvent);

        private void OnCameraShakeEvent(float duration, float amplitude, float frequency,
            float amplitudeX, float amplitudeY, float amplitudeZ, bool infinite,
            MMChannelData channelData, bool useUnscaledTime)
        {
            // Per-axis amplitude only if explicitly provided (matches MMCameraShaker's own
            // convention) - every call site in this project passes 0,0,0 and relies on the
            // single shared `amplitude` for both axes.
            Vector2 amp = (amplitudeX != 0f || amplitudeY != 0f)
                ? new Vector2(amplitudeX, amplitudeY)
                : new Vector2(amplitude, amplitude);

            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            transform.localPosition = _restPosition;
            _shakeRoutine = StartCoroutine(ShakeRoutine(duration, amp, frequency, useUnscaledTime));
        }

        private IEnumerator ShakeRoutine(float duration, Vector2 amplitude, float frequency, bool useUnscaledTime)
        {
            // Distinct Perlin seeds per axis so X and Y don't move in lockstep.
            float seedX = Random.Range(0f, 1000f);
            float seedY = Random.Range(0f, 1000f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t        = duration > 0f ? elapsed / duration : 1f;
                float strength = _falloff.Evaluate(t);

                float noiseX = Mathf.PerlinNoise(seedX, elapsed * frequency) * 2f - 1f;
                float noiseY = Mathf.PerlinNoise(seedY, elapsed * frequency) * 2f - 1f;

                transform.localPosition = _restPosition +
                    new Vector3(noiseX * amplitude.x * strength, noiseY * amplitude.y * strength, 0f);

                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            transform.localPosition = _restPosition;
            _shakeRoutine = null;
        }
    }
}
