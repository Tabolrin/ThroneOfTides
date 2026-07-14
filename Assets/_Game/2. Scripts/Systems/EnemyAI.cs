// Assets/_Game/2. Scripts/Systems/EnemyAI.cs
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Picks the next card the enemy should play this turn, or null if nothing in hand is
        /// currently playable (caller should end the turn). Called once per card played — the
        /// caller re-invokes this after each play since hand/mana/HP change each time.
        /// </summary>
        public CardSO PickCard(IReadOnlyList<CardSO> hand, int enemyMana, int enemyHP)
        {
            if (hand.Count == 0) return null;

            var candidates = new List<(CardSO card, float weight)>();

            foreach (var card in hand)
            {
                // Reaction cards are never played from hand by the enemy —
                // enemy AI doesn't hold reaction cards in normal gameplay
                if (card.CardType == CardType.Reaction) continue;

                // Cards requiring a player-chosen target (e.g. Tidal Wave) have no AI-facing
                // targeting UI — excluded until the AI gets its own targeting heuristic.
                if (card.RequiresTargetSelection) continue;

                // Cannot play cards that cost more mana than currently available
                if (card.ManaCost > enemyMana) continue;

                // Never play a card that would reduce the enemy to 0 HP or below.
                if (card.HPCost >= enemyHP) continue;

                float weight = _captain.GetWeightForCard(card);
                if (weight <= 0f) continue;

                candidates.Add((card, weight));
            }

            if (candidates.Count == 0) return null;

            // Prefer cards that are more valuable played before an attack (Siren Song, Monkey
            // Grab, etc.) — if any are still playable, restrict the pick to that group; only
            // fall back to the full candidate pool (including attacks) once none remain. Still
            // uses weighted-random selection within whichever pool is active, so the captain's
            // weight table still governs which specific card gets picked.
            var preferred = candidates.Where(c => c.card.AiPlayBeforeAttack).ToList();
            var pool      = preferred.Count > 0 ? preferred : candidates;

            return WeightedRandom(pool);
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