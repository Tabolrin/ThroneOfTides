// Assets/_Game/2. Scripts/Systems/VFX/TidalWaveVFXController.cs
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Tidal Wave's presentation: a fill-image tween from 0% to 100%, then a water-spray
    /// particle system on the target ship. Fires its own two SFX cues (tween-start and splash)
    /// directly instead of the generic entry-level Sfx, since it needs two independent beats —
    /// leave the authoring entry's own Sfx field unset for this card. Spawns already positioned
    /// by CardPresentationPlayer at the explicitly chosen target ship's anchor (left of the ship
    /// is an author-placed VFXSpawnPosition marker, not something this script computes).
    /// Implements ICardPlayEffect so CardPresentationPlayer hosts it without knowing any of this.
    /// </summary>
    public class TidalWaveVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Fill")]
        [Tooltip("The Image (Filled type) tweened from 0 to 1 as the wave builds.")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private float _fillDuration = 1f;
        [SerializeField] private Ease _fillEase = Ease.InOutSine;

        [Header("Splash")]
        [Tooltip("Water-spray particle system, started once the fill reaches 100%.")]
        [SerializeField] private ParticleSystem _splashParticles;

        [Header("SFX")]
        [Tooltip("Played the moment the fill tween starts.")]
        [SerializeField] private CardSfxCue _tweenStartSfx;
        [Tooltip("Played together with the splash particle system.")]
        [SerializeField] private CardSfxCue _splashSfx;

        public event Action Completed;

        private Sequence _sequence;

        public void Initialize(CardEffectSpawnContext context)
        {
            BuildAndPlaySequence();
        }

        private void BuildAndPlaySequence()
        {
            if (_fillImage != null) _fillImage.fillAmount = 0f;

            _sequence = DOTween.Sequence();
            _sequence.AppendCallback(OnTweenStart);

            if (_fillImage != null)
                _sequence.Append(_fillImage.DOFillAmount(1f, _fillDuration).SetEase(_fillEase));
            else
                _sequence.AppendInterval(_fillDuration);

            _sequence.AppendCallback(OnFillComplete);
            _sequence.OnComplete(() => Completed?.Invoke());
        }

        private void OnTweenStart() => CardSfxPlayer.Play(_tweenStartSfx, transform.position);

        private void OnFillComplete()
        {
            _splashParticles?.Play();
            CardSfxPlayer.Play(_splashSfx, transform.position);
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
