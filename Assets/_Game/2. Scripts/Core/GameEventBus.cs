using System;

namespace ThroneOfTides.Core
{
    public static class GameEventBus
    {
        // Turn events
        public static event Action<TurnPhase> OnTurnPhaseChanged;

        // Card events
        public static event Action<ICard> OnCardDrawn;
        public static event Action<ICard> OnCardPlayed;
        public static event Action<ICard> OnCardPlayAccepted;
        public static event Action<ICard> OnPlayerCardRemoved;
        public static event Action<ICard> OnEnemyCardPlayed;

        // Animation sync - set by TurnCoordinator, invoked by App layer
        public static Action OnEnemyCardAnimationComplete;

        // Combat events
        public static event Action<DamageTarget, int> OnDamageDealt;
        public static event Action<int>               OnHPChanged;
        public static event Action                    OnComboResolved;
        public static event Action<int>               OnComboStackChanged;

        // DOT events
        public static event Action<DotEffect> OnDOTApplied;
        public static event Action<DotEffect> OnDOTTick;

        // Action card events
        public static event Action       OnDeadMansTurnPrompt;
        public static event Action<bool> OnDeadMansTurnResolved;
        public static event Action       OnPowerUpUsed;

        // Match events
        public static event Action OnMatchWin;
        public static event Action OnMatchLoss;

        public static void FireTurnPhaseChanged(TurnPhase phase)            => OnTurnPhaseChanged?.Invoke(phase);
        public static void FireCardDrawn(ICard card)                        => OnCardDrawn?.Invoke(card);
        public static void FireCardPlayed(ICard card)                       => OnCardPlayed?.Invoke(card);
        public static void FireCardPlayAccepted(ICard card)                 => OnCardPlayAccepted?.Invoke(card);
        public static void FirePlayerCardRemoved(ICard card)                => OnPlayerCardRemoved?.Invoke(card);
        public static void FireEnemyCardPlayed(ICard card)                  => OnEnemyCardPlayed?.Invoke(card);
        public static void FireDamageDealt(DamageTarget target, int amount) => OnDamageDealt?.Invoke(target, amount);
        public static void FireHPChanged(int hp)                            => OnHPChanged?.Invoke(hp);
        public static void FireComboResolved()                              => OnComboResolved?.Invoke();
        public static void FireComboStackChanged(int count)                 => OnComboStackChanged?.Invoke(count);
        public static void FireDOTApplied(DotEffect effect)                 => OnDOTApplied?.Invoke(effect);
        public static void FireDOTTick(DotEffect effect)                    => OnDOTTick?.Invoke(effect);
        public static void FireDeadMansTurnPrompt()                         => OnDeadMansTurnPrompt?.Invoke();
        public static void FireDeadMansTurnResolved(bool negated)           => OnDeadMansTurnResolved?.Invoke(negated);
        public static void FirePowerUpUsed()                                => OnPowerUpUsed?.Invoke();
        public static void FireMatchWin()                                   => OnMatchWin?.Invoke();
        public static void FireMatchLoss()                                  => OnMatchLoss?.Invoke();

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
            OnMatchWin                   = null;
            OnMatchLoss                  = null;
        }
    }
}