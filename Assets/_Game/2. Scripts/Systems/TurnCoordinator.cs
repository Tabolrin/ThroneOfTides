using System.Collections;
using System.Linq;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class TurnCoordinator : MonoBehaviour
    {
        private GameState          _gameState;
        private TurnStateMachine   _stateMachine;
        private EnemyAI            _enemyAI;
        private IHandLayoutManager _handLayout;
        private CombatResolver     _combatResolver;
        private GameConfigSO       _config;

        public System.Action OnTurnChanged;
        public System.Action OnHPChanged;

        public delegate void DeadMansTurnPromptHandler(
            CardSO card, int damage, string blockCost,
            System.Action onNegate, System.Action onTakeHit);
        public DeadMansTurnPromptHandler OnShowDeadMansTurnPrompt;

        public bool IsPlayerTurn => _gameState?.IsPlayerTurn ?? false;

        public void Initialise(GameState gameState, TurnStateMachine stateMachine,
                               EnemyAI enemyAI, IHandLayoutManager handLayout,
                               CombatResolver combatResolver, GameConfigSO config)
        {
            _gameState      = gameState;
            _stateMachine   = stateMachine;
            _enemyAI        = enemyAI;
            _handLayout     = handLayout;
            _combatResolver = combatResolver;
            _config         = config;

            _gameState.OnEnemyTurnReady += OnEnemyTurnReady;
        }

        private void OnDestroy()
        {
            if (_gameState != null)
                _gameState.OnEnemyTurnReady -= OnEnemyTurnReady;
        }

        public void EndTurn()
        {
            if (!_gameState.IsPlayerTurn) return;

            if (_gameState.ComboStackCount > 0 && !_gameState.DamageCardPlayedThisTurn)
                _gameState.ResetCombo();

            if (_gameState.SirenSongActive && !_gameState.DamageCardPlayedThisTurn)
                _gameState.ClearSiren();

            _stateMachine.TransitionTo(_stateMachine.EnemyTurn);
        }

        public void HandleCardPlayed(CardSO cardSO)
        {
            if (!_gameState.CanPlayCard(cardSO))
            {
                Debug.Log($"Cannot play {cardSO.Name} - card play limit reached");
                return;
            }

            // Notify UI this specific card instance was accepted
            GameEventBus.FireCardPlayAccepted(cardSO);

            _gameState.RegisterCardPlayed(cardSO);
            _gameState.PlayerHand.RemoveCard(cardSO);
            _gameState.DiscardPlayerCard(cardSO);

            int damage = _combatResolver.ResolvePlayerCard(cardSO, _handLayout);
            if (damage > 0)
                _gameState.ApplyDamage(DamageTarget.Enemy, damage);

            OnHPChanged?.Invoke();

            if (_gameState.IsGameOver())
            {
                if (_gameState.GetWinner() == Winner.Player)
                    GameEventBus.FireMatchWin();
                else
                    GameEventBus.FireMatchLoss();
            }
        }

        private void OnEnemyTurnReady() => StartCoroutine(EnemyTurnRoutine());

        private IEnumerator EnemyTurnRoutine()
        {
            float delay = Random.Range(_config.EnemyThinkTimeMin, _config.EnemyThinkTimeMax);
            yield return new WaitForSeconds(delay);

            // Process DOT effects at start of enemy turn per GDD
            _gameState.ProcessDotEffects();
            OnHPChanged?.Invoke();

            // Check if DOT ended the match
            if (_gameState.IsGameOver())
            {
                if (_gameState.GetWinner() == Winner.Player)
                    GameEventBus.FireMatchWin();
                else
                    GameEventBus.FireMatchLoss();
                yield break;
            }

            // Draw enemy card into logical hand
            if (_gameState.EnemyHand.Count < _config.MaxHandSize)
            {
                CardSO enemyDrawn = _gameState.EnemyDeck.Draw();
                if (enemyDrawn != null)
                    _gameState.EnemyHand.AddCard(enemyDrawn, _config.MaxHandSize);
            }

            // AI picks card
            CardSO playedCard = _enemyAI.PickCard(
                _gameState.EnemyHand.CardsSO,
                damageCardPlayed: false,
                actionCardPlayed: false);

            if (playedCard == null)
            {
                Debug.Log("Enemy has no valid card - skipping turn");
                _stateMachine.TransitionTo(_stateMachine.PlayerTurn);
                yield break;
            }

            _gameState.EnemyHand.RemoveCard(playedCard);
            _gameState.DiscardEnemyCard(playedCard);

            // Subscribe BEFORE firing so callback is never missed
            bool animationDone = false;
            GameEventBus.OnEnemyCardAnimationComplete += () => animationDone = true;
            GameEventBus.FireEnemyCardPlayed(playedCard);
            yield return new WaitUntil(() => animationDone);
            GameEventBus.OnEnemyCardAnimationComplete = null;

            bool isKraken        = playedCard.Name == "The Kraken";
            bool playerHasDMT    = _gameState.PlayerHand.CardsSO.Any(c => c.Name == "Dead Man's Turn");
            bool playerHasKraken = _gameState.PlayerHand.CardsSO.Any(c => c.Name == "The Kraken");
            bool isAttackCard    = playedCard.CardType == CardType.Weapon ||
                                   playedCard.CardType == CardType.Combo  ||
                                   playedCard.CardType == CardType.DOT;

            bool canBlock = isAttackCard && !_gameState.SirenSongActive &&
                            ((isKraken && playerHasKraken) || (!isKraken && playerHasDMT));

            if (canBlock)
            {
                string blockCost   = isKraken ? "The Kraken (3 HP + 33% materials)" : "Dead Man's Turn";
                bool   wasNegated  = false;
                bool   playerChose = false;

                OnShowDeadMansTurnPrompt?.Invoke(
                    playedCard,
                    playedCard.Damage,
                    blockCost,
                    () => { wasNegated = true;  playerChose = true; },
                    () => { wasNegated = false; playerChose = true; }
                );

                yield return new WaitUntil(() => playerChose);

                if (wasNegated)
                {
                    string blockCardName = isKraken ? "The Kraken" : "Dead Man's Turn";
                    CardSO blockCard     = _gameState.PlayerHand.CardsSO
                        .FirstOrDefault(c => c.Name == blockCardName);

                    if (blockCard != null)
                    {
                        _gameState.PlayerHand.RemoveCard(blockCard);
                        _gameState.NotifyPlayerCardRemoved(blockCard);
                        _gameState.DiscardPlayerCard(blockCard);
                        // TODO - deduct 33% materials when material system is built
                        if (isKraken)
                            _gameState.ApplyDamage(DamageTarget.Player, 3);
                    }
                    Debug.Log($"Attack negated - {playedCard.Name} blocked");
                }
                else
                {
                    _gameState.ApplyDamage(DamageTarget.Player, playedCard.Damage);
                    Debug.Log($"Took hit - {playedCard.Name} damage: {playedCard.Damage}");
                }
            }
            else if (isAttackCard)
            {
                _gameState.ApplyDamage(DamageTarget.Player, playedCard.Damage);
                Debug.Log($"Enemy played: {playedCard.Name} - damage: {playedCard.Damage}");
            }

            OnHPChanged?.Invoke();

            if (_gameState.IsGameOver())
            {
                if (_gameState.GetWinner() == Winner.Player)
                    GameEventBus.FireMatchWin();
                else
                    GameEventBus.FireMatchLoss();
                yield break;
            }

            _stateMachine.TransitionTo(_stateMachine.PlayerTurn);
        }
    }
}