using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/SirenSong")]
    public class SirenSongEffectSO : ActionEffectSO
    {
        public override void Execute(ICardEffectContext context)
        {
            // Must be played before the damage card this turn
            // Cleared if no attack card is played before turn ends
            context.SetSirenActive();
        }
    }
}