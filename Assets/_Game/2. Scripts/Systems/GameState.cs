// Assets/_Game/2. Scripts/Systems/GameState.cs
using System;
using System.Collections.Generic;
using System.Linq;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public enum Winner { Player, Enemy, None }

    public class GameState
    {
        public Hand PlayerHand { get; private set; }
        public Hand EnemyHand  { get; private set; }
        public Deck PlayerDeck { get; private set; }
        public Deck EnemyDeck  { get; private set; }

        public int  PlayerHP     { get; private set; }
        public int  EnemyHP      { get; private set; }
        public bool IsPlayerTurn { get; set; }

        public int PlayerMaxHP { get; private set; }
        public int EnemyMaxHP  { get; private set; }

        // ── Mana ──────────────────────────────────────────────────────────────
        public int PlayerMana    { get; private set; }
        public int PlayerMaxMana { get; private set; }
        public int EnemyMana     { get; private set; }
        public int EnemyMaxMana  { get; private set; }

        // ── Combo (per side — gunpowder priming/resolving works independently on each ship) ──
        private readonly ComboState _playerCombo = new ComboState();
        private readonly ComboState _enemyCombo  = new ComboState();

        public int    PlayerComboStackCount => _playerCombo.StackCount;
        public CardSO PlayerActiveComboCard => _playerCombo.ActiveCard;
        public int    EnemyComboStackCount  => _enemyCombo.StackCount;
        public CardSO EnemyActiveComboCard  => _enemyCombo.ActiveCard;

        private class ComboState
        {
            public int    StackCount;
            public CardSO ActiveCard;
        }

        // ── Status Effects ────────────────────────────────────────────────────
        public bool SirenSongActive    { get; private set; }
        public bool PendingUnblockable { get; private set; }
        public int  HighSpiritsPlayCount { get; private set; }

        // ── Turn Tracking ─────────────────────────────────────────────────────
        public bool DamageCardPlayedThisTurn { get; private set; }
        public bool ActionCardPlayedThisTurn { get; private set; }
        public bool HasDrawnThisTurn         { get; private set; }

        // ── Reaction Charges ──────────────────────────────────────────────────
        public int DeadMansTurnCharges  { get; private set; }
        public int BloodForBloodCharges { get; private set; }

        // ── Discard & Snapshot ────────────────────────────────────────────────
        private readonly List<CardSO>    _playerDiscard       = new List<CardSO>();
        private readonly List<CardSO>    _enemyDiscard        = new List<CardSO>();
        private readonly List<DotEffect> _dotEffects          = new List<DotEffect>();
        private readonly List<CardSO>    _originalDeckSnapshot;

        public IReadOnlyList<CardSO> PlayerDiscard        => _playerDiscard.AsReadOnly();
        public IReadOnlyList<CardSO> EnemyDiscard         => _enemyDiscard.AsReadOnly();
        public IReadOnlyList<CardSO> OriginalDeckSnapshot => _originalDeckSnapshot.AsReadOnly();

        public Action OnEnemyTurnReady;

        public GameState(int startingHP, int startingMaxMana,
                         Deck playerDeck, Deck enemyDeck,
                         List<CardSO> originalDeckSnapshot)
        {
            PlayerMaxHP = startingHP;
            EnemyMaxHP  = startingHP;
            PlayerHP    = startingHP;
            EnemyHP     = startingHP;
            PlayerDeck  = playerDeck;
            EnemyDeck   = enemyDeck;
            PlayerHand  = new Hand();
            EnemyHand   = new Hand();

            PlayerMaxMana = startingMaxMana;
            EnemyMaxMana  = startingMaxMana;
            // Both sides start with a full mana pool — previously only the player's mana was
            // implicitly filled (as a side effect of PlayerTurnState.Enter() firing on the very
            // first state-machine transition); the enemy had no equivalent until its first turn.
            PlayerMana = startingMaxMana;
            EnemyMana  = startingMaxMana;

            _originalDeckSnapshot = new List<CardSO>(originalDeckSnapshot);
        }

        // ── HP ────────────────────────────────────────────────────────────────

        public void ApplyDamage(DamageTarget target, int amount)
        {
            if (target == DamageTarget.Player)
                PlayerHP = Mathf.Max(0, PlayerHP - amount);
            else
                EnemyHP  = Mathf.Max(0, EnemyHP  - amount);

            GameEventBus.FireDamageDealt(target, amount);
            GameEventBus.FireHPChanged(target == DamageTarget.Player ? PlayerHP : EnemyHP);
        }

        public void HealPlayer(int amount)
        {
            PlayerHP = Mathf.Min(PlayerHP + amount, PlayerMaxHP);
            GameEventBus.FireHPChanged(PlayerHP);
        }

        // ── Mana ──────────────────────────────────────────────────────────────

        public void ResetPlayerMana()
        {
            PlayerMana = PlayerMaxMana;
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        public void ResetEnemyMana()
        {
            EnemyMana = EnemyMaxMana;
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
        }

        public bool SpendPlayerMana(int amount)
        {
            if (PlayerMana < amount) return false;
            PlayerMana -= amount;
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
            return true;
        }

        public bool SpendEnemyMana(int amount)
        {
            if (EnemyMana < amount) return false;
            EnemyMana -= amount;
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
            return true;
        }

        public void AddPlayerMaxMana(int amount)
        {
            PlayerMaxMana += amount;
            PlayerMana    += amount;
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        // Enemy mana floor is 1 — cannot be fully drained by Stolen Wind
        public void StealEnemyMana(int amount)
        {
            int actual = Mathf.Min(amount, EnemyMana - 1);
            if (actual <= 0) return;
            EnemyMana  -= actual;
            PlayerMana += actual;
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        // ── Reactions ─────────────────────────────────────────────────────────

        public void AddDeadMansTurnCharge()
        {
            DeadMansTurnCharges++;
            GameEventBus.FireReactionCharged(ReactionType.DeadMansTurn, DeadMansTurnCharges);
        }

        public void AddBloodForBloodCharge()
        {
            BloodForBloodCharges++;
            GameEventBus.FireReactionCharged(ReactionType.BloodForBlood, BloodForBloodCharges);
        }

        public bool ConsumeDeadMansTurn()
        {
            if (DeadMansTurnCharges <= 0) return false;
            DeadMansTurnCharges--;
            GameEventBus.FireReactionFired(ReactionType.DeadMansTurn);
            return true;
        }

        public bool ConsumeBloodForBlood()
        {
            if (BloodForBloodCharges <= 0) return false;
            BloodForBloodCharges--;
            GameEventBus.FireReactionFired(ReactionType.BloodForBlood);
            return true;
        }

        public bool HasAnyReaction() =>
            DeadMansTurnCharges > 0 || BloodForBloodCharges > 0;

        // ── Status Effects ────────────────────────────────────────────────────

        public void SetSirenActive(DamageTarget caster = DamageTarget.Player)
        {
            SirenSongActive    = true;
            PendingUnblockable = true;
            GameEventBus.FireShipStatusCountChanged(ShipStatusType.SirenSong, caster, 1);
        }

        public void ClearSiren(DamageTarget caster = DamageTarget.Player)
        {
            SirenSongActive    = false;
            PendingUnblockable = false;
            GameEventBus.FireShipStatusCountChanged(ShipStatusType.SirenSong, caster, 0);
        }

        // High Spirits is a permanent buff — its icon count only ever grows (capped at 3
        // copies per deck) and is never cleared for the rest of the match.
        public void RegisterHighSpiritsPlayed()
        {
            HighSpiritsPlayCount++;
            GameEventBus.FireShipStatusCountChanged(ShipStatusType.HighSpirits, DamageTarget.Player, HighSpiritsPlayCount);
        }

        // ── DOT ───────────────────────────────────────────────────────────────

        public void AddDotEffect(DotEffect effect)
        {
            _dotEffects.Add(effect);
            GameEventBus.FireDOTApplied(effect);

            if (effect.Source != ShipStatusType.None)
                GameEventBus.FireShipStatusCountChanged(effect.Source, effect.Target, effect.TurnsRemaining);
        }

        public void ProcessDotEffects()
        {
            for (int i = _dotEffects.Count - 1; i >= 0; i--)
            {
                DotEffect dot = _dotEffects[i];
                ApplyDamage(dot.Target, dot.DamagePerTurn);
                GameEventBus.FireDOTTick(dot);

                int turnsRemaining = dot.TurnsRemaining - 1;

                if (turnsRemaining <= 0)
                    _dotEffects.RemoveAt(i);
                else
                    _dotEffects[i] = new DotEffect(dot.Target, dot.DamagePerTurn, turnsRemaining, dot.Source);

                if (dot.Source != ShipStatusType.None)
                    GameEventBus.FireShipStatusCountChanged(dot.Source, dot.Target, Mathf.Max(0, turnsRemaining));
            }
        }

        // ── Card Play Validation ──────────────────────────────────────────────

        public bool CanPlayCard(CardSO card)
        {
            // Draw is only mandatory while the deck can still supply one — once it's empty,
            // waiting for a draw that can never happen would softlock the player's turn.
            if (!HasDrawnThisTurn && PlayerDeck.Count > 0) return false;
            if (card.CardType == CardType.Reaction)        return false;
            if (PlayerMana < card.ManaCost)                return false;

            return true;
        }

        public bool CanDraw() =>
            !HasDrawnThisTurn &&
            IsPlayerTurn      &&
            PlayerHand.Count < 5 &&
            PlayerDeck.Count > 0;

        public void RegisterCardPlayed(CardSO card)
        {
            if (card.CardType == CardType.Action)
                ActionCardPlayedThisTurn = true;
            else
                DamageCardPlayedThisTurn = true;
        }

        public void SetHasDrawnThisTurn() => HasDrawnThisTurn = true;

        public void ResetTurnCardPlays()
        {
            DamageCardPlayedThisTurn = false;
            ActionCardPlayedThisTurn = false;
            HasDrawnThisTurn         = false;
        }

        // ── Combo (per side) ──────────────────────────────────────────────────

        private ComboState GetCombo(DamageTarget side) =>
            side == DamageTarget.Player ? _playerCombo : _enemyCombo;

        public void IncrementCombo(DamageTarget side, CardSO card)
        {
            var combo = GetCombo(side);
            combo.ActiveCard = card;
            combo.StackCount++;
            GameEventBus.FireComboStackChanged(side, combo.StackCount);
            GameEventBus.FireShipStatusCountChanged(ShipStatusType.Gunpowder, side, combo.StackCount);
        }

        public int ResolveCombo(DamageTarget side)
        {
            var combo = GetCombo(side);
            int damage = combo.ActiveCard.ComboDamage +
                         ((combo.StackCount - 1) * combo.ActiveCard.ComboStackBonus);
            ResetCombo(side);
            GameEventBus.FireComboResolved();
            return damage;
        }

        public void ResetCombo(DamageTarget side)
        {
            var combo = GetCombo(side);
            combo.StackCount = 0;
            combo.ActiveCard = null;
            GameEventBus.FireComboStackChanged(side, 0);
            GameEventBus.FireShipStatusCountChanged(ShipStatusType.Gunpowder, side, 0);
        }

        // ── Game Over ─────────────────────────────────────────────────────────

        public bool IsGameOver()
        {
            if (PlayerHP <= 0 || EnemyHP <= 0) return true;
            if (PlayerDeck.Count == 0 && PlayerHand.Count == 0) return true;
            if (EnemyDeck.Count  == 0 && EnemyHand.Count  == 0) return true;
            return false;
        }

        public Winner GetWinner()
        {
            if (PlayerHP <= 0 || (PlayerDeck.Count == 0 && PlayerHand.Count == 0)) return Winner.Enemy;
            if (EnemyHP  <= 0 || (EnemyDeck.Count  == 0 && EnemyHand.Count  == 0)) return Winner.Player;
            return Winner.None;
        }

        // ── Discard ───────────────────────────────────────────────────────────

        public void DiscardPlayerCard(CardSO card) => _playerDiscard.Add(card);
        public void DiscardEnemyCard(CardSO card)  => _enemyDiscard.Add(card);

        public List<CardSO> RetrieveFromPlayerDiscard(int count)
        {
            var eligible  = _playerDiscard.Where(c => c.Name != "The Kraken").ToList();
            var retrieved = new List<CardSO>();

            for (int i = 0; i < count && eligible.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, eligible.Count);
                retrieved.Add(eligible[index]);
                _playerDiscard.Remove(eligible[index]);
                eligible.RemoveAt(index);
            }
            return retrieved;
        }

        public List<CardSO> GetRandomFromSnapshot(int count)
        {
            var pool   = new List<CardSO>(_originalDeckSnapshot);
            var result = new List<CardSO>();

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return result;
        }

        // ── Notifications ─────────────────────────────────────────────────────

        public void NotifyEnemyTurnReady()               => OnEnemyTurnReady?.Invoke();
        public void NotifyCardDrawn(CardSO card)         => GameEventBus.FireCardDrawn(card);
        public void NotifyPlayerCardRemoved(CardSO card) => GameEventBus.FirePlayerCardRemoved(card);

        // ── Cheats (playtest only — see UI/CheatsPanel.cs) ────────────────────

        public void CheatAddPlayerHP(int amount)
        {
            PlayerHP = Mathf.Clamp(PlayerHP + amount, 0, PlayerMaxHP);
            GameEventBus.FireHPChanged(PlayerHP);
        }

        public void CheatAddEnemyHP(int amount)
        {
            EnemyHP = Mathf.Clamp(EnemyHP + amount, 0, EnemyMaxHP);
            GameEventBus.FireHPChanged(EnemyHP);
        }

        public void CheatAddPlayerMana(int amount)
        {
            PlayerMana = Mathf.Clamp(PlayerMana + amount, 0, PlayerMaxMana);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        public void CheatAddEnemyMana(int amount)
        {
            EnemyMana = Mathf.Clamp(EnemyMana + amount, 0, EnemyMaxMana);
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
        }

        // Raises (or lowers) the ceiling itself — current value is only pulled down if it would
        // otherwise exceed the new max. Follow with the matching Add cheat to fill it back up.
        public void CheatSetPlayerMaxHP(int amount)
        {
            PlayerMaxHP = Mathf.Max(1, amount);
            PlayerHP    = Mathf.Min(PlayerHP, PlayerMaxHP);
            GameEventBus.FireHPChanged(PlayerHP);
        }

        public void CheatSetEnemyMaxHP(int amount)
        {
            EnemyMaxHP = Mathf.Max(1, amount);
            EnemyHP    = Mathf.Min(EnemyHP, EnemyMaxHP);
            GameEventBus.FireHPChanged(EnemyHP);
        }

        public void CheatSetPlayerMaxMana(int amount)
        {
            PlayerMaxMana = Mathf.Max(0, amount);
            PlayerMana    = Mathf.Min(PlayerMana, PlayerMaxMana);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        public void CheatSetEnemyMaxMana(int amount)
        {
            EnemyMaxMana = Mathf.Max(0, amount);
            EnemyMana    = Mathf.Min(EnemyMana, EnemyMaxMana);
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
        }
    }
}