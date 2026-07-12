// Assets/_Game/2. Scripts/Systems/EnemyAI.cs
using System.Collections.Generic;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    // Selects cards for the enemy using weight-based random selection.
    // Captain personality defined entirely by CaptainSO weight table.
    public class EnemyAI
    {
        private readonly CaptainSO _captain;

        public EnemyAI(CaptainSO captain)
        {
            _captain = captain;
        }

        public CardSO PickCard(IReadOnlyList<CardSO> hand, bool damageCardPlayed,
                               bool actionCardPlayed, int enemyMana)
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

                // Reaction cards are never played from hand by the enemy —
                // enemy AI doesn't hold reaction cards in normal gameplay
                if (card.CardType == CardType.Reaction) continue;

                // Cards requiring a player-chosen target (e.g. Tidal Wave) have no AI-facing
                // targeting UI — excluded until the AI gets its own targeting heuristic.
                if (card.RequiresTargetSelection) continue;

                // Cannot play cards that cost more mana than currently available
                if (card.ManaCost > enemyMana) continue;

                float weight = _captain.GetWeightForCard(card);
                if (weight <= 0f) continue;

                candidates.Add((card, weight));
            }

            return candidates.Count == 0 ? null : WeightedRandom(candidates);
        }

        private static CardSO WeightedRandom(List<(CardSO card, float weight)> candidates)
        {
            float total      = 0f;
            foreach (var c in candidates) total += c.weight;

            float roll       = Random.Range(0f, total);
            float cumulative = 0f;

            foreach (var c in candidates)
            {
                cumulative += c.weight;
                if (roll <= cumulative) return c.card;
            }
            return candidates[candidates.Count - 1].card;
        }
    }
}