// Assets/_Game/2. Scripts/Systems/VFX/ReconParrotVFXController.cs
using System;
using DG.Tweening;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Recon Parrot's presentation: the parrot (flapping in a continuous animation loop the
    /// whole time, driven by its own Animator) launches from the activating player's ship,
    /// flies to the targeted ship's sky anchor, flips horizontally to turn back around, flies
    /// back to the activating player's ship, then fades out and destroys itself.
    /// A pure world-space effect (no canvas conversion needed) — spawns already positioned by
    /// CardPresentationPlayer at the caster's ShipDeck anchor via AnchorSide = Caster.
    /// </summary>
    public class ReconParrotVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Flight")]
        [SerializeField] private float _outboundDuration = 0.6f;
        [SerializeField] private Ease  _outboundEase = Ease.InOutSine;
        [SerializeField] private float _holdAtTargetDuration = 0.3f;
        [SerializeField] private float _returnDuration = 0.6f;
        [SerializeField] private Ease  _returnEase = Ease.InOutSine;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Fade")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private Ease  _fadeEase = Ease.InQuad;

        public event Action Completed;

        private Sequence _sequence;

        public void Initialize(CardEffectSpawnContext context)
        {
            Vector3 spawnPosition = transform.position;
            Transform targetAnchor = context.GetOpponentAnchor?.Invoke(VfxAnchorType.Sky);
            Vector3 targetPosition = targetAnchor != null ? targetAnchor.position : spawnPosition;

            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOMove(targetPosition, _outboundDuration).SetEase(_outboundEase));
            _sequence.AppendCallback(FlipHorizontal);
            _sequence.AppendInterval(_holdAtTargetDuration);
            _sequence.Append(transform.DOMove(spawnPosition, _returnDuration).SetEase(_returnEase));

            if (_spriteRenderer != null)
                _sequence.Append(_spriteRenderer.DOFade(0f, _fadeDuration).SetEase(_fadeEase));
            else
                _sequence.AppendInterval(_fadeDuration);

            _sequence.OnComplete(() => Completed?.Invoke());
        }

        private void FlipHorizontal()
        {
            if (_spriteRenderer != null) _spriteRenderer.flipX = !_spriteRenderer.flipX;
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
