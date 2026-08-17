// Assets/_Game/2. Scripts/Systems/VFX/TidalWaveVFXController.cs
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.Systems.VFX;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Tidal Wave's presentation: a Filled-from-bottom wave image fills and slides from a
    /// configurable start point to a configurable end point at once, "eclipsing" the ship on
    /// arrival (a Screen Space Overlay canvas element always draws over world-space ship
    /// sprites, so no manual sort-order work is needed for that). Contact triggers a camera
    /// shake and SFX; the target's Gunpowder sprite is held at its pre-hit look for the whole
    /// approach and only released - snapping to normal if it was cleared - right before the
    /// fade-out starts, then the wave fades and destroys itself.
    /// Start/End Point below override wherever CardPresentationPlayer originally spawned this
    /// (per the CardSO's own PresentationEntry) - defaults match the original design (both ends
    /// on the explicitly chosen target ship: SeaSurface to its left, then its ShipHit).
    /// </summary>
    public class TidalWaveVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Travel - Start Point")]
        [SerializeField] private CardPresentationSide _startSide = CardPresentationSide.ExplicitTarget;
        [SerializeField] private VfxAnchorType _startAnchorType = VfxAnchorType.SeaSurfaceLeft;
        [Tooltip("Extra manual nudge applied after resolving the start anchor, in canvas pixels.")]
        [SerializeField] private Vector2 _startOffset;

        [Header("Travel - End Point")]
        [SerializeField] private CardPresentationSide _endSide = CardPresentationSide.ExplicitTarget;
        [SerializeField] private VfxAnchorType _endAnchorType = VfxAnchorType.ShipHit;
        [Tooltip("Extra manual nudge applied after resolving the end anchor, in canvas pixels.")]
        [SerializeField] private Vector2 _endOffset;

        [Header("Start")]
        [Tooltip("Played a short beat after the wave begins forming - runs independently of the fill/move animation below, so it doesn't delay either.")]
        [SerializeField] private CardSfxCue _startSfx;
        [SerializeField] private float _startSfxDelay = 0.1f;

        [Header("Wave")]
        [Tooltip("The Image (Filled type, Fill Origin = Bottom) that fills and slides toward the target.")]
        [SerializeField] private Image _waveImage;
        [Tooltip("Uniform vertical nudge (canvas pixels) applied to the wave graphic for its whole travel - both Start and End Point - separate from the per-endpoint Start/End Offset above, for quick whole-effect height tuning without having to keep both offsets in sync.")]
        [SerializeField] private float _verticalOffset = 0f;
        [Tooltip("How long the fill animation takes, independent of how long the move takes.")]
        [SerializeField] private float _fillDuration = 0.45f;
        [SerializeField] private Ease  _fillEase = Ease.InQuad;
        [Tooltip("How long the move from Start to End takes, independent of the fill duration.")]
        [SerializeField] private float _moveDuration = 0.45f;
        [SerializeField] private Ease  _moveEase = Ease.InQuad;

        [Header("Contact")]
        [SerializeField] private ScreenShakeLevel _shakeLevel = ScreenShakeLevel.Level3;
        [SerializeField] private CardSfxCue _contactSfx;
        [Tooltip("How long the wave sits eclipsing the ship after contact before the Gunpowder reveal and fade-out.")]
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
            // Freeze the target's Gunpowder look now, before CombatResolver clears it a moment
            // later - released at the "right before fade" beat further down. Tied to the card's
            // real explicit target regardless of Start/End Point above (those are purely visual).
            context.BeginExplicitTargetGunpowderHold?.Invoke();

            // Shakes the camera itself at the wave's own impact beat - the generic instant
            // on-damage shake would otherwise double up with it.
            context.SuppressNextDamageCameraShake?.Invoke();

            // Re-anchors to our own configurable Start Point, overriding wherever
            // CardPresentationPlayer originally placed this based on the CardSO's entry.
            Transform startAnchor = context.GetAnchor?.Invoke(_startSide, _startAnchorType);
            if (_rect != null && startAnchor != null && context.GameCamera != null)
                _rect.anchoredPosition = WorldToCanvasLocalPoint(startAnchor.position, context.GameCamera, context.GameCanvas)
                    + _startOffset + Vector2.up * _verticalOffset;

            _sequence = DOTween.Sequence();

            // Scheduled at an absolute offset into the sequence's own timeline rather than
            // Appended/Joined - fires independently alongside the fill/move animation below
            // without delaying (or being delayed by) either.
            _sequence.InsertCallback(_startSfxDelay, () => CardSfxPlayer.Play(_startSfx, transform.position));

            if (_waveImage != null)
            {
                _waveImage.fillAmount = 0f;
                _sequence.Append(_waveImage.DOFillAmount(1f, _fillDuration).SetEase(_fillEase));
            }
            else
            {
                _sequence.AppendInterval(_fillDuration);
            }

            Transform endAnchor = context.GetAnchor?.Invoke(_endSide, _endAnchorType);
            if (_rect != null && endAnchor != null && context.GameCamera != null)
            {
                Vector2 endLocalPos = WorldToCanvasLocalPoint(endAnchor.position, context.GameCamera, context.GameCanvas)
                    + _endOffset + Vector2.up * _verticalOffset;
                _sequence.Join(_rect.DOAnchorPos(endLocalPos, _moveDuration).SetEase(_moveEase));
            }

            _sequence.AppendCallback(() => OnContact(context));
            _sequence.AppendInterval(_eclipseHoldDuration);
            _sequence.AppendCallback(() => context.EndExplicitTargetGunpowderHold?.Invoke());

            if (_waveImage != null)
                _sequence.Append(_waveImage.DOFade(0f, _fadeDuration).SetEase(_fadeEase));
            else
                _sequence.AppendInterval(_fadeDuration);

            _sequence.OnComplete(() => Completed?.Invoke());
        }

        private void OnContact(CardEffectSpawnContext context)
        {
            ScreenShake.Trigger(_shakeLevel);
            CardSfxPlayer.Play(_contactSfx, transform.position);
        }

        private Vector2 WorldToCanvasLocalPoint(Vector3 worldPosition, Camera cam, RectTransform canvasRect)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
            // null camera is correct for Screen Space Overlay canvases (see CardPresentationPlayer's
            // own WorldToCanvasLocalPoint) - passing the real camera here collapses the result to
            // roughly the canvas corner regardless of the input world position.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var localPoint);
            return localPoint;
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
