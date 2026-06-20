using System;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.Systems.VFX
{
    public class SirenVFXController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Image         _sirenImage;
        [SerializeField] private RectTransform _mouthAnchor; // child of SirenBody, positioned at the siren's mouth

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

        private RectTransform  _rectTransform;
        private RectTransform  _rootCanvasRect;
        private Camera         _gameCamera;
        private ParticleSystem _musicNoteParticles; // scene-level world-space object, injected
        private Vector2        _restPosition;
        private Sequence       _seq;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake() => _rectTransform = GetComponent<RectTransform>();

        private void OnDestroy() => _seq?.Kill();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Injected by CardVFXHandler after instantiation.
        /// musicNoteParticles is a persistent scene-level world-space ParticleSystem —
        /// sized once in the editor and repositioned each use. Never destroyed.
        /// </summary>
        public void Inject(RectTransform canvasRect, Camera gameCamera, ParticleSystem musicNoteParticles)
        {
            _rootCanvasRect     = canvasRect;
            _gameCamera         = gameCamera;
            _musicNoteParticles = musicNoteParticles;
        }

        // Named StartSequence rather than Play to avoid conflict with DOTween's
        // Play<T> extension method injected on all MonoBehaviours at compile time.
        public void StartSequence(Vector3 worldPosition)
        {
            PositionAtWorldPoint(worldPosition);
            _restPosition = _rectTransform.anchoredPosition;
            PositionParticles();
            ResetVisuals();
            BuildSequence();
        }

        // ── Positioning ───────────────────────────────────────────────────────

        private void PositionAtWorldPoint(Vector3 worldPosition)
        {
            Vector2 screenPoint = _gameCamera.WorldToScreenPoint(worldPosition);

            // null camera converts correctly for Screen Space Overlay canvases.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect, screenPoint, null, out Vector2 localPoint);

            _rectTransform.anchoredPosition = localPoint + _canvasSpawnOffset;
        }

        private void PositionParticles()
        {
            // Convert MouthAnchor's screen position to world space so the particle
            // system sits exactly at the siren's mouth regardless of canvas scale or
            // resolution. null camera is correct for Screen Space Overlay.
            // Z placed just in front of the game camera so particles are visible.
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, _mouthAnchor.position);
            Vector3 worldPoint  = _gameCamera.ScreenToWorldPoint(
                new Vector3(screenPoint.x, screenPoint.y, _gameCamera.nearClipPlane + 1f));

            _musicNoteParticles.transform.position = worldPoint;
        }

        // ── Sequence ──────────────────────────────────────────────────────────

        private void BuildSequence()
        {
            // Start below rest position — siren rises into frame while filling
            // bottom-to-top, giving the illusion of emerging from the water.
            // Image FillOrigin must be set to Bottom in the Inspector.
            _rectTransform.anchoredPosition = _restPosition - new Vector2(0f, _riseDistance);

            _seq = DOTween.Sequence();

            // Phase 1 — Rise: move upward to rest while filling 0→1.
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

            // Phase 4 — Sink: move downward while unfilling 1→0, reversing the rise.
            _seq.Append(_rectTransform
                .DOAnchorPosY(_restPosition.y - _riseDistance, _sinkDuration)
                .SetEase(_sinkEase));
            _seq.Join(DOTween.To(
                    () => _sirenImage.fillAmount,
                    (float x) => _sirenImage.fillAmount = x,
                    0f, _sinkDuration)
                .SetEase(_sinkEase));

            // Phase 5 — Gone: stop particles (scene object — never destroyed) and notify caller.
            _seq.AppendCallback(() =>
            {
                _musicNoteParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                OnSequenceEnd?.Invoke();
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ResetVisuals()
        {
            _sirenImage.fillAmount = 0f;
            _musicNoteParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}