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
                Debug.Log($"{card.Name} — paid {card.HPCost} HP");
            }

            switch (card.CardType)
            {
                case CardType.Combo:    return ResolveCombo(card, caster);
                case CardType.DOT:      return ResolveDOT(card, caster);
                case CardType.Action:   return ResolveEffect(card, handLayout, selectedTarget);
                case CardType.Reaction: return ResolveEffect(card, handLayout, selectedTarget);
                case CardType.Weapon:   return ResolveWeapon(card, caster, handLayout, selectedTarget);
                default:                return card.Damage;
            }
        }

        // Player-path convenience wrapper — kept so existing call sites don't need to name the side.
        public int ResolvePlayerCard(CardSO card, IHandLayoutManager handLayout, DamageTarget? selectedTarget = null)
            => ResolveCard(card, DamageTarget.Player, handLayout, selectedTarget);

        public int ResolveBloodForBlood(int incomingDamage)
        {
            int reflected = Mathf.FloorToInt(incomingDamage * 0.5f);
            Debug.Log($"Blood for Blood — reflecting {reflected} damage");
            return reflected;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private static DamageTarget Opponent(DamageTarget side) =>
            side == DamageTarget.Player ? DamageTarget.Enemy : DamageTarget.Player;

        private int ResolveCombo(CardSO card, DamageTarget caster)
        {
            if (card.ComboStackBonus > 0)
            {
                _gameState.IncrementCombo(caster, card);
                int stack = caster == DamageTarget.Player ? _gameState.PlayerComboStackCount : _gameState.EnemyComboStackCount;
                Debug.Log($"Gunpowder primed ({caster}) — stack: {stack}");
                return 0;
            }

            int  activeStack = caster == DamageTarget.Player ? _gameState.PlayerComboStackCount : _gameState.EnemyComboStackCount;
            CardSO activeCard  = caster == DamageTarget.Player ? _gameState.PlayerActiveComboCard  : _gameState.EnemyActiveComboCard;

            if (activeStack > 0 && activeCard != null)
            {
                int damage = _gameState.ResolveCombo(caster);
                Debug.Log($"Combo resolved ({caster}) — damage: {damage}");
                return damage;
            }

            Debug.Log("Torch with no active combo — base damage only");
            return card.Damage;
        }

        private int ResolveDOT(CardSO card, DamageTarget caster)
        {
            DamageTarget target = Opponent(caster);
            _gameState.AddDotEffect(new DotEffect(target, card.DotDamagePerTurn, card.DotDuration, card.StatusType));
            Debug.Log($"DOT applied ({target}) — {card.DotDamagePerTurn} dmg × {card.DotDuration} turns");
            return 0;
        }

        private int ResolveEffect(CardSO card, IHandLayoutManager handLayout, DamageTarget? selectedTarget)
        {
            if (card.ActionEffect == null)
            {
                Debug.LogWarning($"{card.Name} has no ActionEffect assigned");
                return 0;
            }
            var context = new CardEffectContext(_gameState, handLayout, _secondaryDrawCallback, selectedTarget);
            card.ActionEffect.Execute(context);
            return 0;
        }

        private int ResolveWeapon(CardSO card, DamageTarget caster, IHandLayoutManager handLayout, DamageTarget? selectedTarget)
        {
            // Weapons with an assigned effect (e.g. Tidal Wave) fully own their own resolution,
            // including applying their own damage — the legacy name-switch below is only reached
            // by weapons that don't need anything beyond flat damage or a small hardcoded extra.
            if (card.ActionEffect != null)
            {
                var context = new CardEffectContext(_gameState, handLayout, _secondaryDrawCallback, selectedTarget);
                card.ActionEffect.Execute(context);
                return 0;
            }

            switch (card.Name)
            {
                case "Ram the Hull":
                    // HP cost deducted above — ship shake TODO when VFX event defined
                    break;
                case "Chain Shot":
                    DiscardRandomOpponentCard(caster);
                    break;
            }
            return card.Damage;
        }

        private void DiscardRandomOpponentCard(DamageTarget caster)
        {
            bool targetIsEnemy = caster == DamageTarget.Player;
            var hand = targetIsEnemy ? _gameState.EnemyHand.CardsSO : _gameState.PlayerHand.CardsSO;
            if (hand.Count == 0) return;

            int    index = UnityEngine.Random.Range(0, hand.Count);
            CardSO card  = hand[index];

            if (targetIsEnemy)
            {
                _gameState.EnemyHand.RemoveCard(card);
                _gameState.DiscardEnemyCard(card);
            }
            else
            {
                _gameState.PlayerHand.RemoveCard(card);
                _gameState.DiscardPlayerCard(card);
            }

            Debug.Log($"Chain Shot — discarded 1 {(targetIsEnemy ? "enemy" : "player")} card");
        }
    }
}
