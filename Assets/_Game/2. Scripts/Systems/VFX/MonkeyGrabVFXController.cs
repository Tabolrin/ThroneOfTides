// Assets/_Game/2. Scripts/Systems/VFX/MonkeyGrabVFXController.cs
using System;
using DG.Tweening;
using UnityEngine;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

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
    /// its return leg shows the front (heading home) - reversed for an enemy-cast steal.
    /// A pure world-space effect (no canvas conversion needed) for the monkey itself - spawns
    /// already positioned by CardPresentationPlayer at the caster's ShipDeck anchor via
    /// AnchorSide = Caster.
    ///
    /// The actual GameState card-ownership change already happened synchronously the instant the
    /// card resolved (Execute() has no async capability), but the persistent hand-card visual is
    /// deliberately withheld until this controller says so: CardEffectContext.StealFromEnemyHand
    /// fires GameEventBus.OnCardStolen instead of touching HandLayoutManager directly, this
    /// controller subscribes to learn which card it's carrying, and only once the monkey is back
    /// on its home ship does a temporary enlarged card visual appear, fly to the receiving side's
    /// hand area, and finally call context.FinalizeStolenCardVisual to pop the real card into the
    /// hand.
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
        [Tooltip("How long the monkey stays hidden on the target ship - the beat where it 'grabs' the card.")]
        [SerializeField] private float _vanishHoldDuration = 0.3f;
        [SerializeField] private float _fadeDuration = 0.12f;

        [Header("Stolen Card Visual")]
        [Tooltip("Scale the card preview grows to on arrival, before flying to the hand.")]
        [SerializeField] private float _cardEnlargedScale = 2.2f;
        [SerializeField] private float _cardAppearDuration = 0.25f;
        [Tooltip("How long the enlarged card sits still on the ship before it starts flying to the hand.")]
        [SerializeField] private float _cardHoldDuration = 0.35f;
        [Tooltip("Canvas units per second the card travels from the ship to the hand area - travel time is the on-screen distance divided by this, so it takes proportionally longer from farther away instead of a fixed duration.")]
        [SerializeField] private float _cardFlightSpeed = 1200f;
        [SerializeField] private Ease _cardAppearEase = Ease.OutBack;
        [SerializeField] private Ease _cardFlightEase = Ease.InQuad;

        public event Action Completed;

        private Sequence _sequence;
        private Sequence _cardSequence;
        private ICard _stolenCard;
        private DamageTarget _receivingSide;

        public void Initialize(CardEffectSpawnContext context)
        {
            Vector3 originPosition = transform.position;
            Transform targetAnchor = context.GetOpponentAnchor?.Invoke(VfxAnchorType.ShipDeck);
            Vector3 targetPosition = targetAnchor != null ? targetAnchor.position : originPosition;

            // The outbound leg always heads to the opponent's ship - that's the player's ship
            // only when the enemy is the one casting.
            bool outboundTowardPlayer = context.Caster == CardCasterFilter.Enemy;
            SetSprite(outboundTowardPlayer);

            GameEventBus.OnCardStolen += CacheStolenCard;

            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOJump(targetPosition, _jumpHeight, 1, _jumpDuration).SetEase(_jumpEase));
            _sequence.Append(Fade(0f));
            _sequence.AppendCallback(() => context.PlaySfx?.Invoke(targetPosition));
            _sequence.AppendInterval(_vanishHoldDuration);
            _sequence.AppendCallback(() => SetSprite(!outboundTowardPlayer));
            _sequence.Append(Fade(1f));
            _sequence.Append(transform.DOJump(originPosition, _jumpHeight, 1, _jumpDuration).SetEase(_jumpEase));
            _sequence.AppendCallback(() => PlayStolenCardVisual(context, originPosition));
            _sequence.Append(Fade(0f));
            _sequence.OnComplete(() => Completed?.Invoke());
        }

        // Learns which card was stolen and who received it the instant CombatResolver actually
        // executes the effect - which happens moments after Initialize (Initialize runs off
        // FireCardPlayAccepted, itself fired before the card's effect resolves), so there's no
        // way to know this any earlier.
        private void CacheStolenCard(ICard card, DamageTarget gainedBy)
        {
            _stolenCard = card;
            _receivingSide = gainedBy;
            GameEventBus.OnCardStolen -= CacheStolenCard;
        }

        // No-op if the steal never actually happened (e.g. the victim's hand was empty) - the
        // monkey just fades out and completes normally with nothing to show.
        private void PlayStolenCardVisual(CardEffectSpawnContext context, Vector3 originPosition)
        {
            if (_stolenCard == null) return;
            if (!(_stolenCard is CardSO cardSO)) return;
            if (context.GameCanvas == null || context.GetHandAreaPosition == null) return;
            if (context.SpawnStolenCardVisual == null) return;

            // The real card prefab (frame/cost/art/text), not a bare art sprite - see
            // HandLayoutManager.SpawnStolenCardPreview.
            GameObject go = context.SpawnStolenCardVisual.Invoke(_stolenCard, context.GameCanvas);
            if (go == null) return;

            var rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                Destroy(go);
                return;
            }

            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = go.AddComponent<CanvasGroup>();

            rect.anchoredPosition = WorldToCanvasLocalPoint(context, originPosition);
            rect.localScale = Vector3.zero;

            // GetHandAreaPosition comes from a RectTransform on the same Screen Space - Overlay
            // canvas, so its .position is already screen-pixel space - unlike originPosition
            // above (a real 3D world-space ship anchor), it must NOT be re-projected through the
            // camera, or it lands somewhere wildly wrong on screen.
            Vector2 handPoint = ScreenSpaceUIToCanvasLocalPoint(context, context.GetHandAreaPosition(_receivingSide));

            float flightDistance = Vector2.Distance(rect.anchoredPosition, handPoint);
            float flightDuration = Mathf.Max(0.05f, flightDistance / Mathf.Max(1f, _cardFlightSpeed));

            // A fresh, independent object/sequence - deliberately NOT killed by this controller's
            // own OnDestroy (see below), since the monkey's own fade-and-destroy finishes well
            // before this flight does and must not cut it off mid-flight.
            _cardSequence = DOTween.Sequence();
            _cardSequence.Append(rect.DOScale(_cardEnlargedScale, _cardAppearDuration).SetEase(_cardAppearEase));
            _cardSequence.AppendInterval(_cardHoldDuration);
            _cardSequence.Append(rect.DOAnchorPos(handPoint, flightDuration).SetEase(_cardFlightEase));
            _cardSequence.Join(rect.DOScale(0f, flightDuration).SetEase(_cardFlightEase));
            _cardSequence.Join(canvasGroup.DOFade(0f, flightDuration).SetEase(_cardFlightEase));
            _cardSequence.OnComplete(() =>
            {
                Destroy(go);
                context.FinalizeStolenCardVisual?.Invoke(_stolenCard, _receivingSide);
            });
        }

        // For real 3D world-space positions (e.g. a ship's VFX anchor Transform) - projects
        // through the camera first, then into the canvas's local space.
        private Vector2 WorldToCanvasLocalPoint(CardEffectSpawnContext context, Vector3 worldPosition)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(context.GameCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                context.GameCanvas, screenPoint, null, out var localPoint);
            return localPoint;
        }

        // For a position that's already another UI element's .position on this same Screen
        // Space - Overlay canvas (already screen-pixel space) - skips the camera projection
        // WorldToCanvasLocalPoint does, which would otherwise scramble an already-screen-space
        // point into a bogus location.
        private Vector2 ScreenSpaceUIToCanvasLocalPoint(CardEffectSpawnContext context, Vector3 screenSpacePosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                context.GameCanvas, screenSpacePosition, null, out var localPoint);
            return localPoint;
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

        private void OnDestroy()
        {
            GameEventBus.OnCardStolen -= CacheStolenCard;
            _sequence?.Kill();
            // _cardSequence is deliberately NOT killed here - it animates a separate, independent
            // GameObject (the stolen-card preview) that outlives this monkey sprite. The monkey
            // fades out and gets destroyed by CardPresentationPlayer well before the card's own
            // flight-to-hand finishes; killing it here would freeze that preview mid-flight
            // forever, since its own OnComplete (which destroys it and adds the real hand card)
            // would never run.
        }
    }
}
