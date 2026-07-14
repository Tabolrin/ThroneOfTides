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
        [Tooltip("Display name shown on the card and used to look it up in the deck builder/inventory.")]
        [SerializeField] private string _name;

        [Tooltip("Determines how this card is resolved: Weapon deals flat damage, Combo needs a Partner, Action/Reaction run an Effect SO, DOT applies damage over time.")]
        [SerializeField] private CardType _cardType;

        [Tooltip("Flavor/rules text shown on the card and in the inspect view.")]
        [SerializeField] private string _description;

        [Header("Cost")]
        [Tooltip("Mana required to play this card.")]
        [SerializeField] private int _manaCost;

        [Tooltip("Deck slots this card occupies — enforced by the deck builder's storage cap.")]
        [SerializeField] private int _storageCost;

        [Tooltip("HP the caster pays to play this card. Only non-zero on cards that sacrifice HP (e.g. Ram the Hull, Stolen Wind).")]
        [SerializeField] private int _hpCost;

        [Header("Combat")]
        [Tooltip("Flat damage dealt. For Combo cards this is the primer's base damage; for DOT cards this is the initial hit before the damage-over-time ticks.")]
        [SerializeField] private int _damage;

        [Header("Combo")]
        [Tooltip("Bonus damage dealt when this card resolves a combo (played after its Partner primed the stack).")]
        [SerializeField] private int _comboDamage;

        [Tooltip("Extra damage added per additional primer stacked before the combo resolves.")]
        [SerializeField] private int _comboStackBonus;

        [Tooltip("The other card in this combo pairing (e.g. the primer if this is the resolver, or vice versa).")]
        [SerializeField] private CardSO _comboPartner;

        [Header("DOT")]
        [Tooltip("Damage dealt on each subsequent enemy-turn tick after this card is played.")]
        [SerializeField] private int _dotDamagePerTurn;

        [Tooltip("Number of turns the damage-over-time effect lasts.")]
        [SerializeField] private int _dotDuration;

        [Header("Action")]
        [Tooltip("The ScriptableObject that implements this card's gameplay effect. Required for Action and Reaction cards — without it the card does nothing. Optional for Weapon cards: if assigned, CombatResolver runs it instead of the legacy name-switch (e.g. Tidal Wave).")]
        [SerializeField] private ActionEffectSO _actionEffect;

        [Tooltip("Whether this Action card can be played in the same turn as a damage card without using up the turn's action-card allowance twice.")]
        [SerializeField] private bool _isEligibleAsActionPair;

        [Tooltip("If true, playing this card shows a target-selection prompt (your ship / enemy ship) before it resolves. The chosen target is available to the Effect SO via ICardEffectContext.SelectedTarget.")]
        [SerializeField] private bool _requiresTargetSelection;

        [Tooltip("Enemy AI: prefer playing this card before any attack card this turn (e.g. Siren Song, Monkey Grab — utility that's more valuable pre-attack). Also gates whether the enemy AI is allowed to actually execute this card's Action Effect at all — only cards confirmed side-safe for an Enemy caster should be flagged.")]
        [SerializeField] private bool _aiPlayBeforeAttack;

        [Header("Ship Status")]
        [Tooltip("The persistent per-ship status this card's play produces (if any) — drives the world-space indicator sprite and the Active Effects Bar badge. Leave None for cards with no lingering status.")]
        [SerializeField] private ShipStatusType _statusType = ShipStatusType.None;

        [Header("Visuals")]
        [Tooltip("The card artwork shown on its face.")]
        [SerializeField] private Sprite _art;

        [Header("Play Presentation")]
        [Tooltip("What spawns when this card is played — sprite-only effects or paired UI-sprite + world-particle effects, per caster side. See CardPresentationPlayer.")]
        [SerializeField] private List<CardPresentationEntry> _presentationEntries = new List<CardPresentationEntry>();

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
        public ActionEffectSO ActionEffect           => _actionEffect;
        public bool            IsEligibleAsActionPair => _isEligibleAsActionPair;
        public bool             RequiresTargetSelection => _requiresTargetSelection;
        public bool             AiPlayBeforeAttack      => _aiPlayBeforeAttack;
        public ShipStatusType   StatusType              => _statusType;
        public Sprite           Art                    => _art;
        public IReadOnlyList<CardPresentationEntry> PresentationEntries => _presentationEntries;
    }
}
