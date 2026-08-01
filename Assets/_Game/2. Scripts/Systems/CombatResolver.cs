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

        /// <summary>
        /// Resolves a card played by either side. `caster` determines whose combo stack/HP-cost
        /// is affected and whose ship non-targeted weapons/DOT effects hit the opponent of.
        /// `selectedTarget` carries the ship chosen via a targeting prompt, if the card required one.
        /// </summary>
        public int ResolveCard(CardSO card, DamageTarget caster, IHandLayoutManager handLayout, DamageTarget? selectedTarget = null)
        {
            if (card.HPCost > 0)
            {
                _gameState.ApplyDamage(caster, card.HPCost);
                GameDebug.Log($"{card.Name} — paid {card.HPCost} HP");
            }

            switch (card.CardType)
            {
                case CardType.Combo:    return ResolveCombo(card, caster);
                case CardType.DOT:      return ResolveDOT(card, caster);
                case CardType.Action:   return ResolveEffect(card, caster, handLayout, selectedTarget);
                case CardType.Reaction: return ResolveEffect(card, caster, handLayout, selectedTarget);
                case CardType.Weapon:   return ResolveWeapon(card, caster, handLayout, selectedTarget);
                default:                return card.Damage;
            }
        }

        // Player-path convenience wrapper — kept so existing call sites don't need to name the side.
        public int ResolvePlayerCard(CardSO card, IHandLayoutManager handLayout, DamageTarget? selectedTarget = null)
            => ResolveCard(card, DamageTarget.Player, handLayout, selectedTarget);

        public int ResolveCounterGale(int incomingDamage)
        {
            int reflected = Mathf.FloorToInt(incomingDamage * 0.5f);
            GameDebug.Log($"Counter Gale — reflecting {reflected} damage");
            return reflected;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private static DamageTarget Opponent(DamageTarget side) =>
            side == DamageTarget.Player ? DamageTarget.Enemy : DamageTarget.Player;

        // Gunpowder sits on whichever ship is being attacked, not on the caster's own ship —
        // Gunpowder Barrel primes the opponent's ship, and Torch (played by the same attacker)
        // ignites that stack. Matches Tidal Wave's "removes Gunpowder from hit ship" wording.
        private int ResolveCombo(CardSO card, DamageTarget caster)
        {
            DamageTarget target = Opponent(caster);
            var          combo  = _gameState.GetSide(target);

            if (card.ComboStackBonus > 0)
            {
                _gameState.IncrementCombo(target, card);
                GameDebug.Log($"Gunpowder primed on {target}'s ship — stack: {combo.ComboStackCount}");
                return 0;
            }

            if (combo.ComboStackCount > 0 && combo.ActiveComboCard != null)
            {
                int damage = _gameState.ResolveCombo(target);
                GameDebug.Log($"Combo resolved on {target}'s ship — damage: {damage}");
                return damage;
            }

            GameDebug.Log("Torch with no active combo — base damage only");
            return card.Damage;
        }

        private int ResolveDOT(CardSO card, DamageTarget caster)
        {
            DamageTarget target = Opponent(caster);
            _gameState.AddDotEffect(new DotEffect(target, card.DotDamagePerTurn, card.DotDuration, card.StatusType));
            GameDebug.Log($"DOT applied ({target}) — {card.DotDamagePerTurn} dmg × {card.DotDuration} turns");
            return 0;
        }

        private int ResolveEffect(CardSO card, DamageTarget caster, IHandLayoutManager handLayout, DamageTarget? selectedTarget)
        {
            if (card.ActionEffect == null)
            {
                Debug.LogWarning($"{card.Name} has no ActionEffect assigned");
                return 0;
            }
            var context = new CardEffectContext(_gameState, handLayout, caster, _secondaryDrawCallback, selectedTarget);
            card.ActionEffect.Execute(context);
            return 0;
        }

        private int ResolveWeapon(CardSO card, DamageTarget caster, IHandLayoutManager handLayout, DamageTarget? selectedTarget)
        {
            // Weapons with an assigned effect (e.g. Tidal Wave, Chain Shot) fully own their own
            // resolution, including applying their own damage. Weapons with none just deal flat
            // CardSO.Damage — every special case is now expressed as an effect SO, not a switch
            // here (Ram The Hull was cut content with no asset; its case was already a no-op).
            if (card.ActionEffect != null)
            {
                var context = new CardEffectContext(_gameState, handLayout, caster, _secondaryDrawCallback, selectedTarget);
                card.ActionEffect.Execute(context);
                return 0;
            }

            return card.Damage;
        }
    }
}
