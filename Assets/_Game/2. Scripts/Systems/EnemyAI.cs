using System.Collections.Generic;
using ThroneOfTides.Data;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems
{
    // Selects cards for the enemy using weight-based random selection
    // Captain archetype and personality defined entirely by CaptainSO weights
    public class EnemyAI
    {
        private readonly CaptainSO _captain;

        public EnemyAI(CaptainSO captain)
        {
            _captain = captain;
        }

        // Picks one card from enemy hand weighted by CaptainSO weight table
        public CardSO PickCard(IReadOnlyList<CardSO> hand, bool damageCardPlayed, bool actionCardPlayed)
        {
            if (hand.Count == 0) return null;

            var candidates = new List<(CardSO card, float weight)>();

            foreach (var card in hand)
            {
                bool isDamageCard = card.CardType == CardType.Weapon ||
                                    card.CardType == CardType.Combo  ||
                                    card.CardType == CardType.DOT;
                bool isActionCard = card.CardType == CardType.Action;

                if (isDamageCard && damageCardPlayed) continue;
                if (isActionCard && (actionCardPlayed || !card.IsEligibleAsActionPair)) continue;

                float weight = _captain.GetWeightForCard(card);

                // Zero weight means captain never plays this card
                if (weight <= 0f) continue;

                candidates.Add((card, weight));
            }

            if (candidates.Count == 0) return null;

            return WeightedRandom(candidates);
        }

        private CardSO WeightedRandom(List<(CardSO card, float weight)> candidates)
        {
            float totalWeight = 0f;
            foreach (var c in candidates)
                totalWeight += c.weight;

            float roll       = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var c in candidates)
            {
                cumulative += c.weight;
                if (roll <= cumulative)
                    return c.card;
            }

            return candidates[candidates.Count - 1].card;
        }
    }
}