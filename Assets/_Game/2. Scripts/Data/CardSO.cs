// Assets/_Game/2. Scripts/Data/CardSO.cs
using System.Collections.Generic;
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
        // HP paid on play — 0 on most cards
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

        [Header("Tags")]
        // CardTagSO stays in Data — not on ICard interface
        [SerializeField] private List<CardTagSO> _tags = new List<CardTagSO>();

        [Header("Visuals")]
        [SerializeField] private Sprite                    _art;
        [SerializeField] private Sprite                    _cardTypeSymbol;
        [SerializeField] private RuntimeAnimatorController _cardArtAnimator;

        // ── ICard ────────────────────────────────────────────────────────────
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

        // ── CardSO-only (not on ICard) ───────────────────────────────────────
        public ActionEffectSO            ActionEffect        => _actionEffect;
        public bool                      IsEligibleAsActionPair => _isEligibleAsActionPair;
        public IReadOnlyList<CardTagSO>  Tags                => _tags.AsReadOnly();
        public Sprite                    Art                 => _art;
        public Sprite                    CardTypeSymbol      => _cardTypeSymbol;
        public RuntimeAnimatorController CardArtAnimator     => _cardArtAnimator;
    }
}