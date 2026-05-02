using System;
using System.Collections.Generic;
using System.Linq;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    public enum HandState { Normal, Full, Empty }

    public class Hand
    {
        private readonly List<CardSO> _cards = new List<CardSO>();
        private HandState             _currentState = HandState.Empty;

        public int       Count => _cards.Count;
        public HandState State => _currentState;

        // ICard for cross-assembly access - UI holds CardSO refs directly via CardView
        public IReadOnlyList<ICard>  Cards   => _cards.Cast<ICard>().ToList().AsReadOnly();

        // CardSO for internal Systems use only
        public IReadOnlyList<CardSO> CardsSO => _cards.AsReadOnly();

        // Fires only on state transition
        public Action<HandState> OnHandStateChanged;

        public void AddCard(CardSO card, int maxHandSize)
        {
            if (_cards.Count >= maxHandSize) return;
            _cards.Add(card);
            CheckHandState(maxHandSize);
        }

        public bool RemoveCard(CardSO card)
        {
            bool removed = _cards.Remove(card);
            // maxHandSize irrelevant on removal - Full cannot trigger here
            if (removed) CheckHandState(0);
            return removed;
        }

        public bool HasCard(CardSO card) => _cards.Contains(card);

        private void CheckHandState(int maxHandSize)
        {
            HandState newState = _cards.Count == 0                              ? HandState.Empty :
                maxHandSize > 0 && _cards.Count >= maxHandSize ? HandState.Full  :
                HandState.Normal;
            if (newState == _currentState) return;
            _currentState = newState;
            OnHandStateChanged?.Invoke(_currentState);
        }
    }
}