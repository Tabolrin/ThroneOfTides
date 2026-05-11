using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class HandLayoutManager : MonoBehaviour
    {
        [SerializeField] private RectTransform _playerHandContainer;
        [SerializeField] private RectTransform _enemyHandContainer;
        [SerializeField] private CardView      _cardPrefab;
<<<<<<< HEAD
        [SerializeField] private Canvas        _dragCanvas;
<<<<<<< HEAD
=======
>>>>>>> parent of 9c6ae3c (Merge branch 'claude/lucid-williams-9bfc13' into Tests)

        [Header("Arc Settings")]
        [SerializeField] private float _arcRadius  = 800f;
        [SerializeField] private float _totalAngle = 60f;

        [Header("Animation")]
        [SerializeField] private float _lerpSpeed = 8f;
=======
>>>>>>> parent of d5a8aee (Merge branch 'claude/lucid-williams-9bfc13' into Tests)

        private readonly List<CardView> _playerCards = new List<CardView>();
        private readonly List<CardView> _enemyCards  = new List<CardView>();

        public void AddCardToPlayerHand(CardSO card)
        {
<<<<<<< HEAD
<<<<<<< HEAD
            CardView view = SpawnCard(_playerHandAnchor);
=======
            CardView view = Instantiate(_cardPrefab, _playerHandContainer);
>>>>>>> parent of d5a8aee (Merge branch 'claude/lucid-williams-9bfc13' into Tests)
=======
            CardView view = Instantiate(_cardPrefab, _playerHandAnchor);
>>>>>>> parent of 9c6ae3c (Merge branch 'claude/lucid-williams-9bfc13' into Tests)
            view.Setup(card);

            // Assign drag canvas reference to drag handler
            var drag = view.GetComponent<CardDragHandler>();
            if (drag != null)
                drag.SetDragCanvas(_dragCanvas);

            _playerCards.Add(view);
        }

        public void AddCardToEnemyHand(CardSO card)
        {
<<<<<<< HEAD
<<<<<<< HEAD
            _enemyCards.Add(SpawnCard(_enemyHandAnchor));
        }

        private CardView SpawnCard(RectTransform anchor)
        {
            CardView view = Instantiate(_cardPrefab, anchor);
            // Inject drag canvas so the card can re-parent itself on drag
            view.GetComponent<CardDragHandler>()?.SetDragCanvas(_dragCanvas);
            return view;
=======
            CardView view = Instantiate(_cardPrefab, _enemyHandAnchor);
            _enemyCards.Add(view);
>>>>>>> parent of 9c6ae3c (Merge branch 'claude/lucid-williams-9bfc13' into Tests)
        }

        public void RemoveCardFromPlayerHand(CardView card)
=======
            CardView view = Instantiate(_cardPrefab, _enemyHandContainer);
            view.SetFaceDown();
            _enemyCards.Add(view);
        }

<<<<<<< HEAD
        public void RemoveCardFromPlayerHand(CardSO card)
>>>>>>> parent of d5a8aee (Merge branch 'claude/lucid-williams-9bfc13' into Tests)
        {
            CardView view = _playerCards.Find(v => v.CardData == card);
            if (view == null) return;
            _playerCards.Remove(view);
            Destroy(view.gameObject);
        }

        public void RemoveCardFromEnemyHand()
=======
        public void RemoveCardFromPlayerHand(CardView card)
>>>>>>> parent of 37e8df4 (Merge branch 'claude/lucid-williams-9bfc13' into Tests)
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