using System.Collections.Generic;
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/ReconParrot")]
    public class ReconParrotEffectSO : ActionEffectSO<IHandEffects>
    {
        [SerializeField] private int _cardsToReveal = 3;

        protected override void Execute(IHandEffects context)
        {
            var opponentHand = context.GetOpponentHand();
            int revealCount  = Mathf.Min(_cardsToReveal, opponentHand.Count);

            var revealed = new List<ICard>(revealCount);
            for (int i = 0; i < revealCount; i++)
                revealed.Add(opponentHand[i]);

            GameEventBus.FireEnemyHandRevealed(revealed);
        }
    }
}