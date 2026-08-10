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
    /// Vertical, Fill Origin = Top) fills from 0 up to FillTarget while simultaneously rising and
    /// sliding toward the opponent ship's hit point — one diagonal breach-and-lunge motion.
    /// Contact triggers a loud crash SFX and a strong camera shake; the whale then holds a beat
    /// before fading out and destroying itself. Whale Ram always hits the opponent (no target
    /// selection), so it uses CardEffectSpawnContext.GetOpponentAnchor rather than the
    /// explicit-target accessor Tidal Wave uses.
    /// Spawns already positioned by CardPresentationPlayer at the SeaSurface anchor (authored
    /// to sit to the left of both ships) via AnchorSide = Opponent.
    /// </summary>
    public class WhaleRamVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Emergence")]
        [Tooltip("Whale cry played the instant the effect spawns.")]
        [SerializeField] private CardSfxCue _emergeSfx;

        [Header("Approach")]
        [Tooltip("The Image (Filled type, Fill Method = Vertical, Fill Origin = Top) that fills and slides toward the target.")]
        [SerializeField] private Image _whaleImage;
        [Tooltip("Fill amount reached at the end of the approach (not a full 1, per design).")]
        [SerializeField] private float _fillTarget = 0.9f;
        [SerializeField] private float _travelDuration = 0.5f;
        [SerializeField] private Ease  _travelEase = Ease.InQuad;

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

            _sequence = DOTween.Sequence();

            if (_whaleImage != null)
            {
                _whaleImage.fillAmount = 0f;
                _sequence.Append(_whaleImage.DOFillAmount(_fillTarget, _travelDuration).SetEase(_travelEase));
            }
            else
            {
                _sequence.AppendInterval(_travelDuration);
            }

            Transform targetAnchor = context.GetOpponentAnchor?.Invoke(VfxAnchorType.ShipHit);
            if (_rect != null && targetAnchor != null && context.GameCamera != null)
            {
                Vector2 endLocalPos = WorldToCanvasLocalPoint(targetAnchor.position, context.GameCamera, context.GameCanvas);
                _sequence.Join(_rect.DOAnchorPos(endLocalPos, _travelDuration).SetEase(_travelEase));
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
