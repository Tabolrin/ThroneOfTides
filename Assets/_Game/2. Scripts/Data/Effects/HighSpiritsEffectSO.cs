// Assets/_Game/2. Scripts/Data/Effects/HighSpiritsEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/HighSpirits")]
    public class HighSpiritsEffectSO : ActionEffectSO
    {
        public override void Execute(ICardEffectContext context)
        {
            context.AddPlayerMaxMana(1);
            context.RegisterHighSpiritsPlayed();
            GameDebug.Log("High Spirits — +1 max mana this match");
        }
    }
}