using System.Collections.Generic;

namespace ThroneOfTides.Core
{
    public interface ICardEffectContext
    {
        int PlayerHP        { get; }
        int EnemyHP         { get; }
        int PlayerDeckCount { get; }
        int EnemyDeckCount  { get; }

        void ApplyDamage(DamageTarget target, int amount);
        void HealPlayer(int amount);
        void SetSirenActive();
        void SetDeadMansTurnActive();
        void ApplyDot(DamageTarget target, int damagePerTurn, int turns);
        void AddCardToPlayerHand(ICard card);
        void StealFromEnemyHand();
        void RetrieveFromDiscard(int count);

        IReadOnlyList<ICard> GetEnemyHand();
        IReadOnlyList<ICard> GetPlayerHand();
    }
}