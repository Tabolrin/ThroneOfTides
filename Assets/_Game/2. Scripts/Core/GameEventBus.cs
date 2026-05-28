using System;

// Assembly: ThroneOfTides.Core
// Location: Scripts/Core/GameEventBus.cs
// Pattern: Static event bus — all systems publish and subscribe here.
//          No direct cross-system references required.

namespace ThroneOfTides.Core
{
    public static class GameEventBus
    {
        // ── Turn ──────────────────────────────────────────────────────────────
        public static event Action<TurnPhase> OnTurnPhaseChanged;

        // ── Card ──────────────────────────────────────────────────────────────
        public static event Action<ICard> OnCardDrawn;
        public static event Action<ICard> OnCardPlayed;
        public static event Action<ICard> OnCardPlayAccepted;
        public static event Action<ICard> OnPlayerCardRemoved;
        public static event Action<ICard> OnEnemyCardPlayed;

        // Invoked by the App layer once the enemy card play animation completes.
        // Set by TurnCoordinator before firing OnEnemyCardPlayed.
        public static Action OnEnemyCardAnimationComplete;

        // ── Combat ────────────────────────────────────────────────────────────
        public static event Action<DamageTarget, int> OnDamageDealt;
        public static event Action<int>               OnHPChanged;
        public static event Action                    OnComboResolved;
        public static event Action<int>               OnComboStackChanged;

        // ── DOT ───────────────────────────────────────────────────────────────
        public static event Action<DotEffect> OnDOTApplied;
        public static event Action<DotEffect> OnDOTTick;

        // ── Action Cards ──────────────────────────────────────────────────────
        public static event Action       OnDeadMansTurnPrompt;
        public static event Action<bool> OnDeadMansTurnResolved;
        public static event Action       OnPowerUpUsed;

        // ── Creature VFX Sync ─────────────────────────────────────────────────
        // Fired by CardVFXHandler at the precise animation moment damage applies.
        // Allows TurnCoordinator to stay decoupled from VFX timing.
        public static event Action OnKrakenAttackMoment;

        // Fired by CardVFXHandler once the siren is fully risen and visible.
        // TurnCoordinator waits on this before marking SirenSong as active in GameState.
        public static event Action OnSirenSongActive;

        // ── Match ─────────────────────────────────────────────────────────────
        public static event Action OnMatchWin;
        public static event Action OnMatchLoss;

        // ── Fire Methods ──────────────────────────────────────────────────────
        public static void FireTurnPhaseChanged(TurnPhase phase)             => OnTurnPhaseChanged?.Invoke(phase);
        public static void FireCardDrawn(ICard card)                         => OnCardDrawn?.Invoke(card);
        public static void FireCardPlayed(ICard card)                        => OnCardPlayed?.Invoke(card);
        public static void FireCardPlayAccepted(ICard card)                  => OnCardPlayAccepted?.Invoke(card);
        public static void FirePlayerCardRemoved(ICard card)                 => OnPlayerCardRemoved?.Invoke(card);
        public static void FireEnemyCardPlayed(ICard card)                   => OnEnemyCardPlayed?.Invoke(card);
        public static void FireDamageDealt(DamageTarget target, int amount)  => OnDamageDealt?.Invoke(target, amount);
        public static void FireHPChanged(int hp)                             => OnHPChanged?.Invoke(hp);
        public static void FireComboResolved()                               => OnComboResolved?.Invoke();
        public static void FireComboStackChanged(int count)                  => OnComboStackChanged?.Invoke(count);
        public static void FireDOTApplied(DotEffect effect)                  => OnDOTApplied?.Invoke(effect);
        public static void FireDOTTick(DotEffect effect)                     => OnDOTTick?.Invoke(effect);
        public static void FireDeadMansTurnPrompt()                          => OnDeadMansTurnPrompt?.Invoke();
        public static void FireDeadMansTurnResolved(bool negated)            => OnDeadMansTurnResolved?.Invoke(negated);
        public static void FirePowerUpUsed()                                 => OnPowerUpUsed?.Invoke();
        public static void FireKrakenAttackMoment()                          => OnKrakenAttackMoment?.Invoke();
        public static void FireSirenSongActive()                             => OnSirenSongActive?.Invoke();
        public static void FireMatchWin()                                    => OnMatchWin?.Invoke();
        public static void FireMatchLoss()                                   => OnMatchLoss?.Invoke();

        // Unsubscribes all listeners — call on scene unload to prevent stale references.
        public static void ClearAllListeners()
        {
            OnTurnPhaseChanged           = null;
            OnCardDrawn                  = null;
            OnCardPlayed                 = null;
            OnCardPlayAccepted           = null;
            OnPlayerCardRemoved          = null;
            OnEnemyCardPlayed            = null;
            OnEnemyCardAnimationComplete = null;
            OnDamageDealt                = null;
            OnHPChanged                  = null;
            OnComboResolved              = null;
            OnComboStackChanged          = null;
            OnDOTApplied                 = null;
            OnDOTTick                    = null;
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