// Assets/_Game/2. Scripts/UI/HandLayoutManager.cs
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace ThroneOfTides.UI
{
    public class HandLayoutManager : MonoBehaviour, IHandLayoutManager
    {
        [SerializeField] private RectTransform _playerHandContainer;
        [SerializeField] private RectTransform _enemyHandContainer;
        [SerializeField] private CardView      _cardPrefab;
        [SerializeField] private Canvas        _dragCanvas;
        [SerializeField] private CardInspectController _inspectController;

        [Header("Player Hand Layout")]
        [SerializeField] private float _cardSpacing = -18f;
        [SerializeField] private float _maxYOffset  = 15f;

        [Header("Enemy Hand Layout")]
        [SerializeField] private float _enemyCardSpacing = -12f;

        [Header("Enemy Card Play")]
        [SerializeField] private float _cardMoveDuration    = 0.4f;
        [SerializeField] private float _cardFadeDuration    = 0.2f;
        [Tooltip("Fallback hold duration used only if GameplaySettings is unassigned.")]
        [SerializeField] private float _cardDisplayDuration = 1.5f;
        [SerializeField] private float _enlargedCardScale = 1.75f;
        [Tooltip("Whether the enemy's played-card reveal waits for a click or auto-dismisses — see OptionsPanel's Gameplay section.")]
        [SerializeField] private ThroneOfTides.Data.GameplaySettingsSO _gameplaySettings;
        [Tooltip("Shown only while RequireClickToDismissEnemyCard is on — click it (or the card) to continue.")]
        [SerializeField] private UnityEngine.UI.Button _enemyCardDismissButton;


        [Header("Opening Deal")]
        [SerializeField] private float _openingDealInterval     = 0.28f;
        [SerializeField] private float _openingSlideDistanceMin = 60f;
        [SerializeField] private float _openingSlideDistanceMax = 120f;
        [SerializeField] private float _openingSlideDuration    = 0.22f;

        [Header("Hand Animation")]
        [SerializeField] private float _gapCloseDuration = 0.12f;

        [Header("Hand Hover")]
        [Tooltip("Bigger/further than EnemyHandRevealPanel's hover so a card popping out of the fanned hand reads clearly above its overlapping neighbors.")]
        [SerializeField] private float _handHoverScale = 1.35f;
        [SerializeField] private float _handHoverRise  = 80f;

        [Header("Manual Draw Animation")]
        [SerializeField] private RectTransform _deckTransform;
        [SerializeField] private float         _drawArcHeight   = 120f;
        [SerializeField] private float         _drawArcDuration = 0.35f;
        [SerializeField] private MMF_Player    _feedbackDeckDraw;

        [Header("Reaction Charge Absorb")]
        [Tooltip("Where a reaction card (Dead Man's Turn, Counter Gale) flies to and shrinks away once the draw phase is done — the player's reaction badge row on ActiveEffectsBar.")]
        [SerializeField] private RectTransform _reactionBadgeAnchor;
        [SerializeField] private float         _reactionAbsorbDuration = 0.35f;

        private readonly List<CardView> _playerCards = new List<CardView>();
        private readonly List<CardView> _enemyCards  = new List<CardView>();

        private ObjectPool<CardView> _cardViewPool;

        private void Awake()
        {
            // One-time fallback if the scene/prefab hasn't had this new field wired up in the
            // Inspector yet — avoids silently breaking card inspect after this refactor.
            if (_inspectController == null)
                _inspectController = FindFirstObjectByType<CardInspectController>();

            // Pools card view instances instead of Instantiate/Destroy per draw/play — hands
            // churn cards constantly (every draw, every enemy play, every discard).
            _cardViewPool = new ObjectPool<CardView>(
                createFunc: () => Instantiate(_cardPrefab),
                actionOnGet: view =>
                {
                    view.gameObject.SetActive(true);
                    view.transform.localScale = Vector3.one;
                    var canvasGroup = view.GetComponent<CanvasGroup>();
                    if (canvasGroup != null) canvasGroup.alpha = 1f;
                },
                actionOnRelease: view =>
                {
                    // Destroy() used to clean up in-flight DOTween tweens automatically —
                    // pooled objects only get deactivated, so kill tweens explicitly.
                    view.transform.DOKill();
                    var canvasGroup = view.GetComponent<CanvasGroup>();
                    if (canvasGroup != null) canvasGroup.DOKill();
                    DisableHover(view);

                    view.gameObject.SetActive(false);
                    view.transform.SetParent(transform, false);
                },
                actionOnDestroy: view => Destroy(view.gameObject),
                collectionCheck: false);
        }

        private CardView SpawnCardView(Transform parent)
        {
            CardView view = _cardViewPool.Get();
            view.transform.SetParent(parent, false);
            view.OnInspectRequested = ShowCardInspect;

            // Only the player's own hand grows/lifts/fronts on hover — the enemy's hand is
            // face-down and not meant to invite interaction.
            if (parent == _playerHandContainer) EnableHover(view);
            else                                DisableHover(view);

            return view;
        }

        // Pooled views can move between the player's hand and the enemy's hand (or the reveal
        // panel briefly borrows one) across their lifetime — hover must be explicitly set every
        // time a view is (re)placed rather than assumed from whatever it had before.
        private void EnableHover(CardView view)
        {
            var hover = view.GetComponent<CardHoverEffect>();
            if (hover == null) hover = view.gameObject.AddComponent<CardHoverEffect>();
            hover.Configure(_handHoverScale, _handHoverRise, bringToFront: true);
            hover.enabled = true;
        }

        private void DisableHover(CardView view)
        {
            var hover = view.GetComponent<CardHoverEffect>();
            if (hover != null) hover.enabled = false;
        }

        private void ShowCardInspect(CardView card)
        {
            if (_inspectController != null) _inspectController.Show(card);
        }

        private void ReleaseCardView(CardView view)
        {
            if (view == null) return;
            _cardViewPool.Release(view);
        }

        // ── IHandLayoutManager ──────────────────────────────────────────────

        void IHandLayoutManager.AddCardToPlayerHand(ICard card) =>
            AddCardToPlayerHand(card as CardSO);

        void IHandLayoutManager.RemoveCardFromPlayerHand(ICard card) =>
            RemoveCardFromPlayerHand(card as CardSO);

        void IHandLayoutManager.StealCardFromEnemyHand(ICard card) =>
            StealCardFromEnemyHand(card as CardSO);

        void IHandLayoutManager.StealCardFromPlayerHand(ICard card) =>
            StealCardFromPlayerHand(card as CardSO);

        void IHandLayoutManager.AddCardToEnemyHand(ICard card) =>
            AddCardToEnemyHand(card as CardSO);

        // ICard parameter to satisfy interface — cast to CardSO internally
        IEnumerator IHandLayoutManager.AnimateManualDraw(ICard card) =>
            AnimateManualDraw(card as CardSO);

        // ── Player Hand ─────────────────────────────────────────────────────

        public void AddCardToPlayerHand(CardSO card)
        {
            CardView view = SpawnCardView(_playerHandContainer);
            view.Setup(card);
            view.HandYOffset = Random.Range(-_maxYOffset, _maxYOffset);

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
            {
                drag.SetDragCanvas(_dragCanvas);
                drag.OnDragStarted += OnCardDragStarted;
                drag.OnDragEnded   += OnCardDragEnded;
            }

            _playerCards.Add(view);
            RefreshPlayerLayout(animated: false);
        }

        private void OnCardDragStarted(CardView card)
        {
            _playerCards.RemoveAll(v => v == null);
            RefreshPlayerLayout(animated: true);
        }

        private void OnCardDragEnded(CardView card)
        {
            if (card == null) return;

            if (card.WasPlayed)
            {
                _playerCards.Remove(card);
                var drag = card.GetComponent<CardDragHandler>();
                if (drag != null)
                {
                    drag.OnDragStarted -= OnCardDragStarted;
                    drag.OnDragEnded   -= OnCardDragEnded;
                }
                ReleaseCardView(card);
                RefreshPlayerLayout(animated: true);
                return;
            }

            if (!_playerCards.Contains(card))
            {
                card.HandYOffset = Random.Range(-_maxYOffset, _maxYOffset);
                _playerCards.Add(card);
            }
            RefreshPlayerLayout(animated: true);
        }

        public void RemoveCardFromPlayerHand(CardSO card)
        {
            CardView view = _playerCards.Find(v => v != null && v.CardData == card);
            if (view == null)
            {
                _playerCards.RemoveAll(v => v == null);
                RefreshPlayerLayout(animated: true);
                return;
            }

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
            {
                drag.OnDragStarted -= OnCardDragStarted;
                drag.OnDragEnded   -= OnCardDragEnded;
            }

            _playerCards.Remove(view);
            ReleaseCardView(view);
            RefreshPlayerLayout(animated: true);
        }

        public void ClearPlayerHand()
        {
            foreach (var c in _playerCards)
            {
                if (c == null) continue;
                var drag = c.GetComponent<CardDragHandler>();
                if (drag != null)
                {
                    drag.OnDragStarted -= OnCardDragStarted;
                    drag.OnDragEnded   -= OnCardDragEnded;
                }
                ReleaseCardView(c);
            }
            _playerCards.Clear();
        }

        private void RefreshPlayerLayout(bool animated)
        {
            _playerCards.RemoveAll(v => v == null);
            if (_playerCards.Count == 0) return;

            var inHand = _playerCards.FindAll(
                v => v.transform.parent == _playerHandContainer);

            if (inHand.Count == 0) return;

            float totalWidth = (inHand.Count - 1) * _cardSpacing;
            float startX     = -totalWidth / 2f;

            for (int i = 0; i < inHand.Count; i++)
            {
                var rect      = inHand[i].GetComponent<RectTransform>();
                float targetX = startX + i * _cardSpacing;
                float targetY = inHand[i].HandYOffset;

                if (animated)
                    rect.DOAnchorPos(new Vector2(targetX, targetY), _gapCloseDuration)
                        .SetEase(Ease.OutCubic);
                else
                    rect.anchoredPosition = new Vector2(targetX, targetY);

                inHand[i].transform.SetSiblingIndex(i);
            }
        }

        public IEnumerator DealOpeningHandAnimated(List<CardSO> cards)
        {
            foreach (var card in cards)
            {
                CardView view = SpawnCardView(_playerHandContainer);
                view.Setup(card);
                view.HandYOffset = Random.Range(-_maxYOffset, _maxYOffset);

                var drag = view.GetComponent<CardDragHandler>();
                if (drag != null)
                {
                    drag.SetDragCanvas(_dragCanvas);
                    drag.OnDragStarted += OnCardDragStarted;
                    drag.OnDragEnded   += OnCardDragEnded;
                }

                _playerCards.Add(view);
                RefreshPlayerLayout(animated: false);

                var rect = view.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector2 targetPos     = rect.anchoredPosition;
                    float slideDistance   = Random.Range(_openingSlideDistanceMin, _openingSlideDistanceMax);
                    rect.anchoredPosition = new Vector2(targetPos.x, targetPos.y - slideDistance);

                    rect.DOAnchorPos(targetPos, _openingSlideDuration)
                        .SetEase(Ease.OutQuart);

                    GameEventBus.FireCardDrawn(card);
                }

                yield return new WaitForSeconds(_openingDealInterval);
            }
        }

        // Spawns a card at the deck position and arcs it into the hand.
        // Called via interface from TurnCoordinator after a successful manual draw.
        private IEnumerator AnimateManualDraw(CardSO card)
{
    if (card == null) yield break;

    _feedbackDeckDraw?.PlayFeedbacks(_deckTransform != null
        ? _deckTransform.position
        : Vector3.zero);

    // Spawn directly in playerHandContainer to get correct native size
    CardView view = SpawnCardView(_playerHandContainer);
    view.Setup(card);
    view.HandYOffset = Random.Range(-_maxYOffset, _maxYOffset);

    var drag = view.GetComponent<CardDragHandler>();
    if (drag != null)
    {
        drag.SetDragCanvas(_dragCanvas);
        drag.OnDragStarted += OnCardDragStarted;
        drag.OnDragEnded   += OnCardDragEnded;
    }

    var rect = view.GetComponent<RectTransform>();
    var canvasGroup = view.GetComponent<CanvasGroup>();

    // Add to list and calculate final slot position via layout
    _playerCards.Add(view);
    RefreshPlayerLayout(animated: false);
    Vector2 targetPos = rect.anchoredPosition;

    // Convert deck world position to local position inside playerHandContainer
    // This ensures the arc starts exactly at the deck visual regardless of canvas nesting
    Vector2 deckLocalPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        _playerHandContainer,
        RectTransformUtility.WorldToScreenPoint(null, _deckTransform.position),
        null,
        out deckLocalPos);

    // Teleport card to deck position to begin arc — size stays correct since parent unchanged
    rect.anchoredPosition = deckLocalPos;

    float midX  = (deckLocalPos.x + targetPos.x) / 2f;
    float peakY = Mathf.Max(deckLocalPos.y, targetPos.y) + _drawArcHeight;

    // Not raycast-interactive while flying to its slot — otherwise the cursor merely sitting
    // anywhere along the arc triggers CardHoverEffect mid-flight, whose enlarge/rise tween then
    // fights this arc tween over the same RectTransform and corrupts the final hand layout.
    if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

    var sequence = DOTween.Sequence();
    sequence.Append(
        rect.DOAnchorPos(new Vector2(midX, peakY), _drawArcDuration * 0.5f)
            .SetEase(Ease.OutQuad));
    sequence.Append(
        rect.DOAnchorPos(targetPos, _drawArcDuration * 0.5f)
            .SetEase(Ease.InQuad));

    yield return sequence.WaitForCompletion();

    if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

    // Already in playerHandContainer — just settle remaining cards
    RefreshPlayerLayout(animated: true);
}

        // ── Enemy Hand ──────────────────────────────────────────────────────

        public void AddCardToEnemyHand(CardSO card)
        {
            CardView view = SpawnCardView(_enemyHandContainer);
            view.SetFaceDown(card);
            _enemyCards.Add(view);
            RefreshEnemyLayout();
        }

        public void RemoveCardFromEnemyHand(CardSO card)
        {
            CardView view = _enemyCards.Find(v => v != null && v.CardData == card);
            if (view == null) return;
            _enemyCards.Remove(view);
            ReleaseCardView(view);
            RefreshEnemyLayout();
        }

        public void RevealEnemyCard(CardSO card)
        {
            CardView view = _enemyCards.Find(v => v != null && v.CardData == card);
            if (view == null) return;
            view.Setup(card);
        }

        public void StealCardFromEnemyHand(CardSO card)
        {
            CardView view = _enemyCards.Find(v => v != null && v.CardData == card);
            if (view == null) return;

            _enemyCards.Remove(view);
            RefreshEnemyLayout(animated: true);

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
            {
                drag.SetDragCanvas(_dragCanvas);
                drag.OnDragStarted += OnCardDragStarted;
                drag.OnDragEnded   += OnCardDragEnded;
            }

            // Reparenting with worldPositionStays keeps the card exactly where it visually was
            // (still inside the enemy hand's on-screen area) — the animated RefreshPlayerLayout
            // right after is what makes it read as "flying" from the enemy's hand into the
            // player's, rather than teleporting straight into its final fanned slot.
            view.transform.SetParent(_playerHandContainer, true);
            // PlayerHandContainer and EnemyHandContainer apply different local scales (the
            // player's own hand renders larger) — worldPositionStays preserves the card's old
            // *world* scale, which lands on a non-1 local scale here that renders smaller than
            // its new sibling cards. Reset explicitly, matching what pool Get() does for a
            // normal spawn.
            view.transform.localScale = Vector3.one;
            view.Setup(card);
            view.HandYOffset = Random.Range(-_maxYOffset, _maxYOffset);
            EnableHover(view);

            _playerCards.Add(view);
            RefreshPlayerLayout(animated: true);
        }

        // Mirror of StealCardFromEnemyHand — reuses the player's existing CardView (flipped
        // face-down) instead of destroying it and spawning a fresh enemy-hand card, so Monkey
        // Grab reads as the same card flying away rather than the player's card vanishing and
        // an unrelated card appearing in the enemy's hand.
        public void StealCardFromPlayerHand(CardSO card)
        {
            CardView view = _playerCards.Find(v => v != null && v.CardData == card);
            if (view == null) return;

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
            {
                drag.OnDragStarted -= OnCardDragStarted;
                drag.OnDragEnded   -= OnCardDragEnded;
            }

            _playerCards.Remove(view);
            DisableHover(view);
            RefreshPlayerLayout(animated: true);

            view.transform.SetParent(_enemyHandContainer, true);
            // See the matching reset in StealCardFromEnemyHand — worldPositionStays otherwise
            // carries over the player hand's larger local scale.
            view.transform.localScale = Vector3.one;
            view.SetFaceDown(card);

            _enemyCards.Add(view);
            RefreshEnemyLayout(animated: true);
        }

        private void RefreshEnemyLayout(bool animated = false)
        {
            _enemyCards.RemoveAll(v => v == null);
            if (_enemyCards.Count == 0) return;

            float totalWidth = (_enemyCards.Count - 1) * _enemyCardSpacing;
            float startX     = -totalWidth / 2f;

            for (int i = 0; i < _enemyCards.Count; i++)
            {
                var rect        = _enemyCards[i].GetComponent<RectTransform>();
                var current     = rect.anchoredPosition;
                var targetPos   = new Vector2(startX + i * _enemyCardSpacing, current.y);

                if (animated)
                    rect.DOAnchorPos(targetPos, _gapCloseDuration).SetEase(Ease.OutCubic);
                else
                    rect.anchoredPosition = targetPos;

                _enemyCards[i].transform.SetSiblingIndex(i);
            }
        }

        public IEnumerator PlayEnemyCardAnimation(CardSO card, RectTransform playZone,
            System.Action onComplete)
        {
            CardView view = _enemyCards.Find(v => v != null && v.CardData == card);
            if (view == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            _enemyCards.Remove(view);
            RefreshEnemyLayout();

            var rect = view.GetComponent<RectTransform>();
            view.transform.SetParent(_dragCanvas.transform, true);
            view.transform.SetAsLastSibling();

            // Capture the enemy hand card size as the start scale
            Vector3 startScale = rect.localScale;
            // Target display scale — how large it grows at the play zone
            Vector3 displayScale = startScale * _enlargedCardScale;

            // Flip to face-up so the card art is visible during travel
            view.Setup(card);

            // Move to play zone and scale up simultaneously during travel
            rect.DOAnchorPos(playZone.anchoredPosition, _cardMoveDuration)
                .SetEase(Ease.OutCubic);
            rect.DOScale(displayScale, _cardMoveDuration)
                .SetEase(Ease.OutBack);

            yield return new WaitForSeconds(_cardMoveDuration);

            // Hold at display size — either until the player clicks to continue, or for a fixed
            // duration, per OptionsPanel's Gameplay toggle.
            bool requireClick = _gameplaySettings != null && _gameplaySettings.RequireClickToDismissEnemyCard;

            if (requireClick && _enemyCardDismissButton != null)
            {
                bool dismissed = false;
                void OnDismissClicked() => dismissed = true;

                _enemyCardDismissButton.onClick.AddListener(OnDismissClicked);
                _enemyCardDismissButton.gameObject.SetActive(true);

                yield return new WaitUntil(() => dismissed);

                _enemyCardDismissButton.onClick.RemoveListener(OnDismissClicked);
                _enemyCardDismissButton.gameObject.SetActive(false);
            }
            else
            {
                float holdDuration = _gameplaySettings != null
                    ? _gameplaySettings.EnemyCardAutoDismissDuration
                    : _cardDisplayDuration;
                yield return new WaitForSeconds(holdDuration);
            }

            // Scale back down and fade out together
            var canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = view.gameObject.AddComponent<CanvasGroup>();

            rect.DOScale(startScale * 0.5f, _cardFadeDuration)
                .SetEase(Ease.InBack);
            canvasGroup.DOFade(0f, _cardFadeDuration)
                .SetEase(Ease.InQuad);

            yield return new WaitForSeconds(_cardFadeDuration);

            // Reset scale before releasing — avoids DOTween leaving dirty state
            rect.localScale = startScale;
            ReleaseCardView(view);
            onComplete?.Invoke();
        }
        
        // Explicit interface — ICard parameter, delegates to public CardSO method
        IEnumerator IHandLayoutManager.AnimateReactionAbsorb(ICard card, System.Action onArrived) =>
            AnimateReactionAbsorb(card as CardSO, onArrived);

        // Public method — accessible from concrete type references (GameBootstrapper).
        // Card must already have a live CardView sitting in _playerCards (from AnimateManualDraw
        // or DealOpeningHandAnimated) — this just flies that existing view to the reaction badge
        // area and shrinks/fades it away, then releases it.
        public IEnumerator AnimateReactionAbsorb(CardSO card, System.Action onArrived)
        {
            if (card == null) { onArrived?.Invoke(); yield break; }

            CardView view = _playerCards.Find(v => v != null && v.CardData == card);
            if (view == null) { onArrived?.Invoke(); yield break; }

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
            {
                drag.OnDragStarted -= OnCardDragStarted;
                drag.OnDragEnded   -= OnCardDragEnded;
            }

            _playerCards.Remove(view);
            DisableHover(view);
            RefreshPlayerLayout(animated: true);

            var rect        = view.GetComponent<RectTransform>();
            var canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;

            // Convert the badge anchor's world position into playerHandContainer-local space —
            // same technique AnimateManualDraw uses for the deck position — so this works
            // regardless of whether the badge row lives under a different canvas/hierarchy.
            Vector2 targetLocalPos = rect.anchoredPosition;
            if (_reactionBadgeAnchor != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _playerHandContainer,
                    RectTransformUtility.WorldToScreenPoint(null, _reactionBadgeAnchor.position),
                    null,
                    out targetLocalPos);
            }

            var sequence = DOTween.Sequence();
            sequence.Append(rect.DOAnchorPos(targetLocalPos, _reactionAbsorbDuration).SetEase(Ease.InQuad));
            sequence.Join(rect.DOScale(Vector3.zero, _reactionAbsorbDuration).SetEase(Ease.InBack));
            sequence.Join(canvasGroup.DOFade(0f, _reactionAbsorbDuration).SetEase(Ease.InQuad));

            yield return sequence.WaitForCompletion();

            // The charge/badge-count increment happens exactly here — once the card has fully
            // vanished — not before, so the badge visibly appearing/incrementing reads as a
            // direct consequence of the card's arrival instead of happening in advance.
            onArrived?.Invoke();

            rect.localScale = Vector3.one;
            ReleaseCardView(view);
        }
    }
}