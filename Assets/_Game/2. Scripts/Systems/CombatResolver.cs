// Assets/_Game/2. Scripts/Systems/CombatResolver.cs
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class CombatResolver
    {
        private readonly GameState _gameState;

        // Set by TurnCoordinator before each card resolution.
        // Allows effect SOs to trigger secondary draws without assembly boundary issues.
        private System.Func<bool> _secondaryDrawCallback;

        public void SetSecondaryDrawCallback(System.Func<bool> callback)
            => _secondaryDrawCallback = callback;

        public CombatResolver(GameState gameState)
        {
            _gameState = gameState;
        }

        public int ResolvePlayerCard(CardSO card, IHandLayoutManager handLayout)
        {
            if (card.HPCost > 0)
            {
                _gameState.ApplyDamage(DamageTarget.Player, card.HPCost);
                Debug.Log($"{card.Name} — paid {card.HPCost} HP");
            }

            switch (card.CardType)
            {
                case CardType.Combo:    return ResolveCombo(card);
                case CardType.DOT:      return ResolveDOT(card);
                case CardType.Action:   return ResolveEffect(card, handLayout);
                case CardType.Reaction: return ResolveEffect(card, handLayout);
                case CardType.Weapon:   return ResolveWeapon(card);
                default:                return card.Damage;
            }
        }

        public int ResolveBloodForBlood(int incomingDamage)
        {
            int reflected = Mathf.FloorToInt(incomingDamage * 0.5f);
            Debug.Log($"Blood for Blood — reflecting {reflected} damage");
            return reflected;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private int ResolveCombo(CardSO card)
        {
            if (card.ComboStackBonus > 0)
            {
                _gameState.IncrementCombo(card);
                Debug.Log($"Gunpowder primed — stack: {_gameState.ComboStackCount}");
                return 0;
            }
            if (_gameState.ComboStackCount > 0 && _gameState.ActiveComboCard != null)
            {
                int damage = _gameState.ResolveCombo();
                Debug.Log($"Combo resolved — damage: {damage}");
                return damage;
            }
            Debug.Log("Torch with no active combo — base damage only");
            return card.Damage;
        }

        private int ResolveDOT(CardSO card)
        {
            _gameState.AddDotEffect(
                new DotEffect(DamageTarget.Enemy, card.DotDamagePerTurn, card.DotDuration));
            Debug.Log($"DOT applied — {card.DotDamagePerTurn} dmg × {card.DotDuration} turns");
            return 0;
        }

        private int ResolveEffect(CardSO card, IHandLayoutManager handLayout)
        {
            if (card.ActionEffect == null)
            {
                Debug.LogWarning($"{card.Name} has no ActionEffect assigned");
                return 0;
            }
            var context = new CardEffectContext(_gameState, handLayout, _secondaryDrawCallback);
            card.ActionEffect.Execute(context);
            return 0;
        }

        private int ResolveWeapon(CardSO card)
        {
            switch (card.Name)
            {
                case "Ram the Hull":
                    // HP cost deducted above — ship shake TODO when VFX event defined
                    break;
                case "Chain Shot":
                    DiscardRandomEnemyCard();
                    break;
                case "Tidal Wave":
                    if (_gameState.ComboStackCount > 0)
                    {
                        _gameState.ResetCombo();
                        Debug.Log("Tidal Wave — enemy combo broken");
                    }
                    break;
            }
            return card.Damage;
        }

        private void DiscardRandomEnemyCard()
        {
            var hand = _gameState.EnemyHand.CardsSO;
            if (hand.Count == 0) return;

            int    index = UnityEngine.Random.Range(0, hand.Count);
            CardSO card  = hand[index];
            _gameState.EnemyHand.RemoveCard(card);
            _gameState.DiscardEnemyCard(card);
            Debug.Log("Chain Shot — discarded 1 enemy card");
        }
    }
}