using System;
using System.Collections.Generic;
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

        // Cleared after attack card played or turn ends
        public bool SirenSongActive    { get; private set; }
        public bool PendingUnblockable { get; private set; }

        // Processed at start of each turn
        private readonly List<DotEffect> _dotEffects = new List<DotEffect>();

        public Action         OnEnemyTurnReady;
        public Action<CardSO> OnCardDrawn;

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
        }

        public void HealPlayer(int amount)
        {
            PlayerHP = Mathf.Min(PlayerHP + amount, _maxHP);
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
        }

        // Iterates backwards so removal does not skip entries
        public void ProcessDotEffects()
        {
            for (int i = _dotEffects.Count - 1; i >= 0; i--)
            {
                DotEffect dot = _dotEffects[i];
                ApplyDamage(dot.Target, dot.DamagePerTurn);

                if (dot.TurnsRemaining <= 1)
                    _dotEffects.RemoveAt(i);
                else
                    _dotEffects[i] = new DotEffect(dot.Target, dot.DamagePerTurn, dot.TurnsRemaining - 1);
            }
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

        public void NotifyEnemyTurnReady() => OnEnemyTurnReady?.Invoke();
        public void NotifyCardDrawn(CardSO card) => OnCardDrawn?.Invoke(card);

        public void IncrementCombo(CardSO card)
        {
            ActiveComboCard = card;
            ComboStackCount++;
        }

        public int ResolveCombo()
        {
            int damage = ActiveComboCard.ComboDamage +
                         ((ComboStackCount - 1) * ActiveComboCard.ComboStackBonus);
            ResetCombo();
            return damage;
        }

        public void ResetCombo()
        {
            ComboStackCount = 0;
            ActiveComboCard = null;
        }
    }
}