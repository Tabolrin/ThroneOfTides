using System;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Drives the lightning-strike VFX sequence for Silas Deepbound's attack.
    ///
    /// Sequence overview:
    ///   1. Cloud fades in above target.
    ///   2. Lightning fill sweeps top→bottom (very fast).
    ///   3. Strike particle fires at _strikeAnchor world position + screen whiteout flash.
    ///   4. FEEL feedback fires.
    ///   5. Everything fades out together via CanvasGroup.
    ///
    /// Scene setup requirements:
    ///   - _cloudImage      : Image with alpha driven by color.a.
    ///   - _lightningImage  : Image, FillMethod = Vertical, FillOrigin = Top.
    ///   - _strikeAnchor    : Empty child of LightningImage, placed at its bottom tip.
    ///   - _canvasGroup     : CanvasGroup on this root — drives combined fade-out.
    ///   - _whiteoutImage   : Full-screen scene Image — cannot be baked into prefab,
    ///                        must be passed via Inject(). Root Canvas child, last sibling,
    ///                        Raycast Target OFF, color (1,1,1,0) at rest.
    ///   - _strikeParticles : Scene-level world-space ParticleSystem, passed via Inject().
    /// </summary>
    public class LightningVFXController : MonoBehaviour, ICardPlayEffect
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Image         _cloudImage;
        [SerializeField] private Image         _lightningImage;
        [SerializeField] private RectTransform _strikeAnchor;  // bottom tip of lightning sprite
        [SerializeField] private CanvasGroup   _canvasGroup;

        [Header("FEEL")]
        [SerializeField] private MMF_Player _feedbackLightningStrike;

        [Header("Spawn Offset (canvas units, applied left of target)")]
        [SerializeField] private Vector2 _canvasSpawnOffset = new Vector2(-80f, 0f);

        [Header("Cloud Fade In")]
        [SerializeField] private float _cloudFadeDuration = 0.4f;
        [SerializeField] private Ease  _cloudFadeEase     = Ease.OutQuad;

        [Header("Hold Before Strike")]
        [SerializeField] private float _holdBeforeStrike = 0.15f;

        [Header("Lightning Strike")]
        // Very fast — sells the instantaneous nature of lightning.
        [SerializeField] private float _strikeFillDuration = 0.08f;
        [SerializeField] private Ease  _strikeFillEase     = Ease.InQuart;

        [Header("Whiteout Flash")]
        [SerializeField] private float _whiteoutHoldDuration = 0.06f;
        [SerializeField] private float _whiteoutFadeDuration = 0.18f;

        [Header("Hold After Strike")]
        [SerializeField] private float _holdAfterStrike = 0.25f;

        [Header("Fade Out")]
        [SerializeField] private float _fadeOutDuration = 0.35f;
        [SerializeField] private Ease  _fadeOutEase     = Ease.InQuad;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired at the lightning strike peak. Apply damage here.</summary>
        public event Action OnStrikeMoment;

        /// <summary>Fired when fully faded. Safe to destroy or return to pool.</summary>
        public event Action OnSequenceEnd;

        /// <summary>ICardPlayEffect — fired when fully faded, so CardPresentationPlayer destroys the instance.</summary>
        public event Action Completed;

        // ── Private ───────────────────────────────────────────────────────────

        private RectTransform  _rectTransform;
        private RectTransform  _rootCanvasRect;
        private Camera         _gameCamera;
        private ParticleSystem _strikeParticles; // scene-level world-space, injected
        private Image          _whiteoutImage;   // scene-level full-screen Image, injected

        private Sequence _seq;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDestroy() => _seq?.Kill();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Injected by CardPresentationPlayer after instantiation.
        /// Both strikeParticles and whiteoutImage are persistent scene objects —
        /// prefabs cannot hold references to scene objects, so both must be injected.
        /// </summary>
        public void Inject(RectTransform canvasRect, Camera gameCamera,
                           ParticleSystem strikeParticles, Image whiteoutImage)
        {
            _rootCanvasRect  = canvasRect;
            _gameCamera      = gameCamera;
            _strikeParticles = strikeParticles;
            _whiteoutImage   = whiteoutImage;
        }

        /// <summary>
        /// ICardPlayEffect entry point — hosted by CardPresentationPlayer. Lightning always
        /// strikes the opponent's ship, matching this card's authored PresentationEntry
        /// (AnchorSide: Opponent).
        /// </summary>
        public void Initialize(CardEffectSpawnContext context)
        {
            Inject(context.GameCanvas, context.GameCamera, context.LightningStrikeParticles, context.WhiteoutImage);
            OnSequenceEnd += () => Completed?.Invoke();
            StartSequence(context.OpponentAnchor.position);
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
            // Convert _strikeAnchor's screen position to world space so the particle
            // system fires exactly at the lightning tip regardless of canvas scale or
            // resolution. Matches the Siren _mouthAnchor pattern.
            // null camera is correct for Screen Space Overlay.
            // Z placed just in front of the camera so particles are visible.
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, _strikeAnchor.position);
            Vector3 worldPoint  = _gameCamera.ScreenToWorldPoint(
                new Vector3(screenPoint.x, screenPoint.y, _gameCamera.nearClipPlane + 1f));

            _strikeParticles.transform.position = worldPoint;
        }

        // ── Sequence ──────────────────────────────────────────────────────────

        private Sequence BuildSequence()
        {
            Sequence seq = DOTween.Sequence();

            // Phase 1 — Cloud fades in.
            seq.Append(DOTween.To(
                    () => _cloudImage.color.a,
                    (float x) => SetCloudAlpha(x),
                    1f, _cloudFadeDuration)
                .SetEase(_cloudFadeEase));

            seq.AppendInterval(_holdBeforeStrike);

            // Phase 2 — Lightning fills top→bottom (FillOrigin = Top set in Inspector).
            seq.Append(DOTween.To(
                    () => _lightningImage.fillAmount,
                    (float x) => _lightningImage.fillAmount = x,
                    1f, _strikeFillDuration)
                .SetEase(_strikeFillEase));

            // Phase 3 — Strike peak: position particles at anchor, then fire everything.
            // Particles positioned here so the anchor's canvas position is fully resolved
            // after the prefab has been placed and laid out.
            seq.AppendCallback(() =>
            {
                PositionParticles();
                OnStrikePeak();
            });

            // Phase 4 — Hold whiteout briefly, then fade it out independently
            // so it doesn't block the master sequence timeline.
            seq.AppendInterval(_whiteoutHoldDuration);
            seq.AppendCallback(BeginWhiteoutFade);

            seq.AppendInterval(_holdAfterStrike);

            // Phase 5 — Fade out cloud + lightning together via CanvasGroup.
            seq.Append(DOTween.To(
                    () => _canvasGroup.alpha,
                    (float x) => _canvasGroup.alpha = x,
                    0f, _fadeOutDuration)
                .SetEase(_fadeOutEase));

            seq.OnComplete(() => OnSequenceEnd?.Invoke());

            return seq;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ResetVisuals()
        {
            _seq?.Kill();

            SetCloudAlpha(0f);
            _lightningImage.fillAmount = 0f;
            _canvasGroup.alpha         = 1f;
            SetWhiteoutAlpha(0f);

            _strikeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnStrikePeak()
        {
            _strikeParticles.Play();
            SetWhiteoutAlpha(1f);
            _feedbackLightningStrike?.PlayFeedbacks();
            OnStrikeMoment?.Invoke();
        }

        /// <summary>
        /// Independent tween so the whiteout fades in parallel with the
        /// master sequence's hold-after-strike interval without stalling it.
        /// </summary>
        private void BeginWhiteoutFade()
        {
            DOTween.To(
                    () => _whiteoutImage.color.a,
                    (float x) => SetWhiteoutAlpha(x),
                    0f, _whiteoutFadeDuration)
                .SetEase(Ease.OutQuad);
        }

        // Isolated alpha setters preserve the Inspector-assigned RGB tint.
        private void SetCloudAlpha(float a)
        {
            Color c = _cloudImage.color;
            c.a = a;
            _cloudImage.color = c;
        }

        private void SetWhiteoutAlpha(float a)
        {
            Color c = _whiteoutImage.color;
            c.a = a;
            _whiteoutImage.color = c;
        }
    }
}