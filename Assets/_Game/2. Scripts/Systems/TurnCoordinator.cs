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

        public System.Action OnTurnChanged;
        public System.Action OnHPChanged;

        public delegate void ReactionPromptHandler(
            CardSO card, int damage, string blockCost,
            System.Action onNegate, System.Action onTakeHit);
        public ReactionPromptHandler OnShowReactionPrompt;

        public delegate void TargetSelectionHandler(CardSO card, System.Action<DamageTarget> onTargetChosen);
        public TargetSelectionHandler OnShowTargetSelection;

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

            _gameState.OnEnemyTurnReady += OnEnemyTurnReady;
        }

        private void OnDestroy()
        {
            if (_gameState != null)
                _gameState.OnEnemyTurnReady -= OnEnemyTurnReady;
        }

        // ── Player Actions ────────────────────────────────────────────────────

        public void EndTurn()
        {
            if (!_gameState.IsPlayerTurn) return;

            if (_gameState.PlayerComboStackCount > 0 && !_gameState.DamageCardPlayedThisTurn)
                _gameState.ResetCombo(DamageTarget.Player);

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

            if (drawn.CardType == CardType.Reaction)
            {
                ChargeReaction(drawn);
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

        // Does not consume HasDrawnThisTurn — used by Treasure Chest secondary draw
        public bool TryDrawCardSecondary()
        {
            if (_gameState.PlayerDeck.Count == 0) return false;
            if (_gameState.PlayerHand.Count >= _config.MaxHandSize) return false;

            CardSO drawn = _gameState.PlayerDeck.Draw();
            if (drawn == null) return false;

            if (drawn.CardType == CardType.Reaction)
            {
                ChargeReaction(drawn);
                StartCoroutine(_handLayout.AnimateReactionDraw(drawn));
                return true;
            }

            _gameState.PlayerHand.AddCard(drawn, _config.MaxHandSize);
            GameEventBus.FireCardDrawn(drawn);
            StartCoroutine(_handLayout.AnimateManualDraw(drawn));
            return true;
        }

        public void HandleCardPlayed(CardSO cardSO)
        {
            if (!_gameState.CanPlayCard(cardSO))
            {
                Debug.Log($"Cannot play {cardSO.Name} — check draw, mana, or play limit");
                return;
            }

            if (!_gameState.SpendPlayerMana(cardSO.ManaCost))
            {
                Debug.Log($"Cannot play {cardSO.Name} — insufficient mana");
                return;
            }

            if (cardSO.RequiresTargetSelection)
            {
                StartCoroutine(PlayCardWithTargetSelection(cardSO));
                return;
            }

            FinishHandleCardPlayed(cardSO, null);
        }

        private IEnumerator PlayCardWithTargetSelection(CardSO cardSO)
        {
            DamageTarget? chosen = null;
            OnShowTargetSelection?.Invoke(cardSO, target => chosen = target);
            yield return new WaitUntil(() => chosen.HasValue);
            FinishHandleCardPlayed(cardSO, chosen);
        }

        private void FinishHandleCardPlayed(CardSO cardSO, DamageTarget? selectedTarget)
        {
            GameEventBus.FireCardPlayAccepted(cardSO, selectedTarget);
            _gameState.RegisterCardPlayed(cardSO);
            _gameState.PlayerHand.RemoveCard(cardSO);
            _gameState.DiscardPlayerCard(cardSO);

            _combatResolver.SetSecondaryDrawCallback(TryDrawCardSecondary);

            int damage = _combatResolver.ResolvePlayerCard(cardSO, _handLayout, selectedTarget);
            if (damage > 0)
                _gameState.ApplyDamage(DamageTarget.Enemy, damage);

            OnHPChanged?.Invoke();

            if (_gameState.IsGameOver()) FireMatchResult();
        }

        public void Concede()
        {
            Debug.Log("Player conceded");
            GameEventBus.FireMatchLoss();
        }

        // ── Auto Draw ─────────────────────────────────────────────────────────

        // Called at the end of each enemy turn. Short delay lets the turn
        // transition visual settle before the card animates in.
        // Not called on turn 1 — GameBootstrapper handles the opening hand deal.
        private IEnumerator AutoDrawRoutine()
        {
            yield return new WaitForSeconds(0.3f);
            TryDrawCard();
            OnHPChanged?.Invoke();
        }

        // ── Enemy Turn ────────────────────────────────────────────────────────

        private void OnEnemyTurnReady() => StartCoroutine(EnemyTurnRoutine());

        private IEnumerator EnemyTurnRoutine()
        {
            float delay = Random.Range(_config.EnemyThinkTimeMin, _config.EnemyThinkTimeMax);
            yield return new WaitForSeconds(delay);

            _gameState.ProcessDotEffects();
            OnHPChanged?.Invoke();

            if (_gameState.IsGameOver()) { FireMatchResult(); yield break; }

            _gameState.ResetEnemyMana();

            if (_gameState.EnemyHand.Count < _config.MaxHandSize)
            {
                CardSO enemyDrawn = _gameState.EnemyDeck.Draw();
                if (enemyDrawn != null)
                    _gameState.EnemyHand.AddCard(enemyDrawn, _config.MaxHandSize);
            }

            bool attackPlayedThisTurn = false;

            // Keeps playing cards until mana/HP/hand constraints leave nothing playable —
            // mana is the only balancing lever now, so the enemy uses as much of its turn as
            // it can afford rather than stopping after a single card.
            while (true)
            {
                CardSO playedCard = _enemyAI.PickCard(
                    _gameState.EnemyHand.CardsSO,
                    _gameState.EnemyMana,
                    _gameState.EnemyHP);

                if (playedCard == null) break;

                bool isAttackCard = playedCard.CardType == CardType.Weapon ||
                                    playedCard.CardType == CardType.Combo  ||
                                    playedCard.CardType == CardType.DOT;
                if (isAttackCard) attackPlayedThisTurn = true;

                yield return StartCoroutine(PlayEnemyCard(playedCard, isAttackCard));

                if (_gameState.IsGameOver()) { FireMatchResult(); yield break; }
            }

            // Siren Song is only meaningful if consumed by an attack the same turn it's cast —
            // mirrors the player-side clear in EndTurn() so a cast-but-unused Siren doesn't
            // linger and incorrectly buff some future enemy attack.
            if (_gameState.SirenSongActive && !attackPlayedThisTurn)
                _gameState.ClearSiren(DamageTarget.Enemy);

            _stateMachine.TransitionTo(_stateMachine.PlayerTurn);
            OnTurnChanged?.Invoke();
            StartCoroutine(AutoDrawRoutine());
        }

        private IEnumerator PlayEnemyCard(CardSO playedCard, bool isAttackCard)
        {
            _gameState.SpendEnemyMana(playedCard.ManaCost);
            _gameState.EnemyHand.RemoveCard(playedCard);
            _gameState.DiscardEnemyCard(playedCard);

            bool animationDone = false;
            GameEventBus.OnEnemyCardAnimationComplete += () => animationDone = true;
            GameEventBus.FireEnemyCardPlayed(playedCard);
            yield return new WaitUntil(() => animationDone);
            GameEventBus.OnEnemyCardAnimationComplete = null;

            if (isAttackCard)
                yield return StartCoroutine(ResolveEnemyAttack(playedCard));
            else if (playedCard.CardType == CardType.Action && playedCard.AiPlayBeforeAttack)
                _combatResolver.ResolveCard(playedCard, DamageTarget.Enemy, _handLayout);

            OnHPChanged?.Invoke();
        }

        private IEnumerator ResolveEnemyAttack(CardSO attackCard)
        {
            // Routed through CombatResolver for every attack type (not just Combo/DOT) so
            // enemy-side gunpowder stacking, damage-over-time tracking, and HP-cost deduction
            // (e.g. The Kraken's self-damage) all actually apply — previously Weapon cards
            // bypassed this and dealt flat attackCard.Damage with no HP cost taken.
            int damage = _combatResolver.ResolveCard(attackCard, DamageTarget.Enemy, _handLayout);

            // A combo primer or a DOT application deals no immediate damage this play.
            if (damage <= 0) yield break;

            bool isKraken      = attackCard.Name == "The Kraken";
            bool isUnblockable = _gameState.SirenSongActive || isKraken;
            bool hasDMT        = _gameState.DeadMansTurnCharges > 0;
            bool hasBFB        = _gameState.BloodForBloodCharges > 0;

            bool playerHasKraken = _gameState.PlayerHand.CardsSO.Any(c => c.Name == "The Kraken");
            if (isKraken && playerHasKraken)
            {
                yield return StartCoroutine(KrakenVsKrakenPrompt(attackCard));
                yield break;
            }

            bool canReact = !isUnblockable && (hasDMT || hasBFB);

            if (canReact)
                yield return StartCoroutine(ReactionPrompt(attackCard, damage, hasDMT, hasBFB));
            else
            {
                _gameState.ApplyDamage(DamageTarget.Player, damage);
                Debug.Log($"Enemy attack — {attackCard.Name}: {damage} dmg");
            }
        }

        private IEnumerator KrakenVsKrakenPrompt(CardSO attackCard)
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
        }

        private IEnumerator ReactionPrompt(CardSO attackCard, int damage, bool hasDMT, bool hasBFB)
        {
            string label = hasDMT && hasBFB
                ? "Dead Man's Turn (negate) | Blood for Blood (reflect half)"
                : hasDMT ? "Dead Man's Turn (negate)"
                         : "Blood for Blood (reflect half)";

            bool usedReaction = false;
            bool usedDMT      = false;
            bool playerChose  = false;

            OnShowReactionPrompt?.Invoke(
                attackCard, damage, label,
                () => { usedReaction = true; usedDMT = hasDMT; playerChose = true; },
                () => { playerChose = true; });

            yield return new WaitUntil(() => playerChose);

            if (!usedReaction)
            {
                _gameState.ApplyDamage(DamageTarget.Player, damage);
                yield break;
            }

            if (usedDMT && _gameState.ConsumeDeadMansTurn())
            {
                Debug.Log("Dead Man's Turn fired — attack negated");
            }
            else if (_gameState.ConsumeBloodForBlood())
            {
                int reflected = _combatResolver.ResolveBloodForBlood(damage);
                _gameState.ApplyDamage(DamageTarget.Enemy, reflected);
                _gameState.ApplyDamage(DamageTarget.Player, damage);
                Debug.Log($"Blood for Blood fired — reflected {reflected}, took {damage}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ChargeReaction(CardSO card)
        {
            if (card.Name == "Dead Man's Turn")      _gameState.AddDeadMansTurnCharge();
            else if (card.Name == "Blood for Blood") _gameState.AddBloodForBloodCharge();
        }

        private void FireMatchResult()
        {
            if (_gameState.GetWinner() == Winner.Player)
                GameEventBus.FireMatchWin();
            else
                GameEventBus.FireMatchLoss();
        }
    }
}