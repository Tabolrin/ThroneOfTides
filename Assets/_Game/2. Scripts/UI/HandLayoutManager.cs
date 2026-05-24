// Assets/_Game/2. Scripts/UI/HandLayoutManager.cs
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.UI
{
    public class HandLayoutManager : MonoBehaviour, IHandLayoutManager
    {
        [SerializeField] private RectTransform _playerHandContainer;
        [SerializeField] private RectTransform _enemyHandContainer;
        [SerializeField] private CardView      _cardPrefab;
        [SerializeField] private Canvas        _dragCanvas;

        [Header("Player Hand Layout")]
        [SerializeField] private float _cardSpacing = -18f;
        [SerializeField] private float _maxYOffset  = 15f;

        [Header("Enemy Hand Layout")]
        [SerializeField] private float _enemyCardSpacing = -12f;

        [Header("Enemy Card Play")]
        [SerializeField] private float _cardMoveDuration    = 0.4f;
        [SerializeField] private float _cardFadeDuration    = 0.2f;
        [SerializeField] private float _cardDisplayDuration = 1.5f;
        [SerializeField] private float _enlargedCardScale = 1.75f;


        [Header("Opening Deal")]
        [SerializeField] private float _openingDealInterval     = 0.28f;
        [SerializeField] private float _openingSlideDistanceMin = 60f;
        [SerializeField] private float _openingSlideDistanceMax = 120f;
        [SerializeField] private float _openingSlideDuration    = 0.22f;

        [Header("Hand Animation")]
        [SerializeField] private float _gapCloseDuration = 0.12f;

        [Header("Manual Draw Animation")]
        [SerializeField] private RectTransform _deckTransform;
        [SerializeField] private float         _drawArcHeight   = 120f;
        [SerializeField] private float         _drawArcDuration = 0.35f;
        [SerializeField] private MMF_Player    _feedbackDeckDraw;

        private readonly List<CardView> _playerCards = new List<CardView>();
        private readonly List<CardView> _enemyCards  = new List<CardView>();

        // ── IHandLayoutManager ──────────────────────────────────────────────

        void IHandLayoutManager.AddCardToPlayerHand(ICard card) =>
            AddCardToPlayerHand(card as CardSO);

        void IHandLayoutManager.RemoveCardFromPlayerHand(ICard card) =>
            RemoveCardFromPlayerHand(card as CardSO);

        void IHandLayoutManager.StealCardFromEnemyHand(ICard card) =>
            StealCardFromEnemyHand(card as CardSO);

        // ICard parameter to satisfy interface — cast to CardSO internally
        IEnumerator IHandLayoutManager.AnimateManualDraw(ICard card) =>
            AnimateManualDraw(card as CardSO);

        // ── Player Hand ─────────────────────────────────────────────────────

        public void AddCardToPlayerHand(CardSO card)
        {
            CardView view = Instantiate(_cardPrefab, _playerHandContainer);
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
            Destroy(view.gameObject);
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
                Destroy(c.gameObject);
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
                CardView view = Instantiate(_cardPrefab, _playerHandContainer);
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
    CardView view = Instantiate(_cardPrefab, _playerHandContainer);
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

    var sequence = DOTween.Sequence();
    sequence.Append(
        rect.DOAnchorPos(new Vector2(midX, peakY), _drawArcDuration * 0.5f)
            .SetEase(Ease.OutQuad));
    sequence.Append(
        rect.DOAnchorPos(targetPos, _drawArcDuration * 0.5f)
            .SetEase(Ease.InQuad));

    yield return sequence.WaitForCompletion();

    // Already in playerHandContainer — just settle remaining cards
    RefreshPlayerLayout(animated: true);
}

        // ── Enemy Hand ──────────────────────────────────────────────────────

        public void AddCardToEnemyHand(CardSO card)
        {
            CardView view = Instantiate(_cardPrefab, _enemyHandContainer);
            view.SetFaceDown(card);
            _enemyCards.Add(view);
            RefreshEnemyLayout();
        }

        public void RemoveCardFromEnemyHand(CardSO card)
        {
            CardView view = _enemyCards.Find(v => v != null && v.CardData == card);
            if (view == null) return;
            _enemyCards.Remove(view);
            Destroy(view.gameObject);
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
            RefreshEnemyLayout();

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
            {
                drag.SetDragCanvas(_dragCanvas);
                drag.OnDragStarted += OnCardDragStarted;
                drag.OnDragEnded   += OnCardDragEnded;
            }

            view.transform.SetParent(_playerHandContainer, true);
            view.Setup(card);
            view.HandYOffset = Random.Range(-_maxYOffset, _maxYOffset);

            _playerCards.Add(view);
            RefreshPlayerLayout(animated: true);
        }

        private void RefreshEnemyLayout()
        {
            _enemyCards.RemoveAll(v => v == null);
            if (_enemyCards.Count == 0) return;

            float totalWidth = (_enemyCards.Count - 1) * _enemyCardSpacing;
            float startX     = -totalWidth / 2f;

            for (int i = 0; i < _enemyCards.Count; i++)
            {
                var rect    = _enemyCards[i].GetComponent<RectTransform>();
                var current = rect.anchoredPosition;
                rect.anchoredPosition = new Vector2(startX + i * _enemyCardSpacing, current.y);
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

            // Hold at display size for the display duration
            yield return new WaitForSeconds(_cardDisplayDuration);

            // Scale back down and fade out together
            var canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = view.gameObject.AddComponent<CanvasGroup>();

            rect.DOScale(startScale * 0.5f, _cardFadeDuration)
                .SetEase(Ease.InBack);
            canvasGroup.DOFade(0f, _cardFadeDuration)
                .SetEase(Ease.InQuad);

            yield return new WaitForSeconds(_cardFadeDuration);

            // Reset scale before destroying — avoids DOTween leaving dirty state
            rect.localScale = startScale;
            Destroy(view.gameObject);
            onComplete?.Invoke();
        }
    }
}