// Assets/_Game/2. Scripts/Data/CardSO.cs
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

        [Header("Cost")]
        [SerializeField] private int _manaCost;
        [SerializeField] private int _storageCost;
        // Only non-zero on cards that sacrifice HP to play (Ram the Hull, Stolen Wind)
        [SerializeField] private int _hpCost;

        [Header("Combat")]
        [SerializeField] private int _damage;

        [Header("Combo")]
        [SerializeField] private int    _comboDamage;
        [SerializeField] private int    _comboStackBonus;
        [SerializeField] private CardSO _comboPartner;

        [Header("DOT")]
        [SerializeField] private int _dotDamagePerTurn;
        [SerializeField] private int _dotDuration;

        [Header("Action")]
        [SerializeField] private ActionEffectSO _actionEffect;
        [SerializeField] private bool           _isEligibleAsActionPair;

        [Header("Visuals")]
        [SerializeField] private Sprite                    _art;
        // Art animator owned by Eldar — not used in code yet, reserved for future
        [SerializeField] private RuntimeAnimatorController _cardArtAnimator;

        // ── ICard ──────────────────────────────────────────────────────────────
        public string   Name             => _name;
        public CardType CardType         => _cardType;
        public string   Description      => _description;
        public int      Damage           => _damage;
        public int      ComboDamage      => _comboDamage;
        public int      ComboStackBonus  => _comboStackBonus;
        public ICard    ComboPartner     => _comboPartner;
        public int      DotDamagePerTurn => _dotDamagePerTurn;
        public int      DotDuration      => _dotDuration;
        public int      ManaCost         => _manaCost;
        public int      StorageCost      => _storageCost;
        public int      HPCost           => _hpCost;

        // ── CardSO-only ────────────────────────────────────────────────────────
        public ActionEffectSO            ActionEffect           => _actionEffect;
        public bool                      IsEligibleAsActionPair => _isEligibleAsActionPair;
        public Sprite                    Art                    => _art;
        public RuntimeAnimatorController CardArtAnimator        => _cardArtAnimator;
    }
}