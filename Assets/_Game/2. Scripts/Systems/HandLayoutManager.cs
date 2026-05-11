using System.Collections.Generic;
using UnityEngine;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class HandLayoutManager : MonoBehaviour
    {
        [SerializeField] private RectTransform _playerHandContainer;
        [SerializeField] private RectTransform _enemyHandContainer;
        [SerializeField] private CardView      _cardPrefab;
        [SerializeField] private Canvas        _dragCanvas;

        private readonly List<CardView> _playerCards = new List<CardView>();
        private readonly List<CardView> _enemyCards  = new List<CardView>();

        public void AddCardToPlayerHand(CardSO card)
        {
            CardView view = Instantiate(_cardPrefab, _playerHandContainer);
            view.Setup(card);

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
                drag.SetDragCanvas(_dragCanvas);

            _playerCards.Add(view);
        }

        public void AddCardToEnemyHand(CardSO card)
        {
            CardView view = Instantiate(_cardPrefab, _enemyHandContainer);
            view.SetFaceDown();
            _enemyCards.Add(view);
        }

        public void RemoveCardFromPlayerHand(CardSO card)
        {
            CardView view = _playerCards.Find(v => v.CardData == card);
            if (view == null) return;
            _playerCards.Remove(view);
            Destroy(view.gameObject);
        }

        public void RemoveCardFromEnemyHand()
        {
            if (_enemyCards.Count == 0) return;
            CardView view = _enemyCards[0];
            _enemyCards.Remove(view);
            Destroy(view.gameObject);
        }

        public void ClearPlayerHand()
        {
            foreach (var card in _playerCards) Destroy(card.gameObject);
            _playerCards.Clear();
        }
    }
}