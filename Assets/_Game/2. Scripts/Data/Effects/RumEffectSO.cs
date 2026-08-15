// Assets/_Game/2. Scripts/Data/Effects/RumEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/Rum")]
    public class RumEffectSO : ActionEffectSO<IHealEffects>
    {
        [SerializeField] private int _healAmount = 5;

        protected override void Execute(IHealEffects context)
        {
            context.HealPlayer(_healAmount);
            GameDebug.Log($"Rum - healed {_healAmount} HP");
        }
    }
}