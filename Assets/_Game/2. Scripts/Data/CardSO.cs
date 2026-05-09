using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/Card")]
    public class CardSO : ScriptableObject, ICard
    {
        [Header("Identity")]
        [SerializeField] private string   _name;
        [SerializeField] private CardType _cardType;
        [SerializeField] private string   _description;

        [Header("Combat")]
        [SerializeField] private int _damage;

        [Header("Combo")]
        // Only relevant when CardType is Combo
        [SerializeField] private int    _comboDamage;
        [SerializeField] private int    _comboStackBonus;
        [SerializeField] private CardSO _comboPartner;

        [Header("DOT")]
        // Only relevant when CardType is DOT
        [SerializeField] private int _dotDamagePerTurn;
        [SerializeField] private int _dotDuration;

        [Header("Action")]
        // Null for non-action cards
        [SerializeField] private ActionEffectSO _actionEffect;

        [Header("Visuals")]
        [SerializeField] private Sprite                   _art;
        [SerializeField] private Sprite                   _cardTypeSymbol;
        // Populated by Eldar - unused in prototype
        [SerializeField] private RuntimeAnimatorController _cardArtAnimator;

        // ICard implementation
        public string   Name             => _name;
        public CardType CardType         => _cardType;
        public string   Description      => _description;
        public int      Damage           => _damage;
        public int      ComboDamage      => _comboDamage;
        public int      ComboStackBonus  => _comboStackBonus;
        public ICard    ComboPartner     => _comboPartner;
        public int      DotDamagePerTurn => _dotDamagePerTurn;
        public int      DotDuration      => _dotDuration;

        // CardSO-only - not on ICard, UI accesses these directly
        public ActionEffectSO            ActionEffect     => _actionEffect;
        public Sprite                    Art              => _art;
        public Sprite                    CardTypeSymbol   => _cardTypeSymbol;
        public RuntimeAnimatorController CardArtAnimator  => _cardArtAnimator;
        
        // Whether this card can be paired with a damage card in the same turn
        [SerializeField] private bool _isEligibleAsActionPair;
        public bool IsEligibleAsActionPair => _isEligibleAsActionPair;
    }
}