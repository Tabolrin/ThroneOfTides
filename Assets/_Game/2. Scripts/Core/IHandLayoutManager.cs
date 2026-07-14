using System.Collections;

namespace ThroneOfTides.Core
{
    public interface IHandLayoutManager
    {
        void AddCardToPlayerHand(ICard card);
        void RemoveCardFromPlayerHand(ICard card);
        void StealCardFromEnemyHand(ICard card);

        /// Adds a card straight into the enemy's face-down hand — used when an enemy-cast
        /// effect (e.g. Monkey Grab) steals a card from the player into its own hand.
        void AddCardToEnemyHand(ICard card);

        IEnumerator AnimateManualDraw(ICard card);
        // Reaction cards animate to the effects bar instead of the hand
        IEnumerator AnimateReactionDraw(ICard card);
    }
}