using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/LockerReturn")]
    public class LockerReturnEffectSO : ActionEffectSO
    {
        [SerializeField] private int _cardsToRetrieve = 3;

        public override void Execute(ICardEffectContext context)
        {
            context.RetrieveFromDiscard(_cardsToRetrieve);
            Debug.Log($"Locker's Return - retrieved {_cardsToRetrieve} cards from discard");
        }
    }
}