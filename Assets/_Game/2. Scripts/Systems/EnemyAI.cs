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
        /// <param name="playerDeadMansTurnCharges">How many Dead Man's Turn charges the player
        /// is holding - each one is a free full negate of a future non-unblockable attack, so
        /// more charges means more reason to play around it, not just a yes/no threat.</param>
        /// <param name="playerCounterGaleCharges">How many Counter Gale charges the player is
        /// holding - each one reflects half the damage of a future non-unblockable attack.</param>
        /// <param name="selfUnblockable">This side's next attack is already guaranteed to land
        /// (Siren Song already resolved this turn) - reaction-threat weighting is skipped since
        /// there's nothing left to play around.</param>
        public CardSO PickCard(IReadOnlyList<CardSO> hand, int enemyMana, int enemyHP,
            bool comboPrimed = false, int playerDeadMansTurnCharges = 0,
            int playerCounterGaleCharges = 0, bool selfUnblockable = false)
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

            ApplyReactionAwareness(candidates, playerDeadMansTurnCharges, playerCounterGaleCharges, selfUnblockable);

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
            int playerDeadMansTurnCharges, int playerCounterGaleCharges, bool selfUnblockable)
        {
            float caution = _captain.WeightPlayAroundReactions;
            bool anyReactionThreat = playerDeadMansTurnCharges > 0 || playerCounterGaleCharges > 0;
            if (caution <= 0f || selfUnblockable || !anyReactionThreat) return;

            // A second (or third) charge of the same reaction doesn't double the threat the way
            // going from 0 to 1 does - the player was already going to block/reflect one attack
            // either way, extra charges just mean they can keep doing it later too. Scaled by
            // sqrt(charges) for diminishing returns: sqrt(1) = 1 (identical to the old flat
            // boolean behavior at one charge), sqrt(2) ~= 1.41, sqrt(3) ~= 1.73, and so on.
            float dmtScale = Mathf.Sqrt(playerDeadMansTurnCharges);
            float cgScale  = Mathf.Sqrt(playerCounterGaleCharges);

            // A full negate is worth playing around much harder than a half-damage reflect -
            // whichever reaction is actually charged (Dead Man's Turn takes priority if the
            // player holds both, matching the human reaction-prompt's own precedence) sets the
            // dampen applied to ordinary attacks below.
            float dampen = playerDeadMansTurnCharges > 0
                ? Mathf.Max(0.15f, 1f - 0.5f  * caution * dmtScale)
                : Mathf.Max(0.4f,  1f - 0.25f * caution * cgScale);

            // The unblockable-card boost scales off whichever reaction is more heavily stacked -
            // more charges (of either type) means landing a guaranteed hit is worth chasing
            // harder, since a dampened normal attack would otherwise just feed one more charge
            // opportunity that's still there next turn.
            float unblockableBoost = 1f + caution * Mathf.Max(dmtScale, cgScale);

            for (int i = 0; i < candidates.Count; i++)
            {
                var (card, weight) = candidates[i];
                bool isUnblockableCard = card.Id == CardId.Kraken || card.Id == CardId.SirenSong;
                bool isAttack = card.CardType == CardType.Weapon ||
                                card.CardType == CardType.Combo  ||
                                card.CardType == CardType.DOT;

                if (isUnblockableCard)
                    candidates[i] = (card, weight * unblockableBoost);
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