// Assets/_Game/2. Scripts/Systems/CombatResolver.cs
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class CombatResolver
    {
        private readonly GameState _gameState;

        public CombatResolver(GameState gameState)
        {
            _gameState = gameState;
        }

        public int ResolvePlayerCard(CardSO card, IHandLayoutManager handLayout)
        {
            // Pay HP cost before resolving — applies to Ram the Hull, Stolen Wind
            if (card.HPCost > 0)
            {
                _gameState.ApplyDamage(DamageTarget.Player, card.HPCost);
                Debug.Log($"{card.Name} — paid {card.HPCost} HP cost");
            }

            switch (card.CardType)
            {
                case CardType.Combo:    return ResolveCombo(card);
                case CardType.DOT:      return ResolveDOT(card);
                case CardType.Action:   return ResolveAction(card, handLayout);
                case CardType.Reaction: return ResolveReaction(card, handLayout);
                case CardType.Weapon:   return ResolveWeapon(card);
                default:                return card.Damage;
            }
        }

        // Resolves Blood for Blood damage reflection — called by TurnCoordinator
        // during enemy attack resolution, not via the normal card play path.
        public int ResolveBloodForBlood(int incomingDamage)
        {
            int reflectedDamage = Mathf.FloorToInt(incomingDamage * 0.5f);
            Debug.Log($"Blood for Blood — reflecting {reflectedDamage} damage");
            return reflectedDamage;
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
                int comboDamage = _gameState.ResolveCombo();
                Debug.Log($"Combo resolved — damage: {comboDamage}");
                return comboDamage;
            }
            Debug.Log("Torch with no active Gunpowder — base damage only");
            return card.Damage;
        }

        private int ResolveDOT(CardSO card)
        {
            _gameState.AddDotEffect(
                new DotEffect(DamageTarget.Enemy, card.DotDamagePerTurn, card.DotDuration));
            Debug.Log($"DOT applied — {card.DotDamagePerTurn} dmg for {card.DotDuration} turns");
            return 0;
        }

        private int ResolveAction(CardSO card, IHandLayoutManager handLayout)
        {
            if (card.ActionEffect != null)
            {
                var context = new CardEffectContext(_gameState, handLayout);
                card.ActionEffect.Execute(context);
            }
            else
                Debug.LogWarning($"Action card {card.Name} has no ActionEffect assigned");
            return 0;
        }

        private int ResolveReaction(CardSO card, IHandLayoutManager handLayout)
        {
            // Reaction cards route through ActionEffect same as Action cards.
            // The effect SO adds a charge to GameState rather than having an immediate effect.
            if (card.ActionEffect != null)
            {
                var context = new CardEffectContext(_gameState, handLayout);
                card.ActionEffect.Execute(context);
            }
            else
                Debug.LogWarning($"Reaction card {card.Name} has no ActionEffect assigned");
            return 0;
        }

        private int ResolveWeapon(CardSO card)
        {
            switch (card.Name)
            {
                case "Ram the Hull":
                    // HP cost already paid above — just log the shake TODO
                    // TODO: fire ship shake VFX event when VFX system is wired
                    Debug.Log("Ram the Hull — both ships shake on resolution");
                    break;

                case "Chain Shot":
                    // Discard 1 random card from enemy hand — player cannot see which
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
            Debug.Log("Chain Shot — discarded 1 enemy card (hidden from player)");
        }
    }
}