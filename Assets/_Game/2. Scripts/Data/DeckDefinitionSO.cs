using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/DeckDefinition")]
    public class DeckDefinitionSO : ScriptableObject
    {
        [Serializable]
        public struct CardEntry
        {
            public CardSO Card;
            public int    Count;
        }

        public List<CardEntry> Cards;

        // Expands entries into a flat list respecting count per card
        public List<CardSO> BuildDeck()
        {
            var deck = new List<CardSO>();
            foreach (var entry in Cards)
            {
                if (entry.Card == null || entry.Count <= 0) continue;
                for (int i = 0; i < entry.Count; i++)
                    deck.Add(entry.Card);
            }
            return deck;
        }
    }
}