using System.Collections.Generic;

namespace ThroneOfTides.Core
{
    // In Core so Data assembly can reference it without touching Systems
    public interface ICardEffectContext
    {
        int PlayerHP        { get; }
        int EnemyHP         { get; }
        int PlayerDeckCount { get; }
        int EnemyDeckCount  { get; }

        void ApplyDamage(DamageTarget target, int amount);
        void HealPlayer(int amount);
        void SetSirenActive();
        void ApplyDot(DamageTarget target, int damagePerTurn, int turns);

        // Returns ICard so Core interface stays independent of Data assembly
        IReadOnlyList<ICard> GetEnemyHand();
        IReadOnlyList<ICard> GetPlayerHand();
    }
}