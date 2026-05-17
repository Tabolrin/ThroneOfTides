using System;
using System.Collections.Generic;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    public enum DeckState { Normal, Low, Empty }

    public class Deck
    {
        private List<CardSO> _cards;
        private DeckState    _currentState = DeckState.Normal;
        private readonly int _lowThreshold;

        public int       Count => _cards.Count;
        public DeckState State => _currentState;

        public Action<DeckState> OnDeckStateChanged;

        public Deck(List<CardSO> cardList, int lowThreshold = 3)
        {
            _cards        = new List<CardSO>(cardList);
            _lowThreshold = lowThreshold;
            Shuffle();
        }

        public CardSO Draw()
        {
            if (_cards.Count <= 0) return null;
            CardSO drawn = _cards[0];
            _cards.RemoveAt(0);
            CheckDeckState();
            return drawn;
        }

        // Returns a card to the bottom of the deck - used by Locker's Return
        public void ReturnCard(CardSO card)
        {
            _cards.Add(card);
            CheckDeckState();
        }

        private void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (_cards[i], _cards[randomIndex]) = (_cards[randomIndex], _cards[i]);
            }
        }

        private void CheckDeckState()
        {
            DeckState newState = _cards.Count == 0             ? DeckState.Empty  :
                _cards.Count <= _lowThreshold ? DeckState.Low    :
                DeckState.Normal;
            if (newState == _currentState) return;
            _currentState = newState;
            OnDeckStateChanged?.Invoke(_currentState);
        }
    }
}