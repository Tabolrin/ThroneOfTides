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

        public int    ComboStackCount { get; private set; }
        public CardSO ActiveComboCard { get; private set; }

        public bool SirenSongActive    { get; private set; }
        public bool PendingUnblockable { get; private set; }

        public bool DamageCardPlayedThisTurn { get; private set; }
        public bool ActionCardPlayedThisTurn { get; private set; }

        private readonly List<CardSO>   _playerDiscard = new List<CardSO>();
        private readonly List<CardSO>   _enemyDiscard  = new List<CardSO>();
        private readonly List<DotEffect> _dotEffects   = new List<DotEffect>();

        public IReadOnlyList<CardSO> PlayerDiscard => _playerDiscard.AsReadOnly();
        public IReadOnlyList<CardSO> EnemyDiscard  => _enemyDiscard.AsReadOnly();

        // Internal timing signal - stays on GameState not on bus
        public Action OnEnemyTurnReady;
        
        public bool DeadMansTurnActive { get; private set; }

        public void SetDeadMansTurnActive() => DeadMansTurnActive = true;
        public void ClearDeadMansTurn()     => DeadMansTurnActive = false;

        public GameState(int startingHP, Deck playerDeck, Deck enemyDeck)
        {
            _maxHP     = startingHP;
            PlayerHP   = startingHP;
            EnemyHP    = startingHP;
            PlayerDeck = playerDeck;
            EnemyDeck  = enemyDeck;
            PlayerHand = new Hand();
            EnemyHand  = new Hand();
        }

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

        public void AddDotEffect(DotEffect effect)
        {
            _dotEffects.Add(effect);
            GameEventBus.FireDOTApplied(effect);
        }

        // Iterates backwards so removal does not skip entries
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

        public bool CanPlayCard(CardSO card)
        {
            if (card.CardType == CardType.Action)
                return !ActionCardPlayedThisTurn && card.IsEligibleAsActionPair;
            return !DamageCardPlayedThisTurn;
        }

        public void RegisterCardPlayed(CardSO card)
        {
            if (card.CardType == CardType.Action)
                ActionCardPlayedThisTurn = true;
            else
                DamageCardPlayedThisTurn = true;
        }

        public void ResetTurnCardPlays()
        {
            DamageCardPlayedThisTurn = false;
            ActionCardPlayedThisTurn = false;
        }

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

        public void NotifyEnemyTurnReady()              => OnEnemyTurnReady?.Invoke();
        public void NotifyCardDrawn(CardSO card)        => GameEventBus.FireCardDrawn(card);
        public void NotifyPlayerCardRemoved(CardSO card) => GameEventBus.FirePlayerCardRemoved(card);

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

        public void DiscardPlayerCard(CardSO card) => _playerDiscard.Add(card);
        public void DiscardEnemyCard(CardSO card)  => _enemyDiscard.Add(card);

        // Returns up to count random cards from discard excluding Kraken
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
    }
}