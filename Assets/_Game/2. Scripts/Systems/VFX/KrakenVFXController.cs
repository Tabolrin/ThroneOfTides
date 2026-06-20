using System;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.Systems.VFX
{
    public class KrakenVFXController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Image                  _bodyImage;
        [SerializeField] private Image                  _tentacleImage;
        [SerializeField] private KrakenTentacleAnimator _tentacleAnimator;
        
        [Header("FEEL")]
        [SerializeField] private MMF_Player _feedbackKrakenAttack;

        [Header("Spawn Offset (canvas units, applied left of target)")]
        [SerializeField] private Vector2 _canvasSpawnOffset = new Vector2(-80f, 0f);

        [Header("Body Rise")]
        [SerializeField] private float _bodyRiseDuration = 0.8f;
        [SerializeField] private float _bodyRiseDistance = 200f;
        [SerializeField] private Ease  _bodyRiseEase     = Ease.OutCubic;

        [Header("Tentacle Rise")]
        [SerializeField] private float _tentacleRiseDuration = 0.5f;
        [SerializeField] private float _tentacleRiseDistance = 120f;
        [SerializeField] private Ease  _tentacleRiseEase     = Ease.OutCubic;

        [Header("Hold Pauses")]
        [SerializeField] private float _holdBeforeAttack = 0.3f;
        [SerializeField] private float _holdAfterAttack  = 0.4f;

        [Header("Sink")]
        [SerializeField] private float _sinkDuration = 0.6f;
        [SerializeField] private Ease  _sinkEase     = Ease.InCubic;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired at the tentacle strike peak. Apply damage here.</summary>
        public event Action OnAttackMoment;

        /// <summary>Fired when fully sunk. Safe to destroy or return to pool.</summary>
        public event Action OnSequenceEnd;

        // ── Private ───────────────────────────────────────────────────────────

        // Rest positions captured after positioning — never overwritten so
        // ResetState always returns images to their post-position anchors.
        private RectTransform _rectTransform;
        private Vector2       _bodyRestPosition;
        private Vector2       _tentacleRestPosition;
        private Sequence      _masterSequence;

        // Injected by CardVFXHandler — scene objects cannot be baked into the prefab.
        private RectTransform _rootCanvasRect;
        private Camera        _gameCamera;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            _bodyRestPosition     = _bodyImage.rectTransform.anchoredPosition;
            _tentacleRestPosition = _tentacleImage.rectTransform.anchoredPosition;
        }

        private void OnDestroy() => _masterSequence?.Kill();

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
            if (_masterSequence != null && _masterSequence.IsActive()) return;

            PositionAtWorldPoint(worldPosition);
            ResetState();
            _masterSequence = BuildSequence();
            _masterSequence.Play();
        }

        // ── Positioning ───────────────────────────────────────────────────────

        private void PositionAtWorldPoint(Vector3 worldPosition)
        {
            Vector2 screenPoint = _gameCamera.WorldToScreenPoint(worldPosition);

            // null camera converts correctly for both Overlay and World Space canvases
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect, screenPoint, null, out Vector2 localPoint);

            _rectTransform.anchoredPosition = localPoint + _canvasSpawnOffset;
        }

        // ── Sequence ──────────────────────────────────────────────────────────

        private Sequence BuildSequence()
        {
            // Start both images below rest so the rise travels upward to rest position.
            _bodyImage.rectTransform.anchoredPosition =
                _bodyRestPosition - new Vector2(0f, _bodyRiseDistance);
            _tentacleImage.rectTransform.anchoredPosition =
                _tentacleRestPosition - new Vector2(0f, _tentacleRiseDistance);

            Sequence seq = DOTween.Sequence();

            // Phase 1 — Body rises: position and fill animate together.
            // Explicit (float x) cast resolves DOTween.To overload ambiguity.
            seq.Append(_bodyImage.rectTransform
                .DOAnchorPosY(_bodyRestPosition.y, _bodyRiseDuration)
                .SetEase(_bodyRiseEase));
            seq.Join(DOTween.To(
                    () => _bodyImage.fillAmount,
                    (float x) => _bodyImage.fillAmount = x,
                    1f, _bodyRiseDuration)
                .SetEase(_bodyRiseEase));

            seq.AppendInterval(_holdBeforeAttack);

            // Phase 2 — Tentacle rises.
            seq.Append(_tentacleImage.rectTransform
                .DOAnchorPosY(_tentacleRestPosition.y, _tentacleRiseDuration)
                .SetEase(_tentacleRiseEase));
            seq.Join(DOTween.To(
                    () => _tentacleImage.fillAmount,
                    (float x) => _tentacleImage.fillAmount = x,
                    1f, _tentacleRiseDuration)
                .SetEase(_tentacleRiseEase));

            // Phase 3 — Attack swing via KrakenTentacleAnimator.
            seq.AppendCallback(() => _tentacleAnimator.PlayAttack(OnAttackPeak));
            // Hold matches the animator's total swing duration to keep sequence timing in sync.
            seq.AppendInterval(_tentacleAnimator.TotalAttackDuration);

            seq.AppendInterval(_holdAfterAttack);

            // Phase 4 — Sink: body and tentacle sink simultaneously.
            seq.Append(_bodyImage.rectTransform
                .DOAnchorPosY(_bodyRestPosition.y - _bodyRiseDistance, _sinkDuration)
                .SetEase(_sinkEase));
            seq.Join(DOTween.To(
                    () => _bodyImage.fillAmount,
                    (float x) => _bodyImage.fillAmount = x,
                    0f, _sinkDuration)
                .SetEase(_sinkEase));
            seq.Join(_tentacleImage.rectTransform
                .DOAnchorPosY(_tentacleRestPosition.y - _tentacleRiseDistance, _sinkDuration)
                .SetEase(_sinkEase));
            seq.Join(DOTween.To(
                    () => _tentacleImage.fillAmount,
                    (float x) => _tentacleImage.fillAmount = x,
                    0f, _sinkDuration)
                .SetEase(_sinkEase));

            seq.OnComplete(() =>
            {
                ResetState();
                OnSequenceEnd?.Invoke();
            });

            return seq;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ResetState()
        {
            _masterSequence?.Kill();

            _bodyImage.fillAmount     = 0f;
            _tentacleImage.fillAmount = 0f;

            _bodyImage.rectTransform.anchoredPosition     = _bodyRestPosition;
            _tentacleImage.rectTransform.anchoredPosition = _tentacleRestPosition;

            _tentacleAnimator.ResetState();
        }

        private void OnAttackPeak()
        {
            _feedbackKrakenAttack?.PlayFeedbacks();
            OnAttackMoment?.Invoke();
        }
    }
}