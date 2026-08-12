// Assets/_Game/2. Scripts/Systems/VFX/WhaleRamVFXController.cs
using System;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Whale Ram's presentation: a whale cry plays immediately, then the whale sprite (Filled,
    /// Vertical, Fill Origin = Top) fills from 0 up to FillTarget while simultaneously sliding
    /// from a configurable start point to a configurable end point — one diagonal
    /// breach-and-lunge motion. Contact triggers a loud crash SFX and a strong camera shake; the
    /// whale then holds a beat before fading out and destroying itself.
    /// Start/End Point below override wherever CardPresentationPlayer originally spawned this
    /// (per the CardSO's own PresentationEntry) — defaults match the original design (both ends
    /// on the opponent's ship: SeaSurface to its left, then its ShipHit — Whale Ram always hits
    /// the opponent, no target selection).
    /// </summary>
    public class WhaleRamVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Emergence")]
        [Tooltip("Whale cry played the instant the effect spawns.")]
        [SerializeField] private CardSfxCue _emergeSfx;

        [Header("Travel — Start Point")]
        [SerializeField] private CardPresentationSide _startSide = CardPresentationSide.Opponent;
        [SerializeField] private VfxAnchorType _startAnchorType = VfxAnchorType.SeaSurfaceLeft;
        [Tooltip("Extra manual nudge applied after resolving the start anchor, in canvas pixels.")]
        [SerializeField] private Vector2 _startOffset;

        [Header("Travel — End Point")]
        [SerializeField] private CardPresentationSide _endSide = CardPresentationSide.Opponent;
        [SerializeField] private VfxAnchorType _endAnchorType = VfxAnchorType.ShipHit;
        [Tooltip("Extra manual nudge applied after resolving the end anchor, in canvas pixels.")]
        [SerializeField] private Vector2 _endOffset;

        [Header("Approach")]
        [Tooltip("The Image (Filled type, Fill Method = Vertical, Fill Origin = Top) that fills and slides toward the target.")]
        [SerializeField] private Image _whaleImage;
        [Tooltip("Fill amount reached at the end of the approach (not a full 1, per design).")]
        [SerializeField] private float _fillTarget = 0.9f;
        [Tooltip("How long the fill animation takes, independent of how long the move takes.")]
        [SerializeField] private float _fillDuration = 0.5f;
        [SerializeField] private Ease  _fillEase = Ease.InQuad;
        [Tooltip("How long the move from Start to End takes, independent of the fill duration.")]
        [SerializeField] private float _moveDuration = 0.5f;
        [SerializeField] private Ease  _moveEase = Ease.InQuad;

        [Header("Contact")]
        [SerializeField] private float _shakeDuration  = 0.3f;
        [SerializeField] private float _shakeAmplitude = 2f;
        [SerializeField] private float _shakeFrequency = 35f;
        [Tooltip("Loud crash played on contact with the ship.")]
        [SerializeField] private CardSfxCue _contactSfx;
        [Tooltip("How long the whale sits eclipsing the ship after contact before the fade-out.")]
        [SerializeField] private float _eclipseHoldDuration = 0.2f;

        [Header("Fade")]
        [SerializeField] private float _fadeDuration = 0.35f;
        [SerializeField] private Ease  _fadeEase = Ease.InQuad;

        public event Action Completed;

        private RectTransform _rect;
        private Sequence      _sequence;

        private void Awake() => _rect = GetComponent<RectTransform>();

        public void Initialize(CardEffectSpawnContext context)
        {
            CardSfxPlayer.Play(_emergeSfx, transform.position);

            // Re-anchors to our own configurable Start Point, overriding wherever
            // CardPresentationPlayer originally placed this based on the CardSO's entry.
            Transform startAnchor = context.GetAnchor?.Invoke(_startSide, _startAnchorType);
            if (_rect != null && startAnchor != null && context.GameCamera != null)
                _rect.anchoredPosition = WorldToCanvasLocalPoint(startAnchor.position, context.GameCamera, context.GameCanvas) + _startOffset;

            _sequence = DOTween.Sequence();

            if (_whaleImage != null)
            {
                _whaleImage.fillAmount = 0f;
                _sequence.Append(_whaleImage.DOFillAmount(_fillTarget, _fillDuration).SetEase(_fillEase));
            }
            else
            {
                _sequence.AppendInterval(_fillDuration);
            }

            Transform endAnchor = context.GetAnchor?.Invoke(_endSide, _endAnchorType);
            if (_rect != null && endAnchor != null && context.GameCamera != null)
            {
                Vector2 endLocalPos = WorldToCanvasLocalPoint(endAnchor.position, context.GameCamera, context.GameCanvas) + _endOffset;
                _sequence.Join(_rect.DOAnchorPos(endLocalPos, _moveDuration).SetEase(_moveEase));
            }

            _sequence.AppendCallback(() => OnContact(context));
            _sequence.AppendInterval(_eclipseHoldDuration);

            if (_whaleImage != null)
                _sequence.Append(_whaleImage.DOFade(0f, _fadeDuration).SetEase(_fadeEase));
            else
                _sequence.AppendInterval(_fadeDuration);

            _sequence.OnComplete(() => Completed?.Invoke());
        }

        private void OnContact(CardEffectSpawnContext context)
        {
            MMCameraShakeEvent.Trigger(_shakeDuration, _shakeAmplitude, _shakeFrequency, 0f, 0f, 0f);
            CardSfxPlayer.Play(_contactSfx, transform.position);
        }

        private Vector2 WorldToCanvasLocalPoint(Vector3 worldPosition, Camera cam, RectTransform canvasRect)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
            // null camera is correct for Screen Space Overlay canvases (see CardPresentationPlayer's
            // own WorldToCanvasLocalPoint) — passing the real camera here collapses the result to
            // roughly the canvas corner regardless of the input world position.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var localPoint);
            return localPoint;
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
