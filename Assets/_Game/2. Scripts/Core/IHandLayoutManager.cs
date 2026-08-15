using System.Collections;

namespace ThroneOfTides.Core
{
    public interface IHandLayoutManager
    {
        void AddCardToPlayerHand(ICard card);
        void RemoveCardFromPlayerHand(ICard card);
        void StealCardFromEnemyHand(ICard card);

        /// Reparents the player's existing CardView into the enemy's hand (flipping it face-down)
        /// and animates it into the enemy's fanned layout - the reverse of StealCardFromEnemyHand,
        /// for when an enemy-cast effect (e.g. Monkey Grab) steals a card from the player.
        void StealCardFromPlayerHand(ICard card);

        /// Adds a card straight into the enemy's face-down hand - used for cards the enemy
        /// otherwise gains (not stolen from an existing visible hand card).
        void AddCardToEnemyHand(ICard card);

        IEnumerator AnimateManualDraw(ICard card);

        /// Card already sits visually in the player's hand (dealt via AnimateManualDraw or the
        /// opening deal, exactly like a normal card) - flies it from its hand slot to the
        /// reaction badge area and shrinks it away. onArrived fires the instant it's fully gone,
        /// so the caller can apply the actual charge increment in sync with the vanish instead
        /// of before the player has seen the card at all.
        IEnumerator AnimateReactionAbsorb(ICard card, System.Action onArrived);
    }
}