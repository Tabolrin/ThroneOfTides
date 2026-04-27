using UnityEngine;

namespace ThroneOfTides.Data
{
    public enum CardType { Weapon, /*Action,*/ Combo} 
    
    [CreateAssetMenu(fileName = "CardSO", menuName = "ThroneOfTides/Data/CardSO")]
    public class CardSO : ScriptableObject
    {
        [SerializeField] public string Name;
        [SerializeField] public int Damage;
        [SerializeField] public CardType Type;
        [SerializeField] public CardSO ComboPartner;
        [SerializeField] public int ComboDamage;
        [SerializeField] public int ComboStackBonus ;
        [SerializeField] public string Description;
        [SerializeField] public Sprite Art;
    }
}
