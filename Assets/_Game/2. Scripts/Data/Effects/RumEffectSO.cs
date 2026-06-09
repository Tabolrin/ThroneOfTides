// Assets/_Game/2. Scripts/Data/Effects/RumEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/Rum")]
    public class RumEffectSO : ActionEffectSO
    {
        [SerializeField] private int _manaToRestore = 2;

        public override void Execute(ICardEffectContext context)
        {
            context.RestorePlayerMana(_manaToRestore);
            Debug.Log($"Rum — restored {_manaToRestore} mana");
        }
    }
}