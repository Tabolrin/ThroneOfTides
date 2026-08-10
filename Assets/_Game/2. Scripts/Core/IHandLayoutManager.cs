using System.Collections;

namespace ThroneOfTides.Core
{
    public interface IHandLayoutManager
    {
        void AddCardToPlayerHand(ICard card);
        void RemoveCardFromPlayerHand(ICard card);
        void StealCardFromEnemyHand(ICard card);

        /// Reparents the player's existing CardView into the enemy's hand (flipping it face-down)
        /// and animates it into the enemy's fanned layout — the reverse of StealCardFromEnemyHand,
        /// for when an enemy-cast effect (e.g. Monkey Grab) steals a card from the player.
        void StealCardFromPlayerHand(ICard card);

        /// Adds a card straight into the enemy's face-down hand — used for cards the enemy
        /// otherwise gains (not stolen from an existing visible hand card).
        void AddCardToEnemyHand(ICard card);

        IEnumerator AnimateManualDraw(ICard card);
        // Reaction cards animate to the effects bar instead of the hand
        IEnumerator AnimateReactionDraw(ICard card);
    }
}