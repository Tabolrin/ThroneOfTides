using System;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

// Assembly: ThroneOfTides.Systems
// Location: Scripts/Systems/VFX/SirenVFXController.cs
// Attach to: SirenVFX prefab root (RectTransform)
//
// Prefab hierarchy:
//   SirenVFX (root)          — this script + RectTransform
//   ├── SirenBody            — UI.Image: ImageType = Filled, FillMethod = Vertical,
//   │                          FillOrigin = Top, Preserve Aspect = ON
//   └── MusicNoteParticles   — ParticleSystem, Play On Awake = OFF
//
// Note: MusicNoteParticles is detached from the Canvas hierarchy at runtime because
//       ParticleSystem cannot render inside a Screen Space Overlay canvas. It is
//       repositioned in world space to match the siren's screen position and
//       destroyed separately when the sequence ends.
//
// Pattern: DOTween Sequence — mirrors KrakenVFXController.
//          Siren descends into frame while filling top-to-bottom (FillOrigin = Top),
//          then ascends while unfilling — giving the illusion of rising from water.
//          _rootCanvasRect and _gameCamera injected at runtime by CardVFXHandler.

namespace ThroneOfTides.Systems.VFX
{
    public class SirenVFXController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Image          _sirenImage;
        [SerializeField] private ParticleSystem _musicNoteParticles;

        [Header("FEEL")]
        [SerializeField] private MMF_Player _feedbackSirenSong;

        [Header("Spawn Offset (canvas units, applied left of target)")]
        [SerializeField] private Vector2 _canvasSpawnOffset = new Vector2(-80f, 0f);

        [Header("Rise")]
        [SerializeField] private float _riseDistance = 200f;
        [SerializeField] private float _riseDuration  = 0.9f;
        [SerializeField] private Ease  _riseEase      = Ease.OutCubic;

        [Header("Hold")]
        [SerializeField] private float _holdDuration = 2.5f;

        [Header("Sink")]
        [SerializeField] private float _sinkDuration = 0.7f;
        [SerializeField] private Ease  _sinkEase     = Ease.InCubic;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired when the siren is fully risen. Apply card effects here.</summary>
        public event Action OnSirenReady;

        /// <summary>Fired when fully sunk. Safe to destroy or return to pool.</summary>
        public event Action OnSequenceEnd;

        // ── Private ───────────────────────────────────────────────────────────

        private RectTransform _rectTransform;
        private RectTransform _rootCanvasRect;
        private Camera        _gameCamera;
        private Vector2       _restPosition;
        private Sequence      _seq;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake() => _rectTransform = GetComponent<RectTransform>();

        private void OnDestroy() => _seq?.Kill();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Injected by CardVFXHandler after instantiation — these are scene objects
        /// that cannot be serialized into the prefab.
        /// </summary>
        public void Inject(RectTransform canvasRect, Camera gameCamera)
        {
            _rootCanvasRect = canvasRect;
            _gameCamera     = gameCamera;
        }

        // Named StartSequence rather than Play to avoid conflict with DOTween's
        // Play<T> extension method injected on all MonoBehaviours at compile time.
        public void StartSequence(Vector3 worldPosition)
        {
            PositionAtWorldPoint(worldPosition);
            _restPosition = _rectTransform.anchoredPosition;
            ResetVisuals();
            DetachParticles();
            BuildSequence();
        }

        // ── Positioning ───────────────────────────────────────────────────────

        private void PositionAtWorldPoint(Vector3 worldPosition)
        {
            Vector2 screenPoint = _gameCamera.WorldToScreenPoint(worldPosition);

            // null camera converts correctly for Screen Space Overlay canvases
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect, screenPoint, null, out Vector2 localPoint);

            _rectTransform.anchoredPosition = localPoint + _canvasSpawnOffset;
        }

        private void DetachParticles()
        {
            // ParticleSystem cannot render inside a Screen Space Overlay canvas.
            // Convert the siren's current screen position to a world position so
            // particles appear at the correct location in the scene.
            Vector3 screenPos  = RectTransformUtility.WorldToScreenPoint(null, _rectTransform.position);
            screenPos.z        = _gameCamera.nearClipPlane + 1f;
            Vector3 worldPos   = _gameCamera.ScreenToWorldPoint(screenPos);

            _musicNoteParticles.transform.SetParent(null);
            _musicNoteParticles.transform.position = worldPos;
        }

        // ── Sequence ──────────────────────────────────────────────────────────

        private void BuildSequence()
        {
            // Start above rest position — siren descends into frame while filling
            // top-to-bottom, giving the illusion of emerging from the water surface.
            // Image FillOrigin must be set to Top in the Inspector.
            _rectTransform.anchoredPosition = _restPosition + new Vector2(0f, _riseDistance);

            _seq = DOTween.Sequence();

            // Phase 1 — Surface: move downward to rest while filling 0→1.
            // Explicit (float x) cast resolves DOTween.To overload ambiguity.
            _seq.Append(_rectTransform
                .DOAnchorPosY(_restPosition.y, _riseDuration)
                .SetEase(_riseEase));
            _seq.Join(DOTween.To(
                    () => _sirenImage.fillAmount,
                    (float x) => _sirenImage.fillAmount = x,
                    1f, _riseDuration)
                .SetEase(_riseEase));

            // Phase 2 — Risen: notify caller, start particles and audio.
            _seq.AppendCallback(() =>
            {
                OnSirenReady?.Invoke();
                _musicNoteParticles.Play();
                _feedbackSirenSong?.PlayFeedbacks();
            });

            // Phase 3 — Hold.
            _seq.AppendInterval(_holdDuration);

            // Phase 4 — Sink: move upward while unfilling 1→0, reversing surface motion.
            _seq.Append(_rectTransform
                .DOAnchorPosY(_restPosition.y + _riseDistance, _sinkDuration)
                .SetEase(_sinkEase));
            _seq.Join(DOTween.To(
                    () => _sirenImage.fillAmount,
                    (float x) => _sirenImage.fillAmount = x,
                    0f, _sinkDuration)
                .SetEase(_sinkEase));

            // Phase 5 — Gone: destroy detached particle object and notify caller.
            _seq.AppendCallback(() =>
            {
                _musicNoteParticles.Stop();
                Destroy(_musicNoteParticles.gameObject);
                OnSequenceEnd?.Invoke();
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ResetVisuals()
        {
            _sirenImage.fillAmount = 0f;

            if (_musicNoteParticles != null)
                _musicNoteParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}