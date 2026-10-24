using System.Collections.Generic;
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    // Uses the full context (not just IHandEffects) because it needs to know who cast it.
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/ReconParrot")]
    public class ReconParrotEffectSO : ActionEffectSO
    {
        [SerializeField] private int _cardsToReveal = 3;

        public override void Execute(ICardEffectContext context)
        {
            // The reveal is information for the human player - it opens a panel showing the
            // opponent's cards. When the enemy casts Recon Parrot there's nothing to show (the AI
            // already reads game state directly), and firing the reveal would pop that panel up
            // with the player's own hand in it. The parrot's flight still plays either way.
            if (context.Caster != DamageTarget.Player) return;

            var opponentHand = context.GetOpponentHand();
            int revealCount  = Mathf.Min(_cardsToReveal, opponentHand.Count);

            var revealed = new List<ICard>(revealCount);
            for (int i = 0; i < revealCount; i++)
                revealed.Add(opponentHand[i]);

            GameEventBus.FireEnemyHandRevealed(revealed);
        }
    }
}
