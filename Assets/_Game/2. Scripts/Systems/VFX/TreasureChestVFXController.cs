// Assets/_Game/2. Scripts/Systems/VFX/TreasureChestVFXController.cs
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Treasure Chest: a UI-canvas chest image fills from empty to full (Filled/Vertical/Fill
    /// Origin = Top, same convention as Whale Ram/Kraken) while rising slightly, then quickly
    /// enlarges. The instant it reaches full size it fires a one-shot coin-burst particle system
    /// spawned right on top of the chest sprite itself, holds briefly, then shrinks back to its
    /// original size and fades out. Root prefab is a UI Image (RectTransform), positioned via
    /// anchoredPosition like the other UI-canvas card controllers.
    /// </summary>
    public class TreasureChestVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("References")]
        [SerializeField] private Image _chestImage;
        [Tooltip("Default coin-burst particle system — spawned right on top of the chest sprite and played the instant the chest reaches full size. Placeholder settings; tweak freely.")]
        [SerializeField] private ParticleSystem _coinParticlesPrefab;

        [Header("Fill")]
        [Tooltip("Seconds for the chest image to fill from empty to full.")]
        [SerializeField] private float _fillDuration = 1f;
        [SerializeField] private Ease  _fillEase = Ease.Linear;
        [Tooltip("How far (canvas units) the chest rises while filling.")]
        [SerializeField] private float _riseDistance = 20f;

        [Header("Enlarge (on full)")]
        [Tooltip("Scale multiplier applied once the chest is full.")]
        [SerializeField] private float _enlargeScale = 1.3f;
        [Tooltip("Seconds for the quick enlarge once full.")]
        [SerializeField] private float _enlargeDuration = 0.15f;
        [SerializeField] private Ease  _enlargeEase = Ease.OutBack;

        [Header("Payoff")]
        [Tooltip("Seconds the enlarged chest holds (coin burst plays here) before shrinking back.")]
        [SerializeField] private float _holdDuration = 0.4f;
        [SerializeField] private float _shrinkDuration = 0.25f;
        [SerializeField] private Ease  _shrinkEase = Ease.InQuad;
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private Ease  _fadeEase = Ease.OutQuad;

        public event Action Completed;

        private RectTransform _rect;
        private CanvasGroup   _canvasGroup;
        private Camera        _gameCamera;
        private Vector2       _basePosition;
        private Vector3       _baseScale;
        private ParticleSystem _coinParticlesInstance;
        private Sequence _sequence;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _baseScale = _rect.localScale;
        }

        public void Initialize(CardEffectSpawnContext context)
        {
            _gameCamera = context.GameCamera;
            _basePosition = _rect.anchoredPosition;

            if (_coinParticlesPrefab != null)
            {
                _coinParticlesInstance = Instantiate(_coinParticlesPrefab, WorldPointOnChest(), Quaternion.identity);
                _coinParticlesInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            BuildAndPlaySequence();
        }

        // The chest is a Screen Space - Overlay UI element — RectTransform.position for it is in
        // screen-pixel space, not the actual 3D world space the (world-space) coin particle
        // system lives in. Converting through the game camera, same pattern as the other
        // controllers' world/UI sync (e.g. GunpowderBarrel's dust trail), is what actually keeps
        // the burst pinned to the chest instead of wherever the raw RectTransform position
        // happens to fall in world units.
        private Vector3 WorldPointOnChest()
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, _rect.position);
            return _gameCamera.ScreenToWorldPoint(
                new Vector3(screenPoint.x, screenPoint.y, _gameCamera.nearClipPlane + 1f));
        }

        private void BuildAndPlaySequence()
        {
            _chestImage.fillAmount = 0f;
            _canvasGroup.alpha = 1f;
            _rect.localScale = _baseScale;

            _sequence = DOTween.Sequence();

            _sequence.Append(DOTween.To(() => _chestImage.fillAmount, x => _chestImage.fillAmount = x, 1f, _fillDuration)
                .SetEase(_fillEase));
            _sequence.Join(_rect.DOAnchorPosY(_basePosition.y + _riseDistance, _fillDuration).SetEase(_fillEase));

            _sequence.Append(_rect.DOScale(_baseScale * _enlargeScale, _enlargeDuration).SetEase(_enlargeEase));
            _sequence.AppendCallback(PlayCoinBurst);
            _sequence.AppendInterval(_holdDuration);
            _sequence.Append(_rect.DOScale(_baseScale, _shrinkDuration).SetEase(_shrinkEase));
            _sequence.Append(_canvasGroup.DOFade(0f, _fadeDuration).SetEase(_fadeEase));
            _sequence.OnComplete(OnSequenceComplete);
        }

        private void PlayCoinBurst()
        {
            if (_coinParticlesInstance == null) return;

            // The chest has risen and enlarged since spawn — re-snap to its current position so
            // the burst plays exactly on top of the sprite, not where it started.
            _coinParticlesInstance.transform.position = WorldPointOnChest();
            _coinParticlesInstance.Play();
        }

        private void OnSequenceComplete()
        {
            if (_coinParticlesInstance != null) Destroy(_coinParticlesInstance.gameObject, 3f);
            Completed?.Invoke();
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
