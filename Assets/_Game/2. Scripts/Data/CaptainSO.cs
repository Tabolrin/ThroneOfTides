using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/Captain")]
    public class CaptainSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _captainName;
        [SerializeField] private Sprite _portrait;
        [SerializeField] private string _archetypeDescription;

        [Header("Combat")]
        [Tooltip("0 = fall back to GameConfigSO.StartingHP.")]
        [SerializeField] private int              _hp;
        [Tooltip("0 = fall back to GameConfigSO.StartingMaxMana.")]
        [SerializeField] private int              _maxMana;
        [SerializeField] private DeckDefinitionSO _deckDefinition;

        [Header("Rewards")]
        [SerializeField] private LevelRewardSO _levelReward;

        [Header("AI Weights")]
        // Scale: 0 = never, 0.5 = low, 1.0 = neutral, 1.5 = preferred, 2.0 = strongly preferred
        [SerializeField] private float _weightHighDamageWeapon;
        [SerializeField] private float _weightLowDamageWeapon;
        [SerializeField] private float _weightBlockHoldForDefense;
        [SerializeField] private float _weightComboInitiator;
        [SerializeField] private float _weightComboFollowUp;
        [SerializeField] private float _weightActionIntel;
        [SerializeField] private float _weightActionDisrupt;
        [SerializeField] private float _weightActionDraw;
        [SerializeField] private float _weightActionHeal;
        [SerializeField] private float _weightActionDefense;
        [SerializeField] private float _weightActionManaSteal;
        [SerializeField] private float _weightKraken;
        [SerializeField] private float _weightBoardingParty;
        [SerializeField] private float _weightDOT;

        [Header("Reaction Weights")]
        // WeightActionDefense above is reused for Dead Man's Turn preference (negate).
        [SerializeField] private float _weightReactionCounter;

        public string         CaptainName          => _captainName;
        public Sprite         Portrait             => _portrait;
        public string         ArchetypeDescription => _archetypeDescription;
        public int            HP                   => _hp;
        public int            MaxMana              => _maxMana;
        public DeckDefinitionSO DeckDefinition     => _deckDefinition;
        public LevelRewardSO  LevelReward          => _levelReward;

        public float WeightHighDamageWeapon   => _weightHighDamageWeapon;
        public float WeightLowDamageWeapon    => _weightLowDamageWeapon;
        public float WeightBlockHoldForDefense=> _weightBlockHoldForDefense;
        public float WeightComboInitiator     => _weightComboInitiator;
        public float WeightComboFollowUp      => _weightComboFollowUp;
        public float WeightActionIntel        => _weightActionIntel;
        public float WeightActionDisrupt      => _weightActionDisrupt;
        public float WeightActionDraw         => _weightActionDraw;
        public float WeightActionHeal         => _weightActionHeal;
        public float WeightActionDefense      => _weightActionDefense;
        public float WeightActionManaSteal    => _weightActionManaSteal;
        public float WeightKraken             => _weightKraken;
        public float WeightBoardingParty      => _weightBoardingParty;
        public float WeightDOT                => _weightDOT;
        public float WeightReactionCounter    => _weightReactionCounter;

        // Convenience method for AI system - returns weight for a given card
        public float GetWeightForCard(CardSO card)
        {
            if (card == null) return 0f;

            return card.CardType switch
            {
                CardType.Combo    => card.Id == CardId.Torch ? _weightComboFollowUp : _weightComboInitiator,
                CardType.DOT      => _weightDOT,
                CardType.Action   => GetActionWeight(card),
                CardType.Weapon   => GetWeaponWeight(card),
                CardType.Reaction => GetReactionWeight(card),
                _                 => 1f
            };
        }

        private float GetWeaponWeight(CardSO card)
        {
            if (card.Id == CardId.Kraken)        return _weightKraken;
            if (card.Id == CardId.BoardingParty) return _weightBoardingParty;
            if (card.Damage >= 5)                return _weightHighDamageWeapon;
            return card.Damage <= 2              ? _weightLowDamageWeapon : 1f;
        }

        // Weighted by the effect's declared Role (see EffectRole/ActionEffectSO) rather than by
        // CardId — a new Action card just needs its effect SO to declare a Role, no edit here.
        private float GetActionWeight(CardSO card)
        {
            EffectRole role = card.ActionEffect != null ? card.ActionEffect.Role : EffectRole.None;
            return role switch
            {
                EffectRole.Intel     => _weightActionIntel,
                EffectRole.Disrupt   => _weightActionDisrupt,
                EffectRole.Draw      => _weightActionDraw,
                EffectRole.Heal      => _weightActionHeal,
                EffectRole.Defense   => _weightActionDefense,
                EffectRole.ManaSteal => _weightActionManaSteal,
                _                    => 1f
            };
        }

        // Dead Man's Turn reuses WeightActionDefense (same "hold the line" preference as an
        // Action-card defensive play); Counter Gale gets its own weight since reflecting damage
        // is a different playstyle choice than negating it outright.
        private float GetReactionWeight(CardSO card) => card.Id switch
        {
            CardId.DeadMansTurn => _weightActionDefense,
            CardId.CounterGale  => _weightReactionCounter,
            _                   => 1f
        };

        // Called when the enemy is about to take damage and has at least one reaction charged.
        // Each available reaction has weight/2 chance to be used; if both would fire, the
        // higher-weighted one wins. Returns null if the captain chooses not to react at all.
        public ReactionType? ChooseReaction(bool hasDeadMansTurn, bool hasCounterGale)
        {
            bool useDeadMansTurn = hasDeadMansTurn && Random.value < (_weightActionDefense / 2f);
            bool useCounterGale  = hasCounterGale  && Random.value < (_weightReactionCounter / 2f);

            if (useDeadMansTurn && useCounterGale)
                return _weightActionDefense >= _weightReactionCounter
                    ? ReactionType.DeadMansTurn
                    : ReactionType.CounterGale;

            if (useDeadMansTurn) return ReactionType.DeadMansTurn;
            if (useCounterGale)  return ReactionType.CounterGale;
            return null;
        }
    }
}