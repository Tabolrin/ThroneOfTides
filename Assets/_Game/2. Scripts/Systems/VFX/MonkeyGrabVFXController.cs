// Assets/_Game/2. Scripts/Systems/VFX/MonkeyGrabVFXController.cs
using System;
using DG.Tweening;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Monkey Grab's presentation: the monkey jumps from the stealing side's ship to the
    /// targeted (opponent) ship, vanishes for a moment (the "grab"), reappears on that same
    /// ship, then jumps back to its home ship.
    /// Sprite choice is purely direction-based, not caster-based: whichever leg moves toward the
    /// player's ship shows the front sprite (approaching the player's viewpoint), whichever leg
    /// moves toward the enemy's ship shows the back sprite (heading away from it). A
    /// player-cast steal's outbound leg therefore shows the back (heading to the enemy ship) and
    /// its return leg shows the front (heading home) — reversed for an enemy-cast steal.
    /// A pure world-space effect (no canvas conversion needed) — spawns already positioned by
    /// CardPresentationPlayer at the caster's ShipDeck anchor via AnchorSide = Caster.
    /// </summary>
    public class MonkeyGrabVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Sprites")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _frontSprite;
        [SerializeField] private Sprite _backSprite;

        [Header("Jump")]
        [SerializeField] private float _jumpDuration = 0.45f;
        [SerializeField] private float _jumpHeight = 0.6f;
        [SerializeField] private Ease _jumpEase = Ease.Linear;

        [Header("Vanish")]
        [Tooltip("How long the monkey stays hidden on the target ship — the beat where it 'grabs' the card.")]
        [SerializeField] private float _vanishHoldDuration = 0.3f;
        [SerializeField] private float _fadeDuration = 0.12f;

        public event Action Completed;

        private Sequence _sequence;

        public void Initialize(CardEffectSpawnContext context)
        {
            Vector3 originPosition = transform.position;
            Transform targetAnchor = context.GetOpponentAnchor?.Invoke(VfxAnchorType.ShipDeck);
            Vector3 targetPosition = targetAnchor != null ? targetAnchor.position : originPosition;

            // The outbound leg always heads to the opponent's ship — that's the player's ship
            // only when the enemy is the one casting.
            bool outboundTowardPlayer = context.Caster == CardCasterFilter.Enemy;
            SetSprite(outboundTowardPlayer);

            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOJump(targetPosition, _jumpHeight, 1, _jumpDuration).SetEase(_jumpEase));
            _sequence.Append(Fade(0f));
            _sequence.AppendInterval(_vanishHoldDuration);
            _sequence.AppendCallback(() => SetSprite(!outboundTowardPlayer));
            _sequence.Append(Fade(1f));
            _sequence.Append(transform.DOJump(originPosition, _jumpHeight, 1, _jumpDuration).SetEase(_jumpEase));
            _sequence.Append(Fade(0f));
            _sequence.OnComplete(() => Completed?.Invoke());
        }

        private void SetSprite(bool front)
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.sprite = front ? _frontSprite : _backSprite;
        }

        private Tween Fade(float alpha)
        {
            if (_spriteRenderer == null) return DOTween.Sequence();
            return _spriteRenderer.DOFade(alpha, _fadeDuration);
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
