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

        /// Which side played the card that owns this effect — lets effects that benefit
        /// "whoever cast this" (Siren Song, Monkey Grab) work correctly for either caster.
        DamageTarget Caster { get; }

        /// The target the player explicitly chose via a targeting prompt (see
        /// CardSO.RequiresTargetSelection). Null for cards that don't require target selection.
        DamageTarget? SelectedTarget { get; }

        void ApplyDamage(DamageTarget target, int amount);
        void HealPlayer(int amount);
        void SetSirenActive();
        void ApplyDot(DamageTarget target, int damagePerTurn, int turns, ShipStatusType source = ShipStatusType.None);
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
        void ClearComboStack(DamageTarget target);

        /// Registers a High Spirits play toward its permanent status icon count (distinct from
        /// AddPlayerMaxMana, which other effects like Treasure Chest also grant mana through).
        void RegisterHighSpiritsPlayed();

        IReadOnlyList<ICard> GetEnemyHand();
        IReadOnlyList<ICard> GetPlayerHand();
    }
}