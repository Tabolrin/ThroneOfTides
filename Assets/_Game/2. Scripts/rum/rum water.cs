using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Game._2._Scripts.rum
{
    /// <summary>
    /// Animates the fillAmount of a UI Image to simulate liquid rising/falling,
    /// with an optional idle "wave" wobble and a hit/damage system for health-bar
    /// style usage. Does NOT change the sprite, texture, or any visual asset -
    /// it only animates the existing "Filled" Image component over time.
    ///
    /// Setup:
    /// 1. Attach this script to the same GameObject as your "Liquid" Image
    ///    (the one with Image Type = Filled).
    /// 2. Make sure the Image component is assigned (auto-fetched if left empty).
    /// 3. Call SetFill(...) / Fill(...) / Drain(...) for direct control, or
    ///    TakeHit(...) whenever the player/entity takes damage.
    /// 4. Tune wave amplitude/speed and hit behaviour directly in the Inspector.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LiquidFillAnimator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The UI Image with Image Type = Filled. Auto-assigned if left empty.")]
        [SerializeField] private Image liquidImage;

        [Header("Animation Settings")]
        [Tooltip("How long a full 0->1 (or 1->0) animation takes, in seconds.")]
        [SerializeField] private float defaultDuration = 1f;

        [Tooltip("Curve applied to the animation progress (0 to 1). Leave as linear for a constant speed.")]
        [SerializeField] private AnimationCurve easing = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("If true, starting a new animation will smoothly override any animation in progress.")]
        [SerializeField] private bool allowInterrupt = true;

        [Header("Idle Wave Motion")]
        [Tooltip("Enable a continuous subtle up/down wobble on top of the current fill level.")]
        [SerializeField] private bool waveEnabled = true;

        [Tooltip("How far the wave pushes fillAmount up/down from the target value (0-1 scale). 0.02 = 2%.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float waveAmplitude = 0.015f;

        [Tooltip("How fast the wave oscillates. Higher = faster bobbing.")]
        [Range(0f, 10f)]
        [SerializeField] private float waveSpeed = 1.5f;

        [Header("Hit / Damage Settings")]
        [Tooltip("Default amount (0-1) removed per hit when calling TakeHit() with no argument.")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultHitAmount = 0.1f;

        [Tooltip("How long the drain animation from a hit takes, in seconds.")]
        [SerializeField] private float hitDrainDuration = 0.35f;

        [Tooltip("If true, fill cannot go below 0 (clamped). Turn off only for special cases.")]
        [SerializeField] private bool clampToZero = true;

        /// <summary>Invoked every frame the displayed fill value changes (includes wave), with the current fill amount (0-1).</summary>
        public event Action<float> OnFillChanged;

        /// <summary>Invoked once when a target-fill animation finishes.</summary>
        public event Action<float> OnFillComplete;

        /// <summary>Invoked whenever TakeHit is called, with the new target fill level after the hit.</summary>
        public event Action<float> OnHit;

        /// <summary>Invoked once when the fill level reaches 0 (e.g. "empty" / "dead").</summary>
        public event Action OnEmptied;

        private Coroutine _activeRoutine;
        private float _targetFill;       // The "real" liquid level, without wave applied.
        private bool _emptiedFired;

        /// <summary>Current fill amount actually displayed on the Image (includes wave offset).</summary>
        public float CurrentFill => liquidImage.fillAmount;

        /// <summary>The underlying target fill level (0-1), not including wave wobble.</summary>
        public float TargetFill => _targetFill;

        /// <summary>True while a fill animation is currently running.</summary>
        public bool IsAnimating => _activeRoutine != null;

        private void Awake()
        {
            if (liquidImage == null)
                liquidImage = GetComponent<Image>();

            _targetFill = liquidImage.fillAmount;
        }

        private void Update()
        {
            if (!waveEnabled || waveAmplitude <= 0f)
                return;

            // Only apply wave when not mid-animation, so it doesn't fight the tween.
            if (_activeRoutine != null)
                return;

            float wave = Mathf.Sin(Time.time * waveSpeed) * waveAmplitude;
            float displayed = Mathf.Clamp01(_targetFill + wave);
            liquidImage.fillAmount = displayed;
            OnFillChanged?.Invoke(displayed);
        }

        // ---------------- Direct fill control ----------------

        /// <summary>Animate the liquid to a target fill amount (0-1) over the default duration.</summary>
        public void SetFill(float target) => SetFill(target, defaultDuration);

        /// <summary>Animate the liquid to a target fill amount (0-1) over a custom duration (seconds).</summary>
        public void SetFill(float target, float duration)
        {
            target = Mathf.Clamp01(target);
            _targetFill = target;

            if (!allowInterrupt && _activeRoutine != null)
                return;

            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            if (duration <= 0f)
            {
                ApplyFill(target);
                OnFillComplete?.Invoke(target);
                CheckEmptied();
                return;
            }

            _activeRoutine = StartCoroutine(AnimateFill(target, duration));
        }

        /// <summary>Convenience: animate to full (1) over the default duration.</summary>
        public void Fill() => SetFill(1f);

        /// <summary>Convenience: animate to full (1) over a custom duration.</summary>
        public void Fill(float duration) => SetFill(1f, duration);

        /// <summary>Convenience: animate to empty (0) over the default duration.</summary>
        public void Drain() => SetFill(0f);

        /// <summary>Convenience: animate to empty (0) over a custom duration.</summary>
        public void Drain(float duration) => SetFill(0f, duration);

        /// <summary>Instantly set the fill amount with no animation.</summary>
        public void SetFillImmediate(float value)
        {
            StopAnimation();
            value = Mathf.Clamp01(value);
            _targetFill = value;
            ApplyFill(value);
            OnFillComplete?.Invoke(value);
            CheckEmptied();
        }

        /// <summary>Stop any in-progress animation, leaving the fill at its current value.</summary>
        public void StopAnimation()
        {
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }
        }

        // ---------------- Hit / damage system ----------------

        /// <summary>
        /// Call this whenever the player/entity takes a hit. Drains the liquid by
        /// the Inspector-configured "Default Hit Amount" over "Hit Drain Duration".
        /// </summary>
        public void TakeHit()
        {
            TakeHit(defaultHitAmount, hitDrainDuration);
        }

        /// <summary>Call this with a specific amount (0-1) to remove on this hit.</summary>
        public void TakeHit(float amount)
        {
            TakeHit(amount, hitDrainDuration);
        }

        /// <summary>Call this with a specific amount (0-1) and custom drain duration.</summary>
        public void TakeHit(float amount, float duration)
        {
            float newTarget = _targetFill - Mathf.Abs(amount);
            if (clampToZero)
                newTarget = Mathf.Max(0f, newTarget);

            OnHit?.Invoke(newTarget);
            SetFill(newTarget, duration);
        }

        private void CheckEmptied()
        {
            if (_targetFill <= 0f && !_emptiedFired)
            {
                _emptiedFired = true;
                OnEmptied?.Invoke();
            }
            else if (_targetFill > 0f)
            {
                _emptiedFired = false;
            }
        }

        // ---------------- Internals ----------------

        private IEnumerator AnimateFill(float target, float duration)
        {
            float start = liquidImage.fillAmount;
            float distance = Mathf.Abs(target - start);
            // Scale duration by how far we actually need to travel, so a small
            // change doesn't take as long as a full 0->1 sweep.
            float scaledDuration = duration * Mathf.Max(distance, 0.0001f);

            float elapsed = 0f;
            while (elapsed < scaledDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaledDuration);
                float eased = easing.Evaluate(t);
                ApplyFill(Mathf.Lerp(start, target, eased));
                yield return null;
            }

            ApplyFill(target);
            _activeRoutine = null;
            OnFillComplete?.Invoke(target);
            CheckEmptied();
        }

        private void ApplyFill(float value)
        {
            liquidImage.fillAmount = value;
            OnFillChanged?.Invoke(value);
        }
    }
}