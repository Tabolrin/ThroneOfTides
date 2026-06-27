// Assets/_Game/2. Scripts/Data/Effects/BloodForBloodEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/BloodForBlood")]
    public class BloodForBloodEffectSO : ActionEffectSO
    {
        public override void Execute(ICardEffectContext context)
        {
            context.AddBloodForBloodCharge();
            Debug.Log("Blood for Blood — reaction charge added");
        }
    }
}