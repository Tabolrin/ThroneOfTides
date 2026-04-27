using System.Collections.Generic;
using UnityEngine;
using ThroneOfTides.Data;
using ThroneOfTides.UI;

namespace ThroneOfTides.Systems
{
    public class HandLayoutManager : MonoBehaviour
    {
        [SerializeField] private Transform    _playerHandAnchor;
        [SerializeField] private Transform    _enemyHandAnchor;
        [SerializeField] private CardView     _cardPrefab;
        [SerializeField] private float        _cardSpacing = 1.0f;

        private List<CardView> _playerCards = new List<CardView>();
        private List<CardView> _enemyCards  = new List<CardView>();

        public void AddCardToPlayerHand(CardSO card)
        {
            CardView view = Instantiate(_cardPrefab, _playerHandAnchor);
            view.Setup(card);
            _playerCards.Add(view);
            RefreshLayout(_playerCards, _playerHandAnchor);
        }

        public void AddCardToEnemyHand(CardSO card)
        {
            // Enemy cards spawn face-down - no Setup call
            CardView view = Instantiate(_cardPrefab, _enemyHandAnchor);
            _enemyCards.Add(view);
            RefreshLayout(_enemyCards, _enemyHandAnchor);
        }

        public void RemoveCardFromPlayerHand(CardView card)
        {
            if (!_playerCards.Remove(card)) return;
            Object.Destroy(card.gameObject);
            RefreshLayout(_playerCards, _playerHandAnchor);
        }

        // Centers cards around anchor - evenly spaced
        private void RefreshLayout(List<CardView> cards, Transform anchor)
        {
            if (cards.Count == 0) return;

            float totalWidth = (cards.Count - 1) * _cardSpacing;
            float startX     = anchor.position.x - totalWidth / 2f;

            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].transform.position = new Vector3(
                    startX + i * _cardSpacing,
                    anchor.position.y,
                    anchor.position.z
                );
            }
        }
    }
}