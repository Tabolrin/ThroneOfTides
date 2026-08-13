// Assets/_Game/2. Scripts/Systems/VFX/ReconParrotVFXController.cs
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Recon Parrot's presentation: the parrot (flapping in a continuous animation loop the
    /// whole time, driven by its own Animator) launches from the activating player's ship and
    /// flies to the targeted ship's sky anchor, then the reveal panel shows the enemy's hand.
    /// Only once the player dismisses that panel does the parrot flip around, fly back to the
    /// activating player's ship, then fade out and destroy itself — sequenced this way (rather
    /// than firing the reveal panel immediately, as ReconParrotEffectSO's own
    /// GameEventBus.FireEnemyHandRevealed would otherwise do on its own) so the panel doesn't pop
    /// up over the parrot before it has even finished flying in.
    /// A pure world-space effect (no canvas conversion needed) — spawns already positioned by
    /// CardPresentationPlayer at the caster's ShipDeck anchor via AnchorSide = Caster.
    /// </summary>
    public class ReconParrotVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Flight")]
        [SerializeField] private float _outboundDuration = 0.6f;
        [SerializeField] private Ease  _outboundEase = Ease.InOutSine;
        [SerializeField] private float _returnDuration = 0.6f;
        [SerializeField] private Ease  _returnEase = Ease.InOutSine;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Fade")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private Ease  _fadeEase = Ease.InQuad;

        public event Action Completed;

        private CardEffectSpawnContext _context;
        private Vector3 _spawnPosition;
        private IReadOnlyList<ICard> _revealedCards;
        private Sequence _sequence;

        public void Initialize(CardEffectSpawnContext context)
        {
            _context = context;
            _spawnPosition = transform.position;

            // ReconParrotEffectSO fires this synchronously as part of the same card-resolution
            // call that spawned this VFX — subscribing before the outbound flight even starts
            // guarantees it's already been raised by the time we'd otherwise miss it.
            GameEventBus.OnEnemyHandRevealed += CacheRevealedHand;

            PlayOutbound();
        }

        private void CacheRevealedHand(IReadOnlyList<ICard> cards) => _revealedCards = cards;

        private void PlayOutbound()
        {
            Transform targetAnchor = _context.GetOpponentAnchor?.Invoke(VfxAnchorType.Sky);
            Vector3 targetPosition = targetAnchor != null ? targetAnchor.position : _spawnPosition;

            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOMove(targetPosition, _outboundDuration).SetEase(_outboundEase));
            _sequence.OnComplete(ShowRevealPanel);
        }

        private void ShowRevealPanel()
        {
            GameEventBus.OnEnemyHandRevealed -= CacheRevealedHand;

            if (_context.ShowEnemyHandReveal != null && _revealedCards != null)
                _context.ShowEnemyHandReveal(_revealedCards, PlayReturn);
            else
                PlayReturn(); // no panel wired / nothing revealed — just fly home
        }

        private void PlayReturn()
        {
            _sequence = DOTween.Sequence();
            _sequence.AppendCallback(FlipHorizontal);
            _sequence.Append(transform.DOMove(_spawnPosition, _returnDuration).SetEase(_returnEase));

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

        private void OnDestroy()
        {
            GameEventBus.OnEnemyHandRevealed -= CacheRevealedHand;
            _sequence?.Kill();
        }
    }
}
