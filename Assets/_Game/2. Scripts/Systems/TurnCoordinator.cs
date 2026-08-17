// Assets/_Game/2. Scripts/Systems/TurnCoordinator.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    public class TurnCoordinator : MonoBehaviour
    {
        // How long a reaction card sits fully dealt into the hand (looking like any other card)
        // before it flies off to the reaction badge area and shrinks away.
        private const float ReactionAbsorbDelay = 0.4f;

        private GameState          _gameState;
        private TurnStateMachine   _stateMachine;
        private EnemyAI            _enemyAI;
        private IHandLayoutManager _handLayout;
        private CombatResolver     _combatResolver;
        private GameConfigSO       _config;

        // Playtest-only cheat override - see ForceEnemyToPlayCard.
        private CardSO _forcedEnemyCard;

        public System.Action OnTurnChanged;
        public System.Action OnHPChanged;

        // negateLabel/onNegate and counterGaleLabel/onCounterGale each come as a pair - pass
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
            if (!_gameState.IsPlayerTurn || _gameState.MatchOver)
            {
                GameDebug.Log("EndTurn ignored - not the player's turn (already ended, or match over).");
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

            _gameState.SetHasDrawnThisTurn();
            OnCardDrawn?.Invoke(drawn);

            if (drawn.CardType == CardType.Reaction)
            {
                StartCoroutine(DrawReactionCardThenAbsorb(drawn));
                return true;
            }

            _gameState.PlayerHand.AddCard(drawn, _config.MaxHandSize);
            GameEventBus.FireCardDrawn(drawn);
            StartCoroutine(_handLayout.AnimateManualDraw(drawn));
            return true;
        }

        // Does not consume HasDrawnThisTurn - used by Treasure Chest secondary draw.
        // ignoreHandLimit lets a card (e.g. Treasure Chest) push its draws past the normal
        // MaxHandSize, but only up to GameState.BonusMaxHandSize - never truly unlimited. Once
        // the hand is already at that absolute ceiling, the draw is skipped (not just capped
        // silently) and logged so the player understands why a guaranteed draw didn't happen.
        public bool TryDrawCardSecondary(bool ignoreHandLimit = false)
        {
            if (_gameState.PlayerDeck.Count == 0) return false;

            int maxHandSize = ignoreHandLimit ? _gameState.BonusMaxHandSize : _config.MaxHandSize;
            if (_gameState.PlayerHand.Count >= maxHandSize)
            {
                if (ignoreHandLimit)
                {
                    GameDebug.Log($"Player hand at max capacity ({maxHandSize}) - bonus draw skipped.");
                    GameEventBus.FireMatchNote($"Hand is full (max {maxHandSize}) - a bonus card was skipped.");
                }
                return false;
            }

            CardSO drawn = _gameState.PlayerDeck.Draw();
            if (drawn == null) return false;

            if (drawn.CardType == CardType.Reaction)
            {
                StartCoroutine(DrawReactionCardThenAbsorb(drawn));
                return true;
            }

            _gameState.PlayerHand.AddCard(drawn, maxHandSize);
            GameEventBus.FireCardDrawn(drawn);
            StartCoroutine(_handLayout.AnimateManualDraw(drawn));
            return true;
        }

        // Enemy-side mirror of TryDrawCardSecondary - used by caster-relative Action effects
        // (e.g. Treasure Chest) so an enemy-cast draw goes into the enemy's own hand.
        public bool TryDrawCardSecondaryEnemy(bool ignoreHandLimit = false)
        {
            if (_gameState.EnemyDeck.Count == 0) return false;

            int maxHandSize = ignoreHandLimit ? _gameState.BonusMaxHandSize : _config.MaxHandSize;
            if (_gameState.EnemyHand.Count >= maxHandSize)
            {
                if (ignoreHandLimit)
                    GameDebug.Log($"Enemy hand at max capacity ({maxHandSize}) - bonus draw skipped.");
                return false;
            }

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
                GameDebug.Log($"Cannot play {cardSO.Name} - check draw, mana, or play limit");
                return;
            }

            // Dead Man's Turn's cost (if pending) taxes only the very next card, whatever it
            // turns out to be - applied here rather than baked into ManaCost so it never shows
            // up on the card itself.
            int surcharge = _gameState.PlayerNextCardManaSurcharge;
            if (!_gameState.SpendPlayerMana(cardSO.ManaCost + surcharge))
            {
                GameDebug.Log($"Cannot play {cardSO.Name} - insufficient mana");
                return;
            }
            if (surcharge > 0) _gameState.ClearPlayerNextCardManaSurcharge();

            // Committed the instant mana is spent - the card leaves the hand regardless of
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

        // Enemy-side mirror of ResolveEnemyAttack/ReactionPrompt - the enemy has no UI to prompt,
        // so EnemyAI.ChooseReaction (a weighted decision, same idea as picking which card to
        // play) decides whether it negates or reflects instead of a player button click.
        private void ApplyPlayerAttackToEnemy(CardSO attackCard, int damage)
        {
            // See ConsumeSirenIfActive - without this, one player Siren cast would silently make
            // every later player attack this turn (and beyond) unblockable too, not just the
            // next one.
            bool isUnblockable = ConsumeSirenIfActive(DamageTarget.Player);
            bool hasDMT         = _gameState.EnemyDeadMansTurnCharges > 0;
            bool hasCounterGale = _gameState.EnemyCounterGaleCharges  > 0;

            ReactionType? chosen = !isUnblockable
                ? _enemyAI.ChooseReaction(hasDMT, hasCounterGale)
                : null;

            if (chosen == ReactionType.DeadMansTurn && _gameState.ConsumeDeadMansTurn(DamageTarget.Enemy))
            {
                // Same cost as the player's own use: not free - costs 1 HP and taxes the
                // enemy's next card played by +1 mana.
                _gameState.ApplyDamage(DamageTarget.Enemy, 1);
                _gameState.AddEnemyNextCardManaSurcharge(1);
                GameDebug.Log("Enemy used Dead Man's Turn - attack negated (-1 HP, next card +1 mana)");
                GameEventBus.FireMatchNote("Enemy used Dead Man's Turn - negated the attack, but it cost 1 HP and its next card costs +1 mana.");
                return;
            }

            if (chosen == ReactionType.CounterGale && _gameState.ConsumeCounterGale(DamageTarget.Enemy))
            {
                int reflected = _combatResolver.ResolveCounterGale(damage);
                _gameState.ApplyDamage(DamageTarget.Player, reflected);
                _gameState.ApplyDamage(DamageTarget.Enemy, damage);
                // Same bonus as the player's own use: refunds 1 mana and draws a replacement card.
                _gameState.RefundEnemyMana(1);
                bool drewCard = TryDrawCardSecondaryEnemy();
                string drawSuffix = drewCard ? ", drew a card" : ", hand full - no card drawn";
                GameDebug.Log($"Enemy used Counter Gale - reflected {reflected}, took {damage}, refunded 1 mana{drawSuffix}");
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
        // win/loss check - IsGameOver() is otherwise only evaluated at specific points in the
        // normal card-play/enemy-turn flow, so a cheat-driven HP change would never show results.
        public void CheckGameOver()
        {
            if (_gameState.IsGameOver()) FireMatchResult();
        }

        // Playtest-only cheat hook (see ForceEnemyCardCheatPanel) - guarantees the given card is
        // the very next one the enemy plays, bypassing EnemyAI's own selection heuristics and
        // mana-affordability filtering entirely (consumed one-shot by the enemy turn loop above).
        // Forces the card into the enemy's hand first (past the normal hand-size limit, since
        // this is a debug override) if it isn't already there.
        public void ForceEnemyToPlayCard(CardSO card)
        {
            if (card == null || _gameState == null) return;

            if (!_gameState.EnemyHand.CardsSO.Contains(card))
            {
                _gameState.EnemyHand.AddCard(card, int.MaxValue);
                _handLayout.AddCardToEnemyHand(card);
            }

            _forcedEnemyCard = card;
            GameDebug.Log($"[Cheat] Forcing enemy to play {card.Name} next.");
        }

        // ── Auto Draw ─────────────────────────────────────────────────────────

        // Called at the end of each enemy turn. Short delay lets the turn
        // transition visual settle before the cards animate in.
        // Not called on turn 1 - GameBootstrapper handles the opening hand deal.
        private IEnumerator AutoDrawRoutine()
        {
            // Set here, before the settle delay below - not just before RefillPlayerHandRoutine's
            // own loop - since the deck stays clickable through this delay too, and a click here
            // would otherwise pass CanDraw()'s !HasDrawnThisTurn check the same way one during the
            // refill's own card animations would. See RefillPlayerHandRoutine's remarks.
            _gameState.SetHasDrawnThisTurn();

            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(RefillPlayerHandRoutine());
            OnHPChanged?.Invoke();
        }

        // Draws until the player's total held resources (hand cards + already-charged reactions)
        // reach MaxHandSize, re-checking after every single card rather than trusting a count
        // computed once up front - see the remarks inside. A Reaction card still counts as one of
        // this turn's draws even though it ends up as a badge charge instead of a hand card, so
        // drawing 4 cards where 1 is a Reaction always ends with 3 cards in hand, never with the
        // draw continuing until 4 non-Reaction cards have been found.
        private IEnumerator RefillPlayerHandRoutine()
        {
            var pendingReactionCards = new List<CardSO>();

            // Defensive duplicate - AutoDrawRoutine already sets this before calling here, closing
            // the deck-click race for its own settle delay too. Kept here as well in case this
            // routine is ever called from somewhere that doesn't already do that.
            _gameState.SetHasDrawnThisTurn();

            // Re-checked after every single card, not computed once up front - if the hand (plus
            // already-charged reactions) is already at or above MaxHandSize when this runs (e.g. a
            // Treasure Chest/Monkey Grab bonus pushed it past 4 last turn), this draws nothing at
            // all and leaves the existing total exactly as it was; it only ever comes down once
            // the player actually plays cards. pendingReactionCards.Count is added in because a
            // reaction card drawn THIS loop sits in neither PlayerHand nor the charge counters yet
            // (it only becomes a charge once AbsorbReactionCard runs, after the whole loop below
            // finishes) - without counting it here too, several reactions drawn back to back would
            // each look like they hadn't used up a slot, letting the loop keep going past MaxHandSize.
            while (_gameState.PlayerHand.Count + _gameState.PlayerDeadMansTurnCharges + _gameState.PlayerCounterGaleCharges + pendingReactionCards.Count < _config.MaxHandSize
                   && _gameState.PlayerDeck.Count > 0)
            {
                CardSO drawn = _gameState.PlayerDeck.Draw();
                if (drawn == null) break;

                // Reaction cards deal into the fanned hand exactly like any other card - they
                // only fly off to charge their badge once the whole refill is done, see below.
                if (drawn.CardType == CardType.Reaction)
                {
                    pendingReactionCards.Add(drawn);
                    GameEventBus.FireCardDrawn(drawn);
                    OnCardDrawn?.Invoke(drawn);
                    yield return StartCoroutine(_handLayout.AnimateManualDraw(drawn));
                    continue;
                }

                _gameState.PlayerHand.AddCard(drawn, _config.MaxHandSize);
                GameEventBus.FireCardDrawn(drawn);
                OnCardDrawn?.Invoke(drawn);
                yield return StartCoroutine(_handLayout.AnimateManualDraw(drawn));
            }

            foreach (var card in pendingReactionCards)
                StartCoroutine(AbsorbReactionCard(card));
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

            // The enemy hand fully refills at the start of its turn too, mirroring the player -
            // drawing until its total held resources (hand + already-charged reactions) reach
            // MaxHandSize, re-checked after every card rather than computed once up front - see
            // RefillPlayerHandRoutine's remarks. Unlike the player's version, the enemy charges a
            // drawn Reaction immediately (no deferred "sits in hand, then flies to badge" beat to
            // account for), so the total is always accurate mid-loop with no extra bookkeeping.
            // EnemyAI.PickCard never plays Reaction-type cards, so leaving one in hand would
            // strand it there permanently unplayable.
            while (_gameState.EnemyHand.Count + _gameState.EnemyDeadMansTurnCharges + _gameState.EnemyCounterGaleCharges < _config.MaxHandSize
                   && _gameState.EnemyDeck.Count > 0)
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

            // Keeps playing cards until mana/HP/hand constraints leave nothing playable -
            // mana is the only balancing lever now, so the enemy uses as much of its turn as
            // it can afford rather than stopping after a single card.
            while (true)
            {
                // Playtest-only cheat override (see ForceEnemyCardCheatPanel/ForceEnemyToPlayCard) -
                // bypasses the AI's own selection heuristics and mana-affordability filtering
                // entirely, guaranteeing this exact card is what the enemy plays next.
                CardSO playedCard;
                if (_forcedEnemyCard != null && _gameState.EnemyHand.CardsSO.Contains(_forcedEnemyCard))
                {
                    playedCard = _forcedEnemyCard;
                    _forcedEnemyCard = null;
                }
                else
                {
                    // Mana available for THIS pick must already account for the Dead Man's Turn
                    // surcharge (if pending) the same way CanPlayCard does for the player -
                    // otherwise the AI could pick a card it can no longer actually afford once
                    // the surcharge is added on top of its mana cost.
                    playedCard = _enemyAI.PickCard(
                        _gameState.EnemyHand.CardsSO,
                        _gameState.EnemyMana - _gameState.EnemyNextCardManaSurcharge,
                        _gameState.EnemyHP,
                        comboPrimed: _gameState.EnemyComboStackCount > 0 && _gameState.EnemyActiveComboCard != null,
                        playerDeadMansTurnCharges: _gameState.PlayerDeadMansTurnCharges,
                        playerCounterGaleCharges: _gameState.PlayerCounterGaleCharges,
                        selfUnblockable: _gameState.SirenSongActive);
                }

                if (playedCard == null) break;

                bool isAttackCard = playedCard.CardType == CardType.Weapon ||
                                    playedCard.CardType == CardType.Combo  ||
                                    playedCard.CardType == CardType.DOT;
                if (isAttackCard) attackPlayedThisTurn = true;

                yield return StartCoroutine(PlayEnemyCard(playedCard, isAttackCard));

                if (_gameState.IsGameOver()) { FireMatchResult(); yield break; }
            }

            // Siren Song is only meaningful if consumed by an attack the same turn it's cast -
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
            // Mirrors HandleCardPlayed's surcharge handling for the player - taxes only the very
            // next card the enemy plays, whatever it turns out to be, then clears.
            int surcharge = _gameState.EnemyNextCardManaSurcharge;
            _gameState.SpendEnemyMana(playedCard.ManaCost + surcharge);
            if (surcharge > 0) _gameState.ClearEnemyNextCardManaSurcharge();

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
            else
            {
                if (playedCard.CardType == CardType.Action && playedCard.AiPlayBeforeAttack)
                    _combatResolver.ResolveCard(playedCard, DamageTarget.Enemy, _handLayout);

                // No reaction prompt can ever gate a non-attack card, so its presentation is
                // always safe to show right away.
                GameEventBus.FireEnemyCardPresentationReady(playedCard);
            }

            OnHPChanged?.Invoke();
        }

        private IEnumerator ResolveEnemyAttack(CardSO attackCard)
        {
            // Gunpowder Barrel (a Combo primer) and DOT cards apply their entire effect as a
            // side effect of resolving (adding the stack / registering the DOT) rather than
            // returning a damage number for the caller to apply afterward the way Weapon/
            // Combo-ignition cards do - so by the time CombatResolver.ResolveCard returns for
            // one of these, it's already too late to let Dead Man's Turn dodge it. Routed
            // through a dedicated path that checks the reaction BEFORE resolving.
            if ((attackCard.CardType == CardType.Combo && attackCard.ComboStackBonus > 0) ||
                attackCard.CardType == CardType.DOT)
            {
                yield return StartCoroutine(ResolveReactableZeroDamageCard(attackCard));
                yield break;
            }

            // Routed through CombatResolver for every attack type (not just Combo/DOT) so
            // enemy-side gunpowder stacking, damage-over-time tracking, and HP-cost deduction
            // (e.g. The Kraken's self-damage) all actually apply - previously Weapon cards
            // bypassed this and dealt flat attackCard.Damage with no HP cost taken.
            int damage = _combatResolver.ResolveCard(attackCard, DamageTarget.Enemy, _handLayout);

            // Combo primers and DOT applications are already routed above and never reach this
            // point - zero here now only means a genuinely harmless resolution (e.g. Torch with
            // no active combo still deals its base 1, so this shouldn't normally trigger, but is
            // handled defensively).
            if (damage <= 0)
            {
                GameEventBus.FireEnemyCardPresentationReady(attackCard);
                yield break;
            }

            bool isKraken    = attackCard.Id == CardId.Kraken;
            // Consumed the instant it's used to make an attack unblockable - without this, Siren
            // Song's "next attack" would silently keep making EVERY subsequent enemy attack this
            // turn (and beyond, since nothing else clears it once an attack has been played)
            // unblockable, permanently locking the player out of Dead Man's Turn/Counter Gale
            // even with charges available. Skipped entirely for Kraken, which is already
            // unconditionally unblockable on its own - consuming Siren here would burn the charge
            // for no benefit and leave a later, non-Kraken attack this turn blockable again.
            bool sirenConsumed = !isKraken && ConsumeSirenIfActive(DamageTarget.Enemy);
            bool isUnblockable = sirenConsumed || isKraken;
            bool hasDMT        = _gameState.PlayerDeadMansTurnCharges > 0;
            bool hasCounterGale = _gameState.PlayerCounterGaleCharges > 0;

            bool playerHasKraken = _gameState.PlayerHand.CardsSO.Any(c => c.Id == CardId.Kraken);
            if (isKraken && playerHasKraken)
            {
                yield return StartCoroutine(KrakenVsKrakenPrompt(attackCard));
                // Standoff already carries its own dedicated prompt/VFX (KrakenVsKrakenPrompt) -
                // no separate presentation entry to defer here.
                yield break;
            }

            bool canReact = !isUnblockable && (hasDMT || hasCounterGale);

            // The attack's own VFX/SFX (fireball, cannon flash, etc.) must not play until the
            // player has actually made their choice - spawning it earlier would show/sound the
            // attack while the negation prompt is still on screen, before the player has decided
            // anything.
            if (canReact)
                yield return StartCoroutine(ReactionPrompt(attackCard, damage, hasDMT, hasCounterGale));

            GameEventBus.FireEnemyCardPresentationReady(attackCard);

            if (!canReact)
            {
                _gameState.ApplyDamage(DamageTarget.Player, damage);
                GameDebug.Log($"Enemy attack - {attackCard.Name}: {damage} dmg");
            }
        }

        // Gunpowder Barrel (Combo primer) and DOT cards have no immediate damage number - their
        // whole effect IS the side effect CombatResolver.ResolveCard applies. Dead Man's Turn can
        // still dodge them entirely, but only by checking BEFORE that call runs, since there's no
        // way to undo it afterward. Counter Gale never applies here (it reflects damage, and
        // there's none yet to reflect) - matches the "a lone-Dead-Man's-Turn defense has no
        // Counter Gale option" case ReactionPromptHandler already documents.
        private IEnumerator ResolveReactableZeroDamageCard(CardSO attackCard)
        {
            bool sirenConsumed = ConsumeSirenIfActive(DamageTarget.Enemy);
            bool hasDMT        = _gameState.PlayerDeadMansTurnCharges > 0;

            if (sirenConsumed || !hasDMT)
            {
                GameEventBus.FireEnemyCardPresentationReady(attackCard);
                _combatResolver.ResolveCard(attackCard, DamageTarget.Enemy, _handLayout);
                yield break;
            }

            bool usedDMT     = false;
            bool playerChose = false;

            OnShowReactionPrompt?.Invoke(
                attackCard, 0,
                "Dead Man's Turn\n(Avoid entirely - costs 1 HP, next card +1 mana)", () => { usedDMT = true;  playerChose = true; },
                null, null, // Counter Gale never applies to a zero-damage effect
                () => { usedDMT = false; playerChose = true; });

            yield return new WaitUntil(() => playerChose);

            // The attack's own VFX/SFX must not play until the player has actually made their
            // choice - matches ResolveEnemyAttack's own ordering for damage-dealing attacks.
            GameEventBus.FireEnemyCardPresentationReady(attackCard);

            if (usedDMT && _gameState.ConsumeDeadMansTurn())
            {
                _gameState.ApplyDamage(DamageTarget.Player, 1);
                _gameState.AddPlayerNextCardManaSurcharge(1);
                GameDebug.Log($"Dead Man's Turn fired - {attackCard.Name} avoided entirely (-1 HP, next card +1 mana)");
                GameEventBus.FireMatchNote($"Dead Man's Turn - avoided {attackCard.Name} entirely, but it cost 1 HP and your next card costs +1 mana.");
                yield break;
            }

            _combatResolver.ResolveCard(attackCard, DamageTarget.Enemy, _handLayout);
        }

        // Consumes Siren Song's unblockable status the instant it's spent on an attack - the
        // status itself (SirenSongActive) has no other "used up" signal, so without this, one
        // Siren cast would silently make every later attack this turn (and any turn after, since
        // nothing else clears it once an attack has been played) unblockable too.
        private bool ConsumeSirenIfActive(DamageTarget caster)
        {
            if (!_gameState.SirenSongActive) return false;
            _gameState.ClearSiren(caster);
            return true;
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
            // Preview-only math - must match CombatResolver.ResolveCounterGale's own formula.
            // Not calling that method here since it also logs, which would misfire if the
            // player ends up picking a different option than the one being previewed.
            int reflectedPreview = Mathf.FloorToInt(damage * 0.5f);

            string negateLabel = hasDMT
                ? "Dead Man's Turn\n(Take 0 damage - costs 1 HP, next card +1 mana)"
                : null;
            string counterGaleLabel = hasCounterGale
                ? $"Counter Gale\n(Take {damage}, deal {reflectedPreview} back - refund 1 mana, draw a card)"
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
                GameDebug.Log("Dead Man's Turn fired - attack negated (-1 HP, next card +1 mana)");
                GameEventBus.FireMatchNote("Dead Man's Turn - attack negated, but it cost 1 HP and your next card costs +1 mana.");
            }
            else if (usedDMT == false && _gameState.ConsumeCounterGale())
            {
                int reflected = _combatResolver.ResolveCounterGale(damage);
                _gameState.ApplyDamage(DamageTarget.Enemy, reflected);
                _gameState.ApplyDamage(DamageTarget.Player, damage);
                _gameState.RefundPlayerMana(1);
                bool drewCard = TryDrawCardSecondary();
                string drawSuffix = drewCard ? ", drew a card" : ", hand full - no card drawn";
                GameDebug.Log($"Counter Gale fired - reflected {reflected}, took {damage}, refunded 1 mana{drawSuffix}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ChargeReaction(CardSO card, DamageTarget side = DamageTarget.Player)
        {
            if (card.Id == CardId.DeadMansTurn)   _gameState.AddDeadMansTurnCharge(side);
            else if (card.Id == CardId.CounterGale) _gameState.AddCounterGaleCharge(side);
        }

        // Single-draw path (TryDrawCard / TryDrawCardSecondary): the reaction card deals into
        // the hand exactly like a normal card, then absorbs into its badge on its own - there's
        // no "rest of the draw" to wait for since it was the only card drawn this call.
        private IEnumerator DrawReactionCardThenAbsorb(CardSO card)
        {
            GameEventBus.FireCardDrawn(card);
            yield return StartCoroutine(_handLayout.AnimateManualDraw(card));
            yield return StartCoroutine(AbsorbReactionCard(card));
        }

        // Waits the post-draw beat, then flies the already-dealt reaction card to its badge and
        // applies the actual charge the instant it vanishes. Multiple pending reaction cards each
        // run their own instance of this in parallel, so they all move together.
        private IEnumerator AbsorbReactionCard(CardSO card)
        {
            yield return new WaitForSeconds(ReactionAbsorbDelay);
            yield return StartCoroutine(_handLayout.AnimateReactionAbsorb(card, () => ChargeReaction(card)));
        }

        private void FireMatchResult()
        {
            // Idempotent - IsGameOver() stays true for the rest of the match once tripped (HP
            // doesn't come back from 0, decks don't refill), so without this guard any of the
            // several call sites that check it (card play, enemy attack, cheat panel) could
            // re-fire a second Win/Loss event and double-grant match rewards.
            if (_gameState.MatchOver) return;
            _gameState.SetMatchOver();

            Winner winner = _gameState.GetWinner();
            GameDebug.Log($"Match over - winner: {winner} " +
                $"(PlayerHP: {_gameState.PlayerHP}, PlayerDeck: {_gameState.PlayerDeck.Count}, PlayerHand: {_gameState.PlayerHand.Count}, " +
                $"EnemyHP: {_gameState.EnemyHP}, EnemyDeck: {_gameState.EnemyDeck.Count}, EnemyHand: {_gameState.EnemyHand.Count})");

            if (winner == Winner.Player)
                GameEventBus.FireMatchWin();
            else
                GameEventBus.FireMatchLoss();
        }
    }
}