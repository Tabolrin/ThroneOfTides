using System.Collections.Generic;
using UnityEngine;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class HandLayoutManager : MonoBehaviour
    {
        [SerializeField] private RectTransform _playerHandContainer;
        [SerializeField] private CardView      _cardPrefab;
        [SerializeField] private Canvas        _dragCanvas;

        [Header("Layout")]
        // Negative value = cards overlap. 10-15% of card width (card is 120px wide so ~15-18px)
        [SerializeField] private float _cardSpacing    = -18f;
        [SerializeField] private float _maxYOffset     = 15f;

        private readonly List<CardView> _playerCards = new List<CardView>();

        private void Start()
        {
            // Register inspect view for right click - finds it in scene
            CardView.InspectView = FindObjectOfType<CardInspectView>();
        }

        public void AddCardToPlayerHand(CardSO card)
        {
            CardView view = Instantiate(_cardPrefab, _playerHandContainer);
            view.Setup(card);

            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
                drag.SetDragCanvas(_dragCanvas);

            // Random Y offset for organic hand feel
            var rect = view.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector2 pos = rect.anchoredPosition;
                pos.y = Random.Range(-_maxYOffset, _maxYOffset);
                rect.anchoredPosition = pos;
            }

            _playerCards.Add(view);
            RefreshLayout();
        }

        public void RemoveCardFromPlayerHand(CardSO card)
        {
            CardView view = _playerCards.Find(v => v.CardData == card);
            if (view == null) return;
            _playerCards.Remove(view);
            Destroy(view.gameObject);
            RefreshLayout();
        }

        public void ClearPlayerHand()
        {
            foreach (var card in _playerCards) Destroy(card.gameObject);
            _playerCards.Clear();
        }

        // Positions cards with overlap, preserving individual Y offsets
        private void RefreshLayout()
        {
            if (_playerCards.Count == 0) return;

            float totalWidth  = (_playerCards.Count - 1) * _cardSpacing;
            float startX      = -totalWidth / 2f;

            for (int i = 0; i < _playerCards.Count; i++)
            {
                var rect    = _playerCards[i].GetComponent<RectTransform>();
                var current = rect.anchoredPosition;
                // Preserve Y offset set on spawn, only update X
                rect.anchoredPosition = new Vector2(startX + i * _cardSpacing, current.y);
                // Later cards render on top
                _playerCards[i].transform.SetSiblingIndex(i);
            }
        }
    }
}