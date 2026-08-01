// Assets/_Game/2. Scripts/Data/Effects/TidalWaveEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    // Deals damage to the player-chosen target ship and clears all Gunpowder stacks there.
    // Requires the card's Requires Target Selection flag so ICardEffectContext.SelectedTarget
    // is populated before Execute runs — see CombatResolver.ResolveWeapon's effect bridge.
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/TidalWave")]
    public class TidalWaveEffectSO : ActionEffectSO
    {
        [Tooltip("Damage dealt to the chosen target ship. Independent of CardSO.Damage — CombatResolver defers entirely to this effect once assigned.")]
        [SerializeField] private int _damage = 3;

        public override void Execute(ICardEffectContext context)
        {
            if (!context.SelectedTarget.HasValue)
            {
                Debug.LogError("Tidal Wave executed with no SelectedTarget — the card must have Requires Target Selection enabled.");
                return;
            }

            DamageTarget target = context.SelectedTarget.Value;
            context.ApplyDamage(target, _damage);
            context.ClearComboStack(target);
            GameDebug.Log($"Tidal Wave — {_damage} dmg to {target}, gunpowder cleared");
        }
    }
}
