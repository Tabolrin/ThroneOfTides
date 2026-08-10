// Assets/_Game/2. Scripts/Systems/VFX/TidalWaveVFXController.cs
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
    /// Tidal Wave's presentation: a Filled-from-bottom wave image fills and slides in from the
    /// sea-surface spawn point to the target ship's hit point at once, "eclipsing" the ship on
    /// arrival (a Screen Space Overlay canvas element always draws over world-space ship
    /// sprites, so no manual sort-order work is needed for that). Contact triggers a camera
    /// shake and SFX; the target's Gunpowder sprite is held at its pre-hit look for the whole
    /// approach and only released — snapping to normal if it was cleared — right before the
    /// fade-out starts, then the wave fades and destroys itself.
    /// Spawns already positioned by CardPresentationPlayer at the SeaSurface anchor (authored
    /// to sit to the left of both ships) via AnchorSide = ExplicitTarget.
    /// </summary>
    public class TidalWaveVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Wave")]
        [Tooltip("The Image (Filled type, Fill Origin = Bottom) that fills and slides toward the target.")]
        [SerializeField] private Image _waveImage;
        [SerializeField] private float _fillMoveDuration = 0.45f;
        [SerializeField] private Ease  _fillEase = Ease.InQuad;
        [SerializeField] private Ease  _moveEase = Ease.InQuad;

        [Header("Contact")]
        [SerializeField] private float _shakeDuration  = 0.25f;
        [SerializeField] private float _shakeAmplitude = 0.4f;
        [SerializeField] private float _shakeFrequency = 30f;
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
            // later — released at the "right before fade" beat further down.
            context.BeginExplicitTargetGunpowderHold?.Invoke();

            _sequence = DOTween.Sequence();

            if (_waveImage != null)
            {
                _waveImage.fillAmount = 0f;
                _sequence.Append(_waveImage.DOFillAmount(1f, _fillMoveDuration).SetEase(_fillEase));
            }
            else
            {
                _sequence.AppendInterval(_fillMoveDuration);
            }

            Transform targetAnchor = context.GetExplicitTargetAnchor?.Invoke(VfxAnchorType.ShipHit);
            if (_rect != null && targetAnchor != null && context.GameCamera != null)
            {
                Vector2 endLocalPos = WorldToCanvasLocalPoint(targetAnchor.position, context.GameCamera, context.GameCanvas);
                _sequence.Join(_rect.DOAnchorPos(endLocalPos, _fillMoveDuration).SetEase(_moveEase));
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
