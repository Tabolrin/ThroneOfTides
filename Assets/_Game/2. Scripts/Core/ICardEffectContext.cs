// Assets/_Game/2. Scripts/Core/ICardEffectContext.cs
using System.Collections.Generic;

namespace ThroneOfTides.Core
{
    public interface ICardEffectContext
    {
        int PlayerHP        { get; }
        int EnemyHP         { get; }
        int PlayerDeckCount { get; }
        int EnemyDeckCount  { get; }
        int PlayerMana      { get; }
        int PlayerMaxMana   { get; }
        int EnemyMana       { get; }

        void ApplyDamage(DamageTarget target, int amount);
        void HealPlayer(int amount);
        void SetSirenActive();
        void ApplyDot(DamageTarget target, int damagePerTurn, int turns);
        void AddCardToPlayerHand(ICard card);
        void StealFromEnemyHand();
        void RetrieveFromDiscard(int count);
        void SpendPlayerMana(int amount);
        void AddPlayerMaxMana(int amount);
        void StealEnemyMana(int amount);
        void AddDeadMansTurnCharge();
        void AddBloodForBloodCharge();
        void ReturnFromSnapshot(int count);
        void DrawOneCard();

        IReadOnlyList<ICard> GetEnemyHand();
        IReadOnlyList<ICard> GetPlayerHand();
    }
}