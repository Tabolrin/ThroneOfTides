using System;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Drives the hailstorm VFX sequence for a weather-based attack.
    ///
    /// Sequence overview:
    ///   1. Cloud fades in.
    ///   2. Brief hold.
    ///   3. Hail particles fire from _hailAnchor world position.
    ///   4. OnAttackMoment fires after _damageDelay seconds.
    ///   5. Particles run for _hailDuration then stop.
    ///   6. Brief hold.
    ///   7. Cloud fades out.
    ///
    /// Scene setup requirements:
    ///   - _cloudImage      : Image with alpha driven by color.a.
    ///   - _hailAnchor      : Empty RectTransform child of CloudImage, placed at
    ///                        the bottom edge — converted to world space for particles.
    ///   - _hailParticles   : Scene-level world-space ParticleSystem, passed via Inject().
    ///                        Never destroyed — stopped and cleared after each use.
    /// </summary>
    public class HailstormVFXController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Image         _cloudImage;
        [SerializeField] private RectTransform _hailAnchor; // bottom edge of cloud sprite

        [Header("FEEL")]
        [SerializeField] private MMF_Player _feedbackHailstorm;

        [Header("Spawn Offset (canvas units, applied left of target)")]
        [SerializeField] private Vector2 _canvasSpawnOffset = new Vector2(-80f, 0f);

        [Header("Cloud Fade In")]
        [SerializeField] private float _cloudFadeInDuration = 0.4f;
        [SerializeField] private Ease  _cloudFadeInEase     = Ease.OutQuad;

        [Header("Hold Before Hail")]
        [SerializeField] private float _holdBeforeHail = 0.3f;

        [Header("Hail")]
        [SerializeField] private float _hailDuration = 0.8f;

        [Header("Damage")]
        // Delay from particle start before damage is applied.
        [SerializeField] private float _damageDelay = 0.3f;

        [Header("Hold After Hail")]
        [SerializeField] private float _holdAfterHail = 0.15f;

        [Header("Cloud Fade Out")]
        [SerializeField] private float _cloudFadeOutDuration = 0.5f;
        [SerializeField] private Ease  _cloudFadeOutEase     = Ease.InQuad;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired _damageDelay seconds after hail begins. Apply damage here.</summary>
        public event Action OnAttackMoment;

        /// <summary>Fired when fully faded. Safe to destroy or return to pool.</summary>
        public event Action OnSequenceEnd;

        // ── Private ───────────────────────────────────────────────────────────

        private RectTransform  _rectTransform;
        private RectTransform  _rootCanvasRect;
        private Camera         _gameCamera;
        private ParticleSystem _hailParticles; // scene-level world-space, injected

        private Sequence _seq;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDestroy() => _seq?.Kill();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Injected by CardVFXHandler after instantiation.
        /// hailParticles : persistent world-space ParticleSystem, repositioned each use.
        /// </summary>
        public void Inject(RectTransform canvasRect, Camera gameCamera, ParticleSystem hailParticles)
        {
            _rootCanvasRect = canvasRect;
            _gameCamera     = gameCamera;
            _hailParticles  = hailParticles;
        }

        // Named StartSequence to match the controller convention and avoid
        // conflict with DOTween's Play<T> extension on MonoBehaviours.
        public void StartSequence(Vector3 worldPosition)
        {
            if (_seq != null && _seq.IsActive()) return;

            PositionAtWorldPoint(worldPosition);
            ResetVisuals();
            _seq = BuildSequence();
            _seq.Play();
        }

        // ── Positioning ───────────────────────────────────────────────────────

        private void PositionAtWorldPoint(Vector3 worldPosition)
        {
            Vector2 screenPoint = _gameCamera.WorldToScreenPoint(worldPosition);

            // null camera is correct for Screen Space Overlay canvases.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect, screenPoint, null, out Vector2 localPoint);

            _rectTransform.anchoredPosition = localPoint + _canvasSpawnOffset;
        }

        private void PositionParticles()
        {
            // Convert _hailAnchor's screen position to world space so particles
            // rain down from the bottom edge of the cloud regardless of canvas
            // scale or resolution. Matches the lightning _strikeAnchor pattern.
            // null camera is correct for Screen Space Overlay.
            // Z placed just in front of the camera so particles are visible.
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, _hailAnchor.position);
            Vector3 worldPoint  = _gameCamera.ScreenToWorldPoint(
                new Vector3(screenPoint.x, screenPoint.y, _gameCamera.nearClipPlane + 1f));

            _hailParticles.transform.position = worldPoint;
        }

        // ── Sequence ──────────────────────────────────────────────────────────

        private Sequence BuildSequence()
        {
            Sequence seq = DOTween.Sequence();

            // Phase 1 — Cloud fades in.
            seq.Append(DOTween.To(
                    () => _cloudImage.color.a,
                    (float x) => SetCloudAlpha(x),
                    1f, _cloudFadeInDuration)
                .SetEase(_cloudFadeInEase));

            seq.AppendInterval(_holdBeforeHail);

            // Phase 2 — Position particles then start hail + FEEL.
            // Positioned here so the anchor's canvas position is fully resolved
            // after the prefab has been placed and laid out.
            seq.AppendCallback(() =>
            {
                PositionParticles();
                _hailParticles.Play();
                _feedbackHailstorm?.PlayFeedbacks();
            });

            // Phase 3 — Damage fires a short moment after particles start
            // so the player sees hail before taking the hit.
            seq.AppendInterval(_damageDelay);
            seq.AppendCallback(() => OnAttackMoment?.Invoke());

            // Phase 4 — Wait out the remainder of the hail duration then stop.
            seq.AppendInterval(_hailDuration - _damageDelay);
            seq.AppendCallback(() =>
                _hailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting));

            seq.AppendInterval(_holdAfterHail);

            // Phase 5 — Cloud fades out.
            seq.Append(DOTween.To(
                    () => _cloudImage.color.a,
                    (float x) => SetCloudAlpha(x),
                    0f, _cloudFadeOutDuration)
                .SetEase(_cloudFadeOutEase));

            seq.OnComplete(() => OnSequenceEnd?.Invoke());

            return seq;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ResetVisuals()
        {
            _seq?.Kill();

            SetCloudAlpha(0f);
            _hailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Isolated alpha setter preserves the Inspector-assigned RGB tint.
        private void SetCloudAlpha(float a)
        {
            Color c = _cloudImage.color;
            c.a = a;
            _cloudImage.color = c;
        }
    }
}