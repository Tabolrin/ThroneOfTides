// Assets/_Game/2. Scripts/Core/GameEventBus.cs
using System;
using System.Collections.Generic;

namespace ThroneOfTides.Core
{
    public static class GameEventBus
    {
        // ── Turn ──────────────────────────────────────────────────────────────
        public static event Action<TurnPhase> OnTurnPhaseChanged;

        // ── Card ──────────────────────────────────────────────────────────────
        public static event Action<ICard> OnCardDrawn;
        public static event Action<ICard> OnCardPlayed;
        // Fires the instant a play is irrevocably committed (mana spent), before any target
        // selection prompt — CardView listens for this to know it's leaving the hand, so it
        // doesn't snap back to hand while a target-selection card is still awaiting the
        // player's choice. Fires for every accepted play, target-selection or not.
        public static event Action<ICard> OnCardCommitted;
        // Target is null for normally-targeted cards (inferred caster/opponent); non-null when
        // the player explicitly chose a target ship via a targeting prompt (e.g. Tidal Wave).
        // Fires once the effect actually resolves — for a target-selection card, that's after
        // the prompt is answered, not at commit time — so VFX spawners get the real target.
        public static event Action<ICard, DamageTarget?> OnCardPlayAccepted;
        public static event Action<ICard> OnPlayerCardRemoved;
        public static event Action<ICard, DamageTarget?> OnEnemyCardPlayed;
        public static event Action       OnEnemyCardAnimationComplete;

        // Fires when an effect (e.g. Recon Parrot) reveals the enemy's hand — EnemyHandRevealPanel
        // displays the given cards until the player dismisses it.
        public static event Action<IReadOnlyList<ICard>> OnEnemyHandRevealed;

        // ── Combat ────────────────────────────────────────────────────────────
        public static event Action<DamageTarget, int> OnDamageDealt;
        public static event Action<DamageTarget, int> OnHealApplied;
        public static event Action<int>               OnHPChanged;
        public static event Action                    OnComboResolved;
        public static event Action<DamageTarget, int> OnComboStackChanged;

        // ── DOT ───────────────────────────────────────────────────────────────
        public static event Action<DotEffect> OnDOTApplied;
        public static event Action<DotEffect> OnDOTTick;

        // ── Ship Status (persistent per-ship indicators: Gunpowder, DOT sources, buffs) ──────
        // Count semantics: 0 = inactive (hide), >0 = active with that badge value.
        public static event Action<ShipStatusType, DamageTarget, int> OnShipStatusCountChanged;

        // ── Mana ──────────────────────────────────────────────────────────────
        public static event Action<int, int> OnPlayerManaChanged; // current, max
        public static event Action<int, int> OnEnemyManaChanged;  // current, max

        // ── Reactions ─────────────────────────────────────────────────────────
        // ReactionType distinguishes DMT from BFB so subscribers can update
        // the correct charge indicator without needing to query GameState directly.
        public static event Action<ReactionType, DamageTarget, int> OnReactionCharged; // type, side, charges remaining
        public static event Action<ReactionType, DamageTarget>      OnReactionFired;   // type, side consumed

        // ── Action Cards ──────────────────────────────────────────────────────
        public static event Action       OnDeadMansTurnPrompt;
        public static event Action<bool> OnDeadMansTurnResolved;
        public static event Action       OnPowerUpUsed;

        // ── Creature VFX Sync ─────────────────────────────────────────────────
        public static event Action OnKrakenAttackMoment;
        public static event Action OnSirenSongActive;

        // ── Match ─────────────────────────────────────────────────────────────
        public static event Action OnMatchWin;
        public static event Action OnMatchLoss;

