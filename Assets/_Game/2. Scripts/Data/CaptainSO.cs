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
        [SerializeField] private int              _hp;
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
        [SerializeField] private float _weightKraken;
        [SerializeField] private float _weightBoardingParty;
        [SerializeField] private float _weightDOT;

        public string         CaptainName          => _captainName;
        public Sprite         Portrait             => _portrait;
        public string         ArchetypeDescription => _archetypeDescription;
        public int            HP                   => _hp;
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
        public float WeightKraken             => _weightKraken;
        public float WeightBoardingParty      => _weightBoardingParty;
        public float WeightDOT                => _weightDOT;

        // Convenience method for AI system - returns weight for a given card
        public float GetWeightForCard(CardSO card)
        {
            if (card == null) return 0f;

            return card.CardType switch
            {
                CardType.Combo   => card.Name.Contains("Torch") ? _weightComboFollowUp : _weightComboInitiator,
                CardType.DOT     => _weightDOT,
                CardType.Action  => GetActionWeight(card),
                CardType.Weapon  => GetWeaponWeight(card),
                _                => 1f
            };
        }

        private float GetWeaponWeight(CardSO card)
        {
            if (card.Name == "The Kraken")      return _weightKraken;
            if (card.Name == "Boarding Party")  return _weightBoardingParty;
            if (card.Damage >= 5)               return _weightHighDamageWeapon;
            return card.Damage <= 2             ? _weightLowDamageWeapon : 1f;
        }

        private float GetActionWeight(CardSO card) => card.Name switch
        {
            "Recon Parrot"   => _weightActionIntel,
            "Siren Song"     => _weightActionDisrupt,
            "Treasure Chest" => _weightActionDraw,
            "High Spirits"   => _weightActionHeal,
            "Dead Man's Turn"=> _weightActionDefense,
            _                => 1f
        };
    }
}