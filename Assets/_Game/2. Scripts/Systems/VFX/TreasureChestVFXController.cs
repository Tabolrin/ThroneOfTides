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
    /// spawned fresh between the two ships, holds briefly, then shrinks back to its original size
    /// and fades out. Root prefab is a UI Image (RectTransform), positioned via anchoredPosition
    /// like the other UI-canvas card controllers.
    /// </summary>
    public class TreasureChestVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("References")]
        [SerializeField] private Image _chestImage;
        [Tooltip("Default coin-burst particle system — spawned fresh between the two ships and played the instant the chest reaches full size. Placeholder settings; tweak freely.")]
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
            _basePosition = _rect.anchoredPosition;

            if (_coinParticlesPrefab != null)
            {
                Vector3 midpoint = Vector3.Lerp(context.CasterAnchor.position, context.OpponentAnchor.position, 0.5f);
                _coinParticlesInstance = Instantiate(_coinParticlesPrefab, midpoint, Quaternion.identity);
                _coinParticlesInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            BuildAndPlaySequence();
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

        private void PlayCoinBurst() => _coinParticlesInstance?.Play();

        private void OnSequenceComplete()
        {
            if (_coinParticlesInstance != null) Destroy(_coinParticlesInstance.gameObject, 3f);
            Completed?.Invoke();
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