        // ── Fire Methods ──────────────────────────────────────────────────────
        public static void FireTurnPhaseChanged(TurnPhase phase)             => OnTurnPhaseChanged?.Invoke(phase);
        public static void FireCardDrawn(ICard card)                         => OnCardDrawn?.Invoke(card);
        public static void FireCardPlayed(ICard card)                        => OnCardPlayed?.Invoke(card);
        public static void FireCardCommitted(ICard card)                     => OnCardCommitted?.Invoke(card);
        public static void FireCardPlayAccepted(ICard card, DamageTarget? target = null) => OnCardPlayAccepted?.Invoke(card, target);
        public static void FirePlayerCardRemoved(ICard card)                 => OnPlayerCardRemoved?.Invoke(card);
        public static void FireEnemyCardPlayed(ICard card, DamageTarget? target = null)  => OnEnemyCardPlayed?.Invoke(card, target);
        public static void FireEnemyCardAnimationComplete()                  => OnEnemyCardAnimationComplete?.Invoke();
        public static void FireEnemyHandRevealed(IReadOnlyList<ICard> cards) => OnEnemyHandRevealed?.Invoke(cards);
        public static void FireDamageDealt(DamageTarget target, int amount)  => OnDamageDealt?.Invoke(target, amount);
        public static void FireHealApplied(DamageTarget target, int amount) => OnHealApplied?.Invoke(target, amount);
        public static void FireHPChanged(int hp)                             => OnHPChanged?.Invoke(hp);
        public static void FireComboResolved()                               => OnComboResolved?.Invoke();
        public static void FireComboStackChanged(DamageTarget side, int count) => OnComboStackChanged?.Invoke(side, count);
        public static void FireDOTApplied(DotEffect effect)                  => OnDOTApplied?.Invoke(effect);
        public static void FireDOTTick(DotEffect effect)                     => OnDOTTick?.Invoke(effect);
        public static void FireShipStatusCountChanged(ShipStatusType type, DamageTarget ship, int count)
            => OnShipStatusCountChanged?.Invoke(type, ship, count);
        public static void FirePlayerManaChanged(int current, int max)       => OnPlayerManaChanged?.Invoke(current, max);
        public static void FireEnemyManaChanged(int current, int max)        => OnEnemyManaChanged?.Invoke(current, max);
        public static void FireReactionCharged(ReactionType type, DamageTarget side, int charges) => OnReactionCharged?.Invoke(type, side, charges);
        public static void FireReactionFired(ReactionType type, DamageTarget side)                => OnReactionFired?.Invoke(type, side);
        public static void FireDeadMansTurnPrompt()                          => OnDeadMansTurnPrompt?.Invoke();
        public static void FireDeadMansTurnResolved(bool negated)            => OnDeadMansTurnResolved?.Invoke(negated);
        public static void FirePowerUpUsed()                                 => OnPowerUpUsed?.Invoke();
        public static void FireKrakenAttackMoment()                          => OnKrakenAttackMoment?.Invoke();
        public static void FireSirenSongActive()                             => OnSirenSongActive?.Invoke();
        public static void FireMatchWin()                                    => OnMatchWin?.Invoke();
        public static void FireMatchLoss()                                   => OnMatchLoss?.Invoke();

        public static void ClearAllListeners()
        {
            OnTurnPhaseChanged           = null;
            OnCardDrawn                  = null;
            OnCardPlayed                 = null;
            OnCardCommitted              = null;
            OnCardPlayAccepted           = null;
            OnPlayerCardRemoved          = null;
            OnEnemyCardPlayed            = null;
            OnEnemyCardAnimationComplete = null;
            OnEnemyHandRevealed          = null;
            OnDamageDealt                = null;
            OnHealApplied                = null;
            OnHPChanged                  = null;
            OnComboResolved              = null;
            OnComboStackChanged          = null;
            OnDOTApplied                 = null;
            OnDOTTick                    = null;
            OnShipStatusCountChanged     = null;
            OnPlayerManaChanged          = null;
            OnEnemyManaChanged           = null;
            OnReactionCharged            = null;
            OnReactionFired              = null;
            OnDeadMansTurnPrompt         = null;
            OnDeadMansTurnResolved       = null;
            OnPowerUpUsed                = null;
            OnKrakenAttackMoment         = null;
            OnSirenSongActive            = null;
            OnMatchWin                   = null;
            OnMatchLoss                  = null;
        }
    }
}