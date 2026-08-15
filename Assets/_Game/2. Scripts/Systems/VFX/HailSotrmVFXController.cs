using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Drives the hailstorm VFX for a weather-based DOT attack. Unlike the other one-shot VFX
    /// controllers, this one stays alive for the DOT's whole duration instead of a fixed sequence:
    ///
    ///   1. Cloud fades in, hail particles start looping.
    ///   2. Stays looping - tracks GameEventBus.OnShipStatusCountChanged for HailStorm on the
    ///      target ship, so it keeps playing across turns for exactly as many turns as the
    ///      card's DOT duration says (no hardcoded turn count here).
    ///   3. When that count reaches 0 (DOT expired), hail stops and cloud fades out.
    ///
    /// Scene setup requirements:
    ///   - _cloudImage      : Image with alpha driven by color.a.
    ///   - _hailAnchor      : Empty RectTransform child of CloudImage, placed at
    ///                        the bottom edge - converted to world space for particles.
    ///   - _hailParticles   : Scene-level world-space ParticleSystem, passed via Inject().
    ///                        Needs its own Looping enabled so it rains continuously while active.
    ///                        Never destroyed - stopped and cleared after each use.
    /// </summary>
    public class HailstormVFXController : MonoBehaviour, ICardPlayEffect
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Image         _cloudImage;
        [SerializeField] private RectTransform _hailAnchor; // bottom edge of cloud sprite

        [Header("SFX")]
        [SerializeField] private CardSfxCue _hailstormSfx;

        [Header("Spawn Offset (canvas units, applied left of target)")]
        [SerializeField] private Vector2 _canvasSpawnOffset = new Vector2(-80f, 0f);

        [Header("Cloud Fade In")]
        [SerializeField] private float _cloudFadeInDuration = 0.4f;
        [SerializeField] private Ease  _cloudFadeInEase     = Ease.OutQuad;

        [Header("Cloud Fade Out")]
        [SerializeField] private float _cloudFadeOutDuration = 0.5f;
        [SerializeField] private Ease  _cloudFadeOutEase     = Ease.InQuad;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired once when the hail starts looping. Apply the initial hit here if needed.</summary>
        public event Action OnAttackMoment;

        /// <summary>Fired when fully faded out (DOT expired). Safe to destroy or return to pool.</summary>
        public event Action OnSequenceEnd;

        /// <summary>ICardPlayEffect - fired when fully faded, so CardPresentationPlayer destroys the instance.</summary>
        public event Action Completed;

        // ── Private ───────────────────────────────────────────────────────────

        private RectTransform  _rectTransform;
        private RectTransform  _rootCanvasRect;
        private Camera         _gameCamera;
        private ParticleSystem _hailParticles; // scene-level world-space, injected

        private DamageTarget _targetShip;
        private bool         _subscribed;
        private Tween        _fadeTween;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            _fadeTween?.Kill();
            Unsubscribe();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Injected by CardPresentationPlayer after instantiation.
        /// hailParticles : persistent world-space ParticleSystem, repositioned each use.
        /// </summary>
        public void Inject(RectTransform canvasRect, Camera gameCamera, ParticleSystem hailParticles)
        {
            _rootCanvasRect = canvasRect;
            _gameCamera     = gameCamera;
            _hailParticles  = hailParticles;
        }

        /// <summary>
        /// ICardPlayEffect entry point - hosted by CardPresentationPlayer. Hail Storm always
        /// strikes the opponent's ship, matching this card's authored PresentationEntry
        /// (AnchorSide: Opponent). Stays alive across turns until GameState's DOT tracking
        /// reports the HailStorm status on that ship has run out.
        /// </summary>
        public void Initialize(CardEffectSpawnContext context)
        {
            Inject(context.GameCanvas, context.GameCamera, context.HailParticles);

            _targetShip = context.Caster == CardCasterFilter.Player ? DamageTarget.Enemy : DamageTarget.Player;

            GameEventBus.OnShipStatusCountChanged += OnStatusCountChanged;
            _subscribed = true;

            StartSequence(context.OpponentAnchor.position);
        }

        // ── Start / Stop ──────────────────────────────────────────────────────

        // Named StartSequence to match the controller convention and avoid
        // conflict with DOTween's Play<T> extension on MonoBehaviours.
        public void StartSequence(Vector3 worldPosition)
        {
            PositionAtWorldPoint(worldPosition);
            SetCloudAlpha(0f);

            _fadeTween?.Kill();
            _fadeTween = DOTween.To(
                    () => _cloudImage.color.a,
                    SetCloudAlpha,
                    1f, _cloudFadeInDuration)
                .SetEase(_cloudFadeInEase)
                .OnComplete(() =>
                {
                    PositionParticles();
                    _hailParticles.Play();
                    CardSfxPlayer.Play(_hailstormSfx, transform.position);
                    OnAttackMoment?.Invoke();
                });
        }

        private void OnStatusCountChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (type != ShipStatusType.HailStorm || ship != _targetShip || count > 0) return;

            // DOT expired - stop raining and fade the cloud out.
            Unsubscribe();
            _hailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            _fadeTween?.Kill();
            _fadeTween = DOTween.To(
                    () => _cloudImage.color.a,
                    SetCloudAlpha,
                    0f, _cloudFadeOutDuration)
                .SetEase(_cloudFadeOutEase)
                .OnComplete(() =>
                {
                    OnSequenceEnd?.Invoke();
                    Completed?.Invoke();
                });
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            GameEventBus.OnShipStatusCountChanged -= OnStatusCountChanged;
            _subscribed = false;
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

        // ── Helpers ───────────────────────────────────────────────────────────

        // Isolated alpha setter preserves the Inspector-assigned RGB tint.
        private void SetCloudAlpha(float a)
        {
            Color c = _cloudImage.color;
            c.a = a;
            _cloudImage.color = c;
        }
    }
}
