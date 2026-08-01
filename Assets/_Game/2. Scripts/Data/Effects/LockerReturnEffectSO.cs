using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/LockerReturn")]
    public class LockerReturnEffectSO : ActionEffectSO<IDiscardEffects>
    {
        [SerializeField] private int _cardsToRetrieve = 3;

        protected override void Execute(IDiscardEffects context)
        {
            context.RetrieveFromDiscard(_cardsToRetrieve);
            GameDebug.Log($"Locker's Return - retrieved {_cardsToRetrieve} cards from discard");
        }
    }
}