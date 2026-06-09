// Assets/_Game/2. Scripts/Systems/TurnCoordinator.cs
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
        // Prevents auto-draw firing on the first turn — opening hand is dealt by GameBootstrapper
        private bool _autoDrawEnabled = false;

        public System.Action OnTurnChanged;
        public System.Action OnHPChanged;

        public delegate void ReactionPromptHandler(
            CardSO card, int damage, string blockCost,
            System.Action onNegate, System.Action onTakeHit);
        public ReactionPromptHandler OnShowReactionPrompt;

        public System.Action<CardSO> OnCardDrawn;

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

            _combatResolver.SecondaryDrawCallback = TryDrawCardSecondary;
            _gameState.OnEnemyTurnReady += OnEnemyTurnReady;
            // Auto-draw activates after the first enemy turn completes
            _autoDrawEnabled = false;
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
            OnTurnChanged?.Invoke();
        }

        public bool TryDrawCard()
        {
            if (!_gameState.CanDraw()) return false;

            CardSO drawn = _gameState.PlayerDeck.Draw();
            if (drawn == null) return false;

            // Reaction cards charge the effects bar instead of entering the hand
            if (drawn.CardType == CardType.Reaction)
            {
                ChargeReactionCard(drawn);
                _gameState.SetHasDrawnThisTurn();
                OnCardDrawn?.Invoke(drawn);
                StartCoroutine(_handLayout.AnimateReactionDraw(drawn));
                return true;
            }

            _gameState.PlayerHand.AddCard(drawn, _config.MaxHandSize);
            _gameState.SetHasDrawnThisTurn();
            GameEventBus.FireCardDrawn(drawn);
            OnCardDrawn?.Invoke(drawn);
            StartCoroutine(_handLayout.AnimateManualDraw(drawn));
            return true;
        }

        // Secondary draw — used by Treasure Chest. Does not consume HasDrawnThisTurn.
        public bool TryDrawCardSecondary()
        {
            if (_gameState.PlayerDeck.Count == 0) return false;
            if (_gameState.PlayerHand.Count >= _config.MaxHandSize) return false;

            CardSO drawn = _gameState.PlayerDeck.Draw();
            if (drawn == null) return false;

            if (drawn.CardType == CardType.Reaction)
            {
                ChargeReactionCard(drawn);
                StartCoroutine(_handLayout.AnimateReactionDraw(drawn));
                return true;
            }

            _gameState.PlayerHand.AddCard(drawn, _config.MaxHandSize);
            GameEventBus.FireCardDrawn(drawn);
            StartCoroutine(_handLayout.AnimateManualDraw(drawn));
            return true;
        }

        private void ChargeReactionCard(CardSO card)
        {
            if (card.Name == "Dead Man's Turn")
                _gameState.AddDeadMansTurnCharge();
            else if (card.Name == "Blood for Blood")
                _gameState.AddBloodForBloodCharge();
        }

        public void Concede()
        {
            Debug.Log("Player conceded");
            GameEventBus.FireMatchLoss();
        }

        public void HandleCardPlayed(CardSO cardSO)
        {
            if (!_gameState.CanPlayCard(cardSO))
            {
                Debug.Log($"Cannot play {cardSO.Name} — limit reached, draw required, or insufficient mana");
                return;
            }

            // Spend mana before resolving
            if (!_gameState.SpendPlayerMana(cardSO.ManaCost))
            {
                Debug.Log($"Cannot play {cardSO.Name} — insufficient mana");
                return;
            }

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
                FireMatchResult();
            }
        }

        private void OnEnemyTurnReady() => StartCoroutine(EnemyTurnRoutine());

        private IEnumerator EnemyTurnRoutine()
        {
            float delay = Random.Range(_config.EnemyThinkTimeMin, _config.EnemyThinkTimeMax);
            yield return new WaitForSeconds(delay);

            _gameState.ProcessDotEffects();
            OnHPChanged?.Invoke();

            if (_gameState.IsGameOver()) { FireMatchResult(); yield break; }

            // Reset enemy mana at start of enemy turn
            _gameState.ResetEnemyMana();

            if (_gameState.EnemyHand.Count < _config.MaxHandSize)
            {
                CardSO enemyDrawn = _gameState.EnemyDeck.Draw();
                if (enemyDrawn != null)
                    _gameState.EnemyHand.AddCard(enemyDrawn, _config.MaxHandSize);
            }

            CardSO playedCard = _enemyAI.PickCard(
                _gameState.EnemyHand.CardsSO,
                damageCardPlayed: false,
                actionCardPlayed: false,
                enemyMana: _gameState.EnemyMana);

            if (playedCard == null)
            {
                Debug.Log("Enemy has no valid card — skipping turn");
                _stateMachine.TransitionTo(_stateMachine.PlayerTurn);
                OnTurnChanged?.Invoke();
                yield break;
            }

            // Spend enemy mana
            _gameState.SpendEnemyMana(playedCard.ManaCost);

            _gameState.EnemyHand.RemoveCard(playedCard);
            _gameState.DiscardEnemyCard(playedCard);

            bool animationDone = false;
            GameEventBus.OnEnemyCardAnimationComplete += () => animationDone = true;
            GameEventBus.FireEnemyCardPlayed(playedCard);
            yield return new WaitUntil(() => animationDone);
            GameEventBus.OnEnemyCardAnimationComplete = null;

            bool isAttackCard = playedCard.CardType == CardType.Weapon ||
                                playedCard.CardType == CardType.Combo  ||
                                playedCard.CardType == CardType.DOT;

            if (isAttackCard)
                yield return StartCoroutine(ResolveEnemyAttack(playedCard));

            OnHPChanged?.Invoke();
            if (_gameState.IsGameOver()) { FireMatchResult(); yield break; }

            _stateMachine.TransitionTo(_stateMachine.PlayerTurn);
            OnTurnChanged?.Invoke();
            _autoDrawEnabled = true;
            StartCoroutine(AutoDrawRoutine());
        }

        // Separated from EnemyTurnRoutine for clarity — handles all reaction logic
        private IEnumerator ResolveEnemyAttack(CardSO attackCard)
        {
            bool isKraken     = attackCard.Name == "The Kraken";
            bool isUnblockable = _gameState.SirenSongActive || isKraken;

            // The Kraken is blockable only by another Kraken — handled separately
            bool hasDMT = _gameState.DeadMansTurnCharges > 0;
            bool hasBFB = _gameState.BloodForBloodCharges > 0;

            // Kraken cannot be countered by DMT or BFB
            bool canReact = !isKraken && !_gameState.SirenSongActive && (hasDMT || hasBFB);

            // Kraken vs Kraken — separate prompt
            bool playerHasKraken = _gameState.PlayerHand.CardsSO.Any(c => c.Name == "The Kraken");
            if (isKraken && playerHasKraken)
            {
                bool wasNegated  = false;
                bool playerChose = false;

                OnShowReactionPrompt?.Invoke(
                    attackCard, attackCard.Damage,
                    "The Kraken (3 HP + 33% materials)",
                    () => { wasNegated = true;  playerChose = true; },
                    () => { wasNegated = false; playerChose = true; });

                yield return new WaitUntil(() => playerChose);

                if (wasNegated)
                {
                    CardSO krakenCard = _gameState.PlayerHand.CardsSO
                        .FirstOrDefault(c => c.Name == "The Kraken");
                    if (krakenCard != null)
                    {
                        _gameState.PlayerHand.RemoveCard(krakenCard);
                        _gameState.NotifyPlayerCardRemoved(krakenCard);
                        _gameState.DiscardPlayerCard(krakenCard);
                        _gameState.ApplyDamage(DamageTarget.Player, 3);
                        // TODO: deduct 33% materials when material system is built
                    }
                    Debug.Log("Kraken negated by player Kraken");
                }
                else
                {
                    _gameState.ApplyDamage(DamageTarget.Player, attackCard.Damage);
                }
                yield break;
            }

            // Normal attack with possible reactions
            if (canReact)
            {
                // Build prompt label from available reactions
                string reactionLabel = hasDMT && hasBFB
                    ? "Dead Man's Turn (negate) or Blood for Blood (reflect half)"
                    : hasDMT ? "Dead Man's Turn" : "Blood for Blood";

                bool usedReaction = false;
                bool playerChose  = false;
                bool usedDMT      = false;

                OnShowReactionPrompt?.Invoke(
                    attackCard, attackCard.Damage,
                    reactionLabel,
                    () =>
                    {
                        // Player chose to react — prefer DMT if both available
                        usedDMT      = hasDMT;
                        usedReaction = true;
                        playerChose  = true;
                    },
                    () => { playerChose = true; });

                yield return new WaitUntil(() => playerChose);

                if (usedReaction)
                {
                    if (usedDMT && _gameState.ConsumeDeadMansTurn())
                    {
                        Debug.Log("Dead Man's Turn — attack negated");
                        // No damage applied
                    }
                    else if (_gameState.ConsumeBloodForBlood())
                    {
                        int reflected = _combatResolver.ResolveBloodForBlood(attackCard.Damage);
                        _gameState.ApplyDamage(DamageTarget.Enemy, reflected);
                        Debug.Log($"Blood for Blood — reflected {reflected} damage");
                        // Player still takes full damage — cannot be cancelled
                        _gameState.ApplyDamage(DamageTarget.Player, attackCard.Damage);
                    }
                }
                else
                {
                    _gameState.ApplyDamage(DamageTarget.Player, attackCard.Damage);
                    Debug.Log($"No reaction — took {attackCard.Damage} damage");
                }
            }
            else
            {
                _gameState.ApplyDamage(DamageTarget.Player, attackCard.Damage);
                Debug.Log($"Enemy played: {attackCard.Name} — {attackCard.Damage} damage");
            }
        }

        private void FireMatchResult()
        {
            if (_gameState.GetWinner() == Winner.Player)
                GameEventBus.FireMatchWin();
            else
                GameEventBus.FireMatchLoss();
        }
        
        // Draws one card automatically at the start of each player turn after turn 1.
// Short delay lets the turn transition animation settle before the card flies in.
        private IEnumerator AutoDrawRoutine()
        {
            yield return new WaitForSeconds(0.3f);
            TryDrawCard();
            OnHPChanged?.Invoke();
        }
    }
}