using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    // Resolves all combat - damage, combo, DOT, action cards
    // Pure logic - no MonoBehaviour, no UI references
    public class CombatResolver
    {
        private readonly GameState _gameState;

        public CombatResolver(GameState gameState)
        {
            _gameState = gameState;
        }

        public int ResolvePlayerCard(CardSO card, IHandLayoutManager handLayout)
        {
            switch (card.CardType)
            {
                case CardType.Combo:
                    return ResolveCombo(card);

                case CardType.DOT:
                    _gameState.AddDotEffect(new DotEffect(DamageTarget.Enemy, card.DotDamagePerTurn, card.DotDuration));
                    Debug.Log($"DOT applied - {card.DotDamagePerTurn} dmg for {card.DotDuration} turns");
                    return 0;

                case CardType.Action:
                    if (card.ActionEffect != null)
                    {
                        var context = new CardEffectContext(_gameState, handLayout);
                        card.ActionEffect.Execute(context);
                    }
                    else
                        Debug.LogWarning($"Action card {card.Name} has no ActionEffect assigned");
                    return 0;

                case CardType.Weapon:
                    return ResolveWeapon(card);

                default:
                    return card.Damage;
            }
        }

        private int ResolveCombo(CardSO card)
        {
            // ComboStackBonus > 0 = initiator (Gunpowder)
            if (card.ComboStackBonus > 0)
            {
                _gameState.IncrementCombo(card);
                Debug.Log($"Gunpowder primed - stack: {_gameState.ComboStackCount}");
                return 0;
            }
            // Torch - resolve if combo active
            if (_gameState.ComboStackCount > 0 && _gameState.ActiveComboCard != null)
            {
                int comboDamage = _gameState.ResolveCombo();
                Debug.Log($"Combo resolved - damage: {comboDamage}");
                return comboDamage;
            }
            Debug.Log("Torch with no active Gunpowder - base damage only");
            return card.Damage;
        }

        private int ResolveWeapon(CardSO card)
        {
            // Boarding Party - player sacrifices 2 HP on play
            if (card.Name == "Boarding Party")
            {
                _gameState.ApplyDamage(DamageTarget.Player, 2);
                Debug.Log("Boarding Party - sacrificed 2 HP");
            }
            // Tidal Wave - breaks active combo
            // TODO - add target selection UI for self-damage option
            if (card.Name == "Tidal Wave" && _gameState.ComboStackCount > 0)
            {
                _gameState.ResetCombo();
                Debug.Log("Tidal Wave - combo broken");
            }
            return card.Damage;
        }
    }
}