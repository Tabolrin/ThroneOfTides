using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

        private readonly List<CardView> _playerCards = new List<CardView>();
        private readonly List<CardView> _enemyCards  = new List<CardView>();

        // --- IHandLayoutManager interface ---

        void IHandLayoutManager.AddCardToPlayerHand(ICard card) =>
            AddCardToPlayerHand(card as CardSO);

        void IHandLayoutManager.RemoveCardFromPlayerHand(ICard card) =>
            RemoveCardFromPlayerHand(card as CardSO);

        void IHandLayoutManager.StealCardFromEnemyHand(ICard card) =>
            StealCardFromEnemyHand(card as CardSO);

        // --- Player Hand ---

        public void AddCardToPlayerHand(CardSO card)
        {
            CardView view = Instantiate(_cardPrefab, _playerHandContainer);
            view.Setup(card);

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
                drag.SetDragCanvas(_dragCanvas);

            var rect = view.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector2 pos = rect.anchoredPosition;
                pos.y = Random.Range(-_maxYOffset, _maxYOffset);
                rect.anchoredPosition = pos;
            }

            _playerCards.Add(view);
            RefreshPlayerLayout();
        }

        // Only used for non-drag removals e.g. Dead Man's Turn block, Monkey Grab
        public void RemoveCardFromPlayerHand(CardSO card)
        {
            CardView view = _playerCards.Find(v => v != null && v.CardData == card);
            if (view == null)
            {
                _playerCards.RemoveAll(v => v == null);
                RefreshPlayerLayout();
                return;
            }
            _playerCards.Remove(view);
            Destroy(view.gameObject);
            RefreshPlayerLayout();
        }

        public void ClearPlayerHand()
        {
            foreach (var c in _playerCards)
                if (c != null) Destroy(c.gameObject);
            _playerCards.Clear();
        }

        private void RefreshPlayerLayout()
        {
            _playerCards.RemoveAll(v => v == null);
            if (_playerCards.Count == 0) return;

            float totalWidth = (_playerCards.Count - 1) * _cardSpacing;
            float startX     = -totalWidth / 2f;

            for (int i = 0; i < _playerCards.Count; i++)
            {
                var rect    = _playerCards[i].GetComponent<RectTransform>();
                var current = rect.anchoredPosition;
                rect.anchoredPosition = new Vector2(startX + i * _cardSpacing, current.y);
                _playerCards[i].transform.SetSiblingIndex(i);
            }
        }

        // --- Enemy Hand ---

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

            view.transform.SetParent(_playerHandContainer, true);
            view.Setup(card);

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
                drag.SetDragCanvas(_dragCanvas);

            var rect = view.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector2 pos = rect.anchoredPosition;
                pos.y = Random.Range(-_maxYOffset, _maxYOffset);
                rect.anchoredPosition = pos;
            }

            _playerCards.Add(view);
            RefreshPlayerLayout();
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

            rect.DOAnchorPos(playZone.anchoredPosition, _cardMoveDuration)
                .SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(_cardMoveDuration);

            var canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = view.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.DOFade(0f, _cardFadeDuration / 2f)
                .OnComplete(() =>
                {
                    view.Setup(card);
                    canvasGroup.DOFade(1f, _cardFadeDuration / 2f);
                });

            yield return new WaitForSeconds(_cardFadeDuration + _cardDisplayDuration);

            Destroy(view.gameObject);
            onComplete?.Invoke();
        }
    }
}