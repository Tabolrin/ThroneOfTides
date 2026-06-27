using System.Collections;

namespace ThroneOfTides.Core
{
    public interface IHandLayoutManager
    {
        void AddCardToPlayerHand(ICard card);
        void RemoveCardFromPlayerHand(ICard card);
        void StealCardFromEnemyHand(ICard card);
        IEnumerator AnimateManualDraw(ICard card);
        // Reaction cards animate to the effects bar instead of the hand
        IEnumerator AnimateReactionDraw(ICard card);
    }
}