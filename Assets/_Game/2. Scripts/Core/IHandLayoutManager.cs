using System.Collections;

namespace ThroneOfTides.Core
{
    public interface IHandLayoutManager
    {
        void AddCardToPlayerHand(ICard card);
        void RemoveCardFromPlayerHand(ICard card);
        void StealCardFromEnemyHand(ICard card);
        IEnumerator AnimateManualDraw(ICard card);
    }
}