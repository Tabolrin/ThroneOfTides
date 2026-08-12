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

        // Per-side HP/Mana/Combo state — GameState forwards its Player*/Enemy* API to these
        // instead of duplicating the underlying math (see ShipState.cs).
        public ShipState Player { get; private set; }
        public ShipState Enemy  { get; private set; }

        public ShipState GetSide(DamageTarget target) => target == DamageTarget.Player ? Player : Enemy;

        public int  PlayerHP     => Player.HP;
        public int  EnemyHP      => Enemy.HP;
        public bool IsPlayerTurn { get; set; }

        public int PlayerMaxHP => Player.MaxHP;
        public int EnemyMaxHP  => Enemy.MaxHP;

        // ── Mana ──────────────────────────────────────────────────────────────
        public int PlayerMana    => Player.Mana;
        public int PlayerMaxMana => Player.MaxMana;
        public int EnemyMana     => Enemy.Mana;
        public int EnemyMaxMana  => Enemy.MaxMana;

        // Extra mana cost tacked onto the very next card the player plays (e.g. Dead Man's Turn's
        // cost) — applied once, then cleared, regardless of what that next card turns out to be.
        public int PlayerNextCardManaSurcharge { get; private set; }
        public void AddPlayerNextCardManaSurcharge(int amount) => PlayerNextCardManaSurcharge += amount;
        public void ClearPlayerNextCardManaSurcharge() => PlayerNextCardManaSurcharge = 0;

        // ── Combo (per side — gunpowder priming/resolving works independently on each ship) ──
        public int    PlayerComboStackCount => Player.ComboStackCount;
        public CardSO PlayerActiveComboCard => Player.ActiveComboCard;
        public int    EnemyComboStackCount  => Enemy.ComboStackCount;
        public CardSO EnemyActiveComboCard  => Enemy.ActiveComboCard;

        // ── Status Effects ────────────────────────────────────────────────────
        public bool SirenSongActive    { get; private set; }
        public bool PendingUnblockable { get; private set; }
        public int  HighSpiritsPlayCount { get; private set; }

        // ── Turn Tracking ─────────────────────────────────────────────────────
        public bool DamageCardPlayedThisTurn { get; private set; }
        public bool ActionCardPlayedThisTurn { get; private set; }
        public bool HasDrawnThisTurn         { get; private set; }

        // ── Reaction Charges (per side) ────────────────────────────────────────
        public int PlayerDeadMansTurnCharges => Player.DeadMansTurnCharges;
        public int EnemyDeadMansTurnCharges  => Enemy.DeadMansTurnCharges;
        public int PlayerCounterGaleCharges  => Player.CounterGaleCharges;
        public int EnemyCounterGaleCharges   => Enemy.CounterGaleCharges;

        // ── Discard & Snapshot ────────────────────────────────────────────────
        private readonly List<CardSO>    _playerDiscard       = new List<CardSO>();
        private readonly List<CardSO>    _enemyDiscard        = new List<CardSO>();
        private readonly List<DotEffect> _dotEffects          = new List<DotEffect>();
        private readonly List<CardSO>    _originalDeckSnapshot;

        public IReadOnlyList<CardSO> PlayerDiscard        => _playerDiscard.AsReadOnly();
        public IReadOnlyList<CardSO> EnemyDiscard         => _enemyDiscard.AsReadOnly();
        public IReadOnlyList<CardSO> OriginalDeckSnapshot => _originalDeckSnapshot.AsReadOnly();

        public Action OnEnemyTurnReady;

        public int MaxHandSize { get; private set; }

        public GameState(int startingHP, int startingMaxMana, int maxHandSize,
                         Deck playerDeck, Deck enemyDeck,
                         List<CardSO> originalDeckSnapshot,
                         int? enemyStartingHP = null, int? enemyStartingMaxMana = null)
        {
            // Both sides start with a full mana pool — previously only the player's mana was
            // implicitly filled (as a side effect of PlayerTurnState.Enter() firing on the very
            // first state-machine transition); the enemy had no equivalent until its first turn.
            // enemyStartingHP/enemyStartingMaxMana let a Captain be tougher (or weaker) than the
            // player's own base stats — null falls back to the shared starting values.
            Player = new ShipState(startingHP, startingMaxMana);
            Enemy  = new ShipState(enemyStartingHP ?? startingHP, enemyStartingMaxMana ?? startingMaxMana);

            MaxHandSize = maxHandSize;
            PlayerDeck  = playerDeck;
            EnemyDeck   = enemyDeck;
            PlayerHand  = new Hand();
            EnemyHand   = new Hand();

            _originalDeckSnapshot = new List<CardSO>(originalDeckSnapshot);
        }

        // ── HP ────────────────────────────────────────────────────────────────

        public void ApplyDamage(DamageTarget target, int amount)
        {
            GetSide(target).ApplyDamage(amount);
            GameEventBus.FireDamageDealt(target, amount);
            GameEventBus.FireHPChanged(target == DamageTarget.Player ? PlayerHP : EnemyHP);
        }

        public void HealPlayer(int amount)
        {
            Player.Heal(amount);
            GameEventBus.FireHealApplied(DamageTarget.Player, amount);
            GameEventBus.FireHPChanged(PlayerHP);
        }

        // Mirror of HealPlayer for when the Enemy is the one casting the heal (e.g. Rum).
        public void HealEnemy(int amount)
        {
            Enemy.Heal(amount);
            GameEventBus.FireHealApplied(DamageTarget.Enemy, amount);
            GameEventBus.FireHPChanged(EnemyHP);
        }

        // ── Mana ──────────────────────────────────────────────────────────────

        public void ResetPlayerMana()
        {
            Player.ResetMana();
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        public void ResetEnemyMana()
        {
            Enemy.ResetMana();
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
        }

        public bool SpendPlayerMana(int amount)
        {
            if (!Player.SpendMana(amount)) return false;
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
            return true;
        }

        public bool SpendEnemyMana(int amount)
        {
            if (!Enemy.SpendMana(amount)) return false;
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
            return true;
        }

        public void AddPlayerMaxMana(int amount)
        {
            Player.AddMaxMana(amount);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        // Refunds current mana (e.g. Counter Gale) without raising PlayerMaxMana.
        public void RefundPlayerMana(int amount)
        {
            Player.RefundMana(amount);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        public void AddEnemyMaxMana(int amount)
        {
            Enemy.AddMaxMana(amount);
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
        }

        // Enemy mana floor is 1 — cannot be fully drained by Stolen Wind/Essence Plunder
        public void StealEnemyMana(int amount)
        {
            int actual = Enemy.TransferManaTo(Player, amount);
            if (actual <= 0) return;
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        // Mirror of StealEnemyMana for when the Enemy is the one casting the steal — Player
        // mana floor is 1, same rule reversed.
        public void StealPlayerMana(int amount)
        {
            int actual = Player.TransferManaTo(Enemy, amount);
            if (actual <= 0) return;
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
        }

        // ── Reactions ─────────────────────────────────────────────────────────

        public void AddDeadMansTurnCharge(DamageTarget side = DamageTarget.Player)
        {
            var ship = GetSide(side);
            ship.AddDeadMansTurnCharge();
            GameEventBus.FireReactionCharged(ReactionType.DeadMansTurn, side, ship.DeadMansTurnCharges);
        }

        public void AddCounterGaleCharge(DamageTarget side = DamageTarget.Player)
        {
            var ship = GetSide(side);
            ship.AddCounterGaleCharge();
            GameEventBus.FireReactionCharged(ReactionType.CounterGale, side, ship.CounterGaleCharges);
        }

        public bool ConsumeDeadMansTurn(DamageTarget side = DamageTarget.Player)
        {
            if (!GetSide(side).ConsumeDeadMansTurn()) return false;
            GameEventBus.FireReactionFired(ReactionType.DeadMansTurn, side);
            return true;
        }

        public bool ConsumeCounterGale(DamageTarget side = DamageTarget.Player)
        {
            if (!GetSide(side).ConsumeCounterGale()) return false;
            GameEventBus.FireReactionFired(ReactionType.CounterGale, side);
            return true;
        }

        public bool HasAnyReaction(DamageTarget side = DamageTarget.Player) => GetSide(side).HasAnyReaction();

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
            if (PlayerMana < card.ManaCost + PlayerNextCardManaSurcharge) return false;

            return true;
        }

        public bool CanDraw() =>
            !HasDrawnThisTurn &&
            IsPlayerTurn      &&
            PlayerHand.Count < MaxHandSize &&
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

        public void IncrementCombo(DamageTarget side, CardSO card)
        {
            var combo = GetSide(side);
            combo.IncrementCombo(card);
            GameEventBus.FireComboStackChanged(side, combo.ComboStackCount);
            GameEventBus.FireShipStatusCountChanged(ShipStatusType.Gunpowder, side, combo.ComboStackCount);
        }

        public int ResolveCombo(DamageTarget side)
        {
            var combo = GetSide(side);
            int damage = combo.ActiveComboCard.ComboDamage +
                         ((combo.ComboStackCount - 1) * combo.ActiveComboCard.ComboStackBonus);
            ResetCombo(side);
            GameEventBus.FireComboResolved();
            return damage;
        }

        public void ResetCombo(DamageTarget side)
        {
            GetSide(side).ResetCombo();
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
            var eligible  = _playerDiscard.Where(c => c.Id != CardId.Kraken).ToList();
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
            Player.CheatAddHP(amount);
            GameEventBus.FireHPChanged(PlayerHP);
        }

        public void CheatAddEnemyHP(int amount)
        {
            Enemy.CheatAddHP(amount);
            GameEventBus.FireHPChanged(EnemyHP);
        }

        public void CheatAddPlayerMana(int amount)
        {
            Player.CheatAddMana(amount);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        public void CheatAddEnemyMana(int amount)
        {
            Enemy.CheatAddMana(amount);
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
        }

        // Raises (or lowers) the ceiling itself — current value is only pulled down if it would
        // otherwise exceed the new max. Follow with the matching Add cheat to fill it back up.
        public void CheatSetPlayerMaxHP(int amount)
        {
            Player.SetMaxHP(amount);
            GameEventBus.FireHPChanged(PlayerHP);
        }

        public void CheatSetEnemyMaxHP(int amount)
        {
            Enemy.SetMaxHP(amount);
            GameEventBus.FireHPChanged(EnemyHP);
        }

        public void CheatSetPlayerMaxMana(int amount)
        {
            Player.SetMaxMana(amount);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        public void CheatSetEnemyMaxMana(int amount)
        {
            Enemy.SetMaxMana(amount);
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
        }
    }
}