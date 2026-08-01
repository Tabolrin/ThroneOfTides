// Assets/_Game/2. Scripts/Data/Effects/ChainShotEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    // Deals damage to the caster's opponent and discards one random card from that opponent's
    // hand. Needs damage + hand + Caster identity, so — like Tidal Wave — this is a genuinely
    // compound effect and uses the full ICardEffectContext rather than a single narrow interface.
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/ChainShot")]
    public class ChainShotEffectSO : ActionEffectSO
    {
        [Tooltip("Damage dealt to the opponent's ship. Independent of CardSO.Damage — CombatResolver defers entirely to this effect once assigned.")]
        [SerializeField] private int _damage = 4;

        public override void Execute(ICardEffectContext context)
        {
            DamageTarget target = context.Caster == DamageTarget.Player ? DamageTarget.Enemy : DamageTarget.Player;
            context.ApplyDamage(target, _damage);
            context.DiscardRandomFromOpponentHand();
            GameDebug.Log($"Chain Shot — {_damage} dmg, discarded 1 card from opponent's hand");
        }
    }
}
