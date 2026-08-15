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
        /// Decides whether the enemy uses a charged reaction to defend against an incoming
        /// attack, and which one. Thin wrapper - the actual weighted decision lives on the
        /// Captain, same as every other card-choice weight.
        /// </summary>
        public ReactionType? ChooseReaction(bool hasDeadMansTurn, bool hasCounterGale) =>
            _captain.ChooseReaction(hasDeadMansTurn, hasCounterGale);

        /// <summary>
        /// Picks the next card the enemy should play this turn, or null if nothing in hand is
        /// currently playable (caller should end the turn). Called once per card played - the
        /// caller re-invokes this after each play since hand/mana/HP change each time.
        /// </summary>
        /// <param name="comboPrimed">
        /// True once the enemy has already primed its own combo (e.g. Gunpowder Barrel) this
        /// match and has an unspent stack - a follow-up finisher (Torch) is always the correct
        /// play once available, so it's forced through rather than left to weighted RNG, which
        /// could otherwise waste the turn (or the whole match) on something else while the
        /// primed stack just sits there.
        /// </param>
        /// <param name="playerHasDeadMansTurn">Player has a Dead Man's Turn charge - the next
        /// non-unblockable attack would be fully negated for free.</param>
        /// <param name="playerHasCounterGale">Player has a Counter Gale charge - the next
        /// non-unblockable attack gets half its damage reflected back.</param>
        /// <param name="selfUnblockable">This side's next attack is already guaranteed to land
        /// (Siren Song already resolved this turn) - reaction-threat weighting is skipped since
        /// there's nothing left to play around.</param>
        public CardSO PickCard(IReadOnlyList<CardSO> hand, int enemyMana, int enemyHP,
            bool comboPrimed = false, bool playerHasDeadMansTurn = false,
            bool playerHasCounterGale = false, bool selfUnblockable = false)
        {
            if (hand.Count == 0) return null;

            var candidates = new List<(CardSO card, float weight)>();

            foreach (var card in hand)
            {
                // Reaction cards are never played from hand by the enemy -
                // enemy AI doesn't hold reaction cards in normal gameplay
                if (card.CardType == CardType.Reaction) continue;

                // Cards requiring a player-chosen target (e.g. Tidal Wave) have no AI-facing
                // targeting UI - excluded until the AI gets its own targeting heuristic.
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

            // A primed combo is always worth cashing in the instant it's available - no
            // personality reads this differently, so it bypasses weighting entirely.
            if (comboPrimed)
            {
                var finisher = candidates.FirstOrDefault(c => c.card.Id == CardId.Torch);
                if (finisher.card != null) return finisher.card;
            }

            ApplyReactionAwareness(candidates, playerHasDeadMansTurn, playerHasCounterGale, selfUnblockable);

            // Prefer cards that are more valuable played before an attack (Siren Song, Monkey
            // Grab, etc.) - if any are still playable, restrict the pick to that group; only
            // fall back to the full candidate pool (including attacks) once none remain. Still
            // uses weighted-random selection within whichever pool is active, so the captain's
            // weight table still governs which specific card gets picked.
            var preferred = candidates.Where(c => c.card.AiPlayBeforeAttack).ToList();
            var pool      = preferred.Count > 0 ? preferred : candidates;

            return WeightedRandom(pool);
        }

        // Plays around the player's charged reactions, scaled by the captain's own
        // WeightPlayAroundReactions (0 = reckless, ignores this entirely). Siren Song and Kraken
        // are both innately unblockable, so they get a boost to land a "free" hit through a
        // charged reaction; every other attack gets dampened so the AI doesn't just feed its
        // best hit into a guaranteed Dead Man's Turn negate.
        private void ApplyReactionAwareness(List<(CardSO card, float weight)> candidates,
            bool playerHasDeadMansTurn, bool playerHasCounterGale, bool selfUnblockable)
        {
            float caution = _captain.WeightPlayAroundReactions;
            bool anyReactionThreat = playerHasDeadMansTurn || playerHasCounterGale;
            if (caution <= 0f || selfUnblockable || !anyReactionThreat) return;

            // A full negate is worth playing around much harder than a half-damage reflect.
            float dampen = playerHasDeadMansTurn
                ? Mathf.Max(0.15f, 1f - 0.5f  * caution)
                : Mathf.Max(0.4f,  1f - 0.25f * caution);

            for (int i = 0; i < candidates.Count; i++)
            {
                var (card, weight) = candidates[i];
                bool isUnblockableCard = card.Id == CardId.Kraken || card.Id == CardId.SirenSong;
                bool isAttack = card.CardType == CardType.Weapon ||
                                card.CardType == CardType.Combo  ||
                                card.CardType == CardType.DOT;

                if (isUnblockableCard)
                    candidates[i] = (card, weight * (1f + caution));
                else if (isAttack)
                    candidates[i] = (card, weight * dampen);
            }
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