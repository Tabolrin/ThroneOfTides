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

        private readonly int _maxHP;

        // ── Mana ─────────────────────────────────────────────────────────────
        public int PlayerMana    { get; private set; }
        public int PlayerMaxMana { get; private set; }
        public int EnemyMana     { get; private set; }
        public int EnemyMaxMana  { get; private set; }

        // ── Combo ─────────────────────────────────────────────────────────────
        public int    ComboStackCount { get; private set; }
        public CardSO ActiveComboCard { get; private set; }

        // ── Status Effects ────────────────────────────────────────────────────
        public bool SirenSongActive    { get; private set; }
        public bool PendingUnblockable { get; private set; }

        // ── Turn Tracking ─────────────────────────────────────────────────────
        public bool DamageCardPlayedThisTurn { get; private set; }
        public bool ActionCardPlayedThisTurn { get; private set; }
        public bool HasDrawnThisTurn         { get; private set; }

        // ── Reaction Charges ──────────────────────────────────────────────────
        public int DeadMansTurnCharges  { get; private set; }
        public int BloodForBloodCharges { get; private set; }

        // ── Discard ───────────────────────────────────────────────────────────
        private readonly List<CardSO>    _playerDiscard = new List<CardSO>();
        private readonly List<CardSO>    _enemyDiscard  = new List<CardSO>();
        private readonly List<DotEffect> _dotEffects    = new List<DotEffect>();

        // Snapshot of the player's deck as configured before the match starts.
        // Used by Treasure Chest to return cards from the original configuration.
        private readonly List<CardSO> _originalDeckSnapshot;

        public IReadOnlyList<CardSO> PlayerDiscard      => _playerDiscard.AsReadOnly();
        public IReadOnlyList<CardSO> EnemyDiscard       => _enemyDiscard.AsReadOnly();
        public IReadOnlyList<CardSO> OriginalDeckSnapshot => _originalDeckSnapshot.AsReadOnly();

        public Action OnEnemyTurnReady;

        public bool DeadMansTurnActive { get; private set; }
        public void SetDeadMansTurnActive() => DeadMansTurnActive = true;
        public void ClearDeadMansTurn()     => DeadMansTurnActive = false;

        public GameState(int startingHP, int startingMaxMana,
                         Deck playerDeck, Deck enemyDeck,
                         List<CardSO> originalDeckSnapshot)
        {
            _maxHP     = startingHP;
            PlayerHP   = startingHP;
            EnemyHP    = startingHP;
            PlayerDeck = playerDeck;
            EnemyDeck  = enemyDeck;
            PlayerHand = new Hand();
            EnemyHand  = new Hand();

            PlayerMaxMana = startingMaxMana;
            EnemyMaxMana  = startingMaxMana;

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
            PlayerHP = Mathf.Min(PlayerHP + amount, _maxHP);
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

        public void RestorePlayerMana(int amount)
        {
            PlayerMana = Mathf.Min(PlayerMana + amount, PlayerMaxMana);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
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

        // Steals amount from enemy mana and adds to player mana this turn.
        // Enemy mana floor is 1 — cannot be drained to 0.
        public void StealEnemyMana(int amount)
        {
            int actualSteal = Mathf.Min(amount, EnemyMana - 1);
            if (actualSteal <= 0) return;
            EnemyMana  -= actualSteal;
            PlayerMana += actualSteal;
            GameEventBus.FireEnemyManaChanged(EnemyMana, EnemyMaxMana);
            GameEventBus.FirePlayerManaChanged(PlayerMana, PlayerMaxMana);
        }

        // ── Reactions ─────────────────────────────────────────────────────────

        public void AddReactionCharge(CardType reactionType)
        {
            if (reactionType == CardType.Reaction)
            {
                // Generic — should not be called with base Reaction type
                return;
            }
        }

        // Named methods for each reaction type — cleaner than a generic switch
        public void AddDeadMansTurnCharge()
        {
            DeadMansTurnCharges++;
            GameEventBus.FireReactionCharged(CardType.Reaction, DeadMansTurnCharges);
        }

        public void AddBloodForBloodCharge()
        {
            BloodForBloodCharges++;
            GameEventBus.FireReactionCharged(CardType.Reaction, BloodForBloodCharges);
        }

        public bool ConsumeDeadMansTurn()
        {
            if (DeadMansTurnCharges <= 0) return false;
            DeadMansTurnCharges--;
            GameEventBus.FireReactionFired(CardType.Reaction);
            return true;
        }

        public bool ConsumeBloodForBlood()
        {
            if (BloodForBloodCharges <= 0) return false;
            BloodForBloodCharges--;
            GameEventBus.FireReactionFired(CardType.Reaction);
            return true;
        }

        public bool HasAnyReaction() =>
            DeadMansTurnCharges > 0 || BloodForBloodCharges > 0;

        // ── Status Effects ────────────────────────────────────────────────────

        public void SetSirenActive()
        {
            SirenSongActive    = true;
            PendingUnblockable = true;
        }

        public void ClearSiren()
        {
            SirenSongActive    = false;
            PendingUnblockable = false;
        }

        // ── DOT ───────────────────────────────────────────────────────────────

        public void AddDotEffect(DotEffect effect)
        {
            _dotEffects.Add(effect);
            GameEventBus.FireDOTApplied(effect);
        }

        public void ProcessDotEffects()
        {
            for (int i = _dotEffects.Count - 1; i >= 0; i--)
            {
                DotEffect dot = _dotEffects[i];
                ApplyDamage(dot.Target, dot.DamagePerTurn);
                GameEventBus.FireDOTTick(dot);

                if (dot.TurnsRemaining <= 1)
                    _dotEffects.RemoveAt(i);
                else
                    _dotEffects[i] = new DotEffect(dot.Target, dot.DamagePerTurn, dot.TurnsRemaining - 1);
            }
        }

        // ── Card Play Validation ──────────────────────────────────────────────

        public bool CanPlayCard(CardSO card)
        {
            if (!HasDrawnThisTurn) return false;

            // Reaction cards are charged on draw — never played from hand normally
            if (card.CardType == CardType.Reaction) return false;

            if (PlayerMana < card.ManaCost) return false;

            if (card.CardType == CardType.Action)
                return !ActionCardPlayedThisTurn && card.IsEligibleAsActionPair;

            return !DamageCardPlayedThisTurn;
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

        // ── Combo ─────────────────────────────────────────────────────────────

        public void IncrementCombo(CardSO card)
        {
            ActiveComboCard = card;
            ComboStackCount++;
            GameEventBus.FireComboStackChanged(ComboStackCount);
        }

        public int ResolveCombo()
        {
            int damage = ActiveComboCard.ComboDamage +
                         ((ComboStackCount - 1) * ActiveComboCard.ComboStackBonus);
            ResetCombo();
            GameEventBus.FireComboResolved();
            return damage;
        }

        public void ResetCombo()
        {
            ComboStackCount = 0;
            ActiveComboCard = null;
            GameEventBus.FireComboStackChanged(0);
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

        // Returns N random cards from the original deck snapshot for Treasure Chest.
        // Does not remove from snapshot — the snapshot is permanent reference data.
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
    }
}