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

        // negateLabel/onNegate and counterGaleLabel/onCounterGale each come as a pair — pass
        // both null to hide that option entirely (e.g. Kraken-vs-Kraken has no Counter Gale
        // option; a lone-Dead-Man's-Turn defense has no Counter Gale option either).
        public delegate void ReactionPromptHandler(
            CardSO card, int damage,
            string negateLabel, System.Action onNegate,
            string counterGaleLabel, System.Action onCounterGale,
            System.Action onTakeHit);
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
            _combatResolver.SetSecondaryDrawCallback(TryDrawCardSecondary, TryDrawCardSecondaryEnemy);
        }

        private void OnDestroy()
        {
            if (_gameState != null)
                _gameState.OnEnemyTurnReady -= OnEnemyTurnReady;
        }

        // ── Player Actions ────────────────────────────────────────────────────

        public void EndTurn()
        {
            if (!_gameState.IsPlayerTurn)
            {
                GameDebug.Log("EndTurn ignored — not the player's turn (already ended, or match over).");
                return;
            }

            // Gunpowder the player primed lives on the Enemy ship (it's what Torch ignites there).
            if (_gameState.EnemyComboStackCount > 0 && !_gameState.DamageCardPlayedThisTurn)
                _gameState.ResetCombo(DamageTarget.Enemy);

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

        // Does not consume HasDrawnThisTurn — used by Treasure Chest secondary draw.
        // ignoreHandLimit lets a card (e.g. Treasure Chest) force its draws into the hand even
        // past MaxHandSize — passing an effectively unlimited cap through to Hand.AddCard too,
        // since it enforces the same limit itself and would otherwise silently drop the card.
        public bool TryDrawCardSecondary(bool ignoreHandLimit = false)
        {
            if (_gameState.PlayerDeck.Count == 0) return false;

            int maxHandSize = ignoreHandLimit ? int.MaxValue : _config.MaxHandSize;
            if (!ignoreHandLimit && _gameState.PlayerHand.Count >= maxHandSize) return false;

            CardSO drawn = _gameState.PlayerDeck.Draw();
            if (drawn == null) return false;

            if (drawn.CardType == CardType.Reaction)
            {
                ChargeReaction(drawn);
                StartCoroutine(_handLayout.AnimateReactionDraw(drawn));
                return true;
            }

            _gameState.PlayerHand.AddCard(drawn, maxHandSize);
            GameEventBus.FireCardDrawn(drawn);
            StartCoroutine(_handLayout.AnimateManualDraw(drawn));
            return true;
        }

        // Enemy-side mirror of TryDrawCardSecondary — used by caster-relative Action effects
        // (e.g. Treasure Chest) so an enemy-cast draw goes into the enemy's own hand.
        public bool TryDrawCardSecondaryEnemy(bool ignoreHandLimit = false)
        {
            if (_gameState.EnemyDeck.Count == 0) return false;

            int maxHandSize = ignoreHandLimit ? int.MaxValue : _config.MaxHandSize;
            if (!ignoreHandLimit && _gameState.EnemyHand.Count >= maxHandSize) return false;

            CardSO drawn = _gameState.EnemyDeck.Draw();
            if (drawn == null) return false;

            if (drawn.CardType == CardType.Reaction)
            {
                ChargeReaction(drawn, DamageTarget.Enemy);
                return true;
            }

            _gameState.EnemyHand.AddCard(drawn, maxHandSize);
            _handLayout.AddCardToEnemyHand(drawn);
            return true;
        }

        public void HandleCardPlayed(CardSO cardSO)
        {
            if (!_gameState.CanPlayCard(cardSO))
            {
                GameDebug.Log($"Cannot play {cardSO.Name} — check draw, mana, or play limit");
                return;
            }

            // Dead Man's Turn's cost (if pending) taxes only the very next card, whatever it
            // turns out to be — applied here rather than baked into ManaCost so it never shows
            // up on the card itself.
            int surcharge = _gameState.PlayerNextCardManaSurcharge;
            if (!_gameState.SpendPlayerMana(cardSO.ManaCost + surcharge))
            {
                GameDebug.Log($"Cannot play {cardSO.Name} — insufficient mana");
                return;
            }
            if (surcharge > 0) _gameState.ClearPlayerNextCardManaSurcharge();

            // Committed the instant mana is spent — the card leaves the hand regardless of
            // which target ends up chosen. CardView listens for this so it stops treating the
            // drag as "rejected, snap back" while a target-selection prompt is still pending;
            // OnCardPlayAccepted (below/later) fires the actual resolution once the target is
            // known, which is what VFX spawning keys off instead.
            GameEventBus.FireCardCommitted(cardSO);
            _gameState.RegisterCardPlayed(cardSO);
            _gameState.PlayerHand.RemoveCard(cardSO);
            _gameState.DiscardPlayerCard(cardSO);

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

            int damage = _combatResolver.ResolvePlayerCard(cardSO, _handLayout, selectedTarget);
            if (damage > 0)
                ApplyPlayerAttackToEnemy(cardSO, damage);

            OnHPChanged?.Invoke();

            if (_gameState.IsGameOver()) FireMatchResult();
        }

        // Enemy-side mirror of ResolveEnemyAttack/ReactionPrompt — the enemy has no UI to prompt,
        // so EnemyAI.ChooseReaction (a weighted decision, same idea as picking which card to
        // play) decides whether it negates or reflects instead of a player button click.
        private void ApplyPlayerAttackToEnemy(CardSO attackCard, int damage)
        {
            bool isUnblockable = _gameState.SirenSongActive;
            bool hasDMT         = _gameState.EnemyDeadMansTurnCharges > 0;
            bool hasCounterGale = _gameState.EnemyCounterGaleCharges  > 0;

            ReactionType? chosen = !isUnblockable
                ? _enemyAI.ChooseReaction(hasDMT, hasCounterGale)
                : null;

            if (chosen == ReactionType.DeadMansTurn && _gameState.ConsumeDeadMansTurn(DamageTarget.Enemy))
            {
                GameDebug.Log("Enemy used Dead Man's Turn — attack negated");
                return;
            }

            if (chosen == ReactionType.CounterGale && _gameState.ConsumeCounterGale(DamageTarget.Enemy))
            {
                int reflected = _combatResolver.ResolveCounterGale(damage);
                _gameState.ApplyDamage(DamageTarget.Player, reflected);
                _gameState.ApplyDamage(DamageTarget.Enemy, damage);
                GameDebug.Log($"Enemy used Counter Gale — reflected {reflected}, took {damage}");
                return;
            }

            _gameState.ApplyDamage(DamageTarget.Enemy, damage);
        }

        public void Concede()
        {
            GameDebug.Log("Player conceded");
            GameEventBus.FireMatchLoss();
        }

        // Lets callers outside the normal turn flow (e.g. CheatsPanel's HP buttons) trigger the
        // win/loss check — IsGameOver() is otherwise only evaluated at specific points in the
        // normal card-play/enemy-turn flow, so a cheat-driven HP change would never show results.
        public void CheckGameOver()
        {
            if (_gameState.IsGameOver()) FireMatchResult();
        }

        // ── Auto Draw ─────────────────────────────────────────────────────────

        // Called at the end of each enemy turn. Short delay lets the turn
        // transition visual settle before the cards animate in.
        // Not called on turn 1 — GameBootstrapper handles the opening hand deal.
        private IEnumerator AutoDrawRoutine()
        {
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(RefillPlayerHandRoutine());
            OnHPChanged?.Invoke();
        }

        // Draws until the hand is full (or the deck runs out) — the hand fully refills at the
        // start of each player turn rather than drawing a single card. Reaction cards are
        // charged instead of occupying a hand slot and don't count toward the fill target,
        // matching TryDrawCard's existing per-card handling.
        private IEnumerator RefillPlayerHandRoutine()
        {
            while (_gameState.PlayerHand.Count < _config.MaxHandSize && _gameState.PlayerDeck.Count > 0)
            {
                CardSO drawn = _gameState.PlayerDeck.Draw();
                if (drawn == null) break;

                if (drawn.CardType == CardType.Reaction)
                {
                    ChargeReaction(drawn);
                    yield return StartCoroutine(_handLayout.AnimateReactionDraw(drawn));
                    continue;
                }

                _gameState.PlayerHand.AddCard(drawn, _config.MaxHandSize);
                GameEventBus.FireCardDrawn(drawn);
                OnCardDrawn?.Invoke(drawn);
                yield return StartCoroutine(_handLayout.AnimateManualDraw(drawn));
            }

            _gameState.SetHasDrawnThisTurn();
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

            // The enemy hand fully refills at the start of its turn too, mirroring the player.
            // Reaction cards charge the enemy's own counter instead of occupying a hand slot —
            // EnemyAI.PickCard never plays Reaction-type cards, so leaving one in hand would
            // strand it there permanently unplayable.
            while (_gameState.EnemyHand.Count < _config.MaxHandSize && _gameState.EnemyDeck.Count > 0)
            {
                CardSO enemyDrawn = _gameState.EnemyDeck.Draw();
                if (enemyDrawn == null) break;

                if (enemyDrawn.CardType == CardType.Reaction)
                {
                    ChargeReaction(enemyDrawn, DamageTarget.Enemy);
                    continue;
                }

                _gameState.EnemyHand.AddCard(enemyDrawn, _config.MaxHandSize);
                _handLayout.AddCardToEnemyHand(enemyDrawn);
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
                    _gameState.EnemyHP,
                    comboPrimed: _gameState.EnemyComboStackCount > 0 && _gameState.EnemyActiveComboCard != null,
                    playerHasDeadMansTurn: _gameState.PlayerDeadMansTurnCharges > 0,
                    playerHasCounterGale: _gameState.PlayerCounterGaleCharges > 0,
                    selfUnblockable: _gameState.SirenSongActive);

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
            void OnAnimationComplete() => animationDone = true;
            GameEventBus.OnEnemyCardAnimationComplete += OnAnimationComplete;
            GameEventBus.FireEnemyCardPlayed(playedCard);
            yield return new WaitUntil(() => animationDone);
            GameEventBus.OnEnemyCardAnimationComplete -= OnAnimationComplete;

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

            bool isKraken      = attackCard.Id == CardId.Kraken;
            bool isUnblockable = _gameState.SirenSongActive || isKraken;
            bool hasDMT        = _gameState.PlayerDeadMansTurnCharges > 0;
            bool hasCounterGale = _gameState.PlayerCounterGaleCharges > 0;

            bool playerHasKraken = _gameState.PlayerHand.CardsSO.Any(c => c.Id == CardId.Kraken);
            if (isKraken && playerHasKraken)
            {
                yield return StartCoroutine(KrakenVsKrakenPrompt(attackCard));
                yield break;
            }

            bool canReact = !isUnblockable && (hasDMT || hasCounterGale);

            if (canReact)
                yield return StartCoroutine(ReactionPrompt(attackCard, damage, hasDMT, hasCounterGale));
            else
            {
                _gameState.ApplyDamage(DamageTarget.Player, damage);
                GameDebug.Log($"Enemy attack — {attackCard.Name}: {damage} dmg");
            }
        }

        private IEnumerator KrakenVsKrakenPrompt(CardSO attackCard)
        {
            bool wasNegated  = false;
            bool playerChose = false;

            OnShowReactionPrompt?.Invoke(
                attackCard, attackCard.Damage,
                "The Kraken\n(Sacrifice 3 HP + 33% materials)", () => { wasNegated = true;  playerChose = true; },
                null, null, // no Counter Gale option in a Kraken-vs-Kraken standoff
                () => { wasNegated = false; playerChose = true; });

            yield return new WaitUntil(() => playerChose);

            if (wasNegated)
            {
                CardSO krakenCard = _gameState.PlayerHand.CardsSO
                    .FirstOrDefault(c => c.Id == CardId.Kraken);
                if (krakenCard != null)
                {
                    _gameState.PlayerHand.RemoveCard(krakenCard);
                    _gameState.NotifyPlayerCardRemoved(krakenCard);
                    _gameState.DiscardPlayerCard(krakenCard);
                    _gameState.ApplyDamage(DamageTarget.Player, 3);
                    // TODO: deduct 33% materials when material system is built
                }
                GameDebug.Log("Kraken negated by player Kraken");
            }
            else
            {
                _gameState.ApplyDamage(DamageTarget.Player, attackCard.Damage);
            }
        }

        private IEnumerator ReactionPrompt(CardSO attackCard, int damage, bool hasDMT, bool hasCounterGale)
        {
            // Preview-only math — must match CombatResolver.ResolveCounterGale's own formula.
            // Not calling that method here since it also logs, which would misfire if the
            // player ends up picking a different option than the one being previewed.
            int reflectedPreview = Mathf.FloorToInt(damage * 0.5f);

            string negateLabel = hasDMT
                ? "Dead Man's Turn\n(Take 0 damage — costs 1 HP, next card +1 mana)"
                : null;
            string counterGaleLabel = hasCounterGale
                ? $"Counter Gale\n(Take {damage}, deal {reflectedPreview} back — refund 1 mana, draw a card)"
                : null;

            // null = no choice made yet / Take the Hit; true = Dead Man's Turn; false = Counter Gale.
            bool? usedDMT      = null;
            bool  playerChose  = false;

            OnShowReactionPrompt?.Invoke(
                attackCard, damage,
                negateLabel,      hasDMT ? new System.Action(() => { usedDMT = true;  playerChose = true; }) : null,
                counterGaleLabel, hasCounterGale ? new System.Action(() => { usedDMT = false; playerChose = true; }) : null,
                () => { playerChose = true; });

            yield return new WaitUntil(() => playerChose);

            if (usedDMT == null)
            {
                _gameState.ApplyDamage(DamageTarget.Player, damage);
                yield break;
            }

            if (usedDMT == true && _gameState.ConsumeDeadMansTurn())
            {
                // Negates the attack, but isn't free: costs 1 HP ("you throw yourself clear,
                // but you get banged up") and taxes the next card played by +1 mana.
                _gameState.ApplyDamage(DamageTarget.Player, 1);
                _gameState.AddPlayerNextCardManaSurcharge(1);
                GameDebug.Log("Dead Man's Turn fired — attack negated (-1 HP, next card +1 mana)");
                GameEventBus.FireMatchNote("Dead Man's Turn — attack negated, but it cost 1 HP and your next card costs +1 mana.");
            }
            else if (usedDMT == false && _gameState.ConsumeCounterGale())
            {
                int reflected = _combatResolver.ResolveCounterGale(damage);
                _gameState.ApplyDamage(DamageTarget.Enemy, reflected);
                _gameState.ApplyDamage(DamageTarget.Player, damage);
                _gameState.RefundPlayerMana(1);
                bool drewCard = TryDrawCardSecondary();
                string drawSuffix = drewCard ? ", drew a card" : ", hand full — no card drawn";
                GameDebug.Log($"Counter Gale fired — reflected {reflected}, took {damage}, refunded 1 mana{drawSuffix}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ChargeReaction(CardSO card, DamageTarget side = DamageTarget.Player)
        {
            if (card.Id == CardId.DeadMansTurn)   _gameState.AddDeadMansTurnCharge(side);
            else if (card.Id == CardId.CounterGale) _gameState.AddCounterGaleCharge(side);
        }

        private void FireMatchResult()
        {
            Winner winner = _gameState.GetWinner();
            GameDebug.Log($"Match over — winner: {winner} " +
                $"(PlayerHP: {_gameState.PlayerHP}, PlayerDeck: {_gameState.PlayerDeck.Count}, PlayerHand: {_gameState.PlayerHand.Count}, " +
                $"EnemyHP: {_gameState.EnemyHP}, EnemyDeck: {_gameState.EnemyDeck.Count}, EnemyHand: {_gameState.EnemyHand.Count})");

            if (winner == Winner.Player)
                GameEventBus.FireMatchWin();
            else
                GameEventBus.FireMatchLoss();
        }
    }
}