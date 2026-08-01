// Assets/_Game/2. Scripts/Core/ICardEffectContext.cs
using System.Collections.Generic;

namespace ThroneOfTides.Core
{
    // Segregated capability interfaces, grouped by what most individual card effects actually
    // touch. Single-purpose effects (e.g. Rum, Siren Song) declare ActionEffectSO<TCapability>
    // with just the one they need — see ActionEffectSO.cs. ICardEffectContext below composes
    // all of them for the small number of effects that are genuinely multi-concern (Tidal Wave,
    // Treasure Chest, High Spirits) and for CardEffectContext, the single concrete implementer.

    public interface IDamageEffects
    {
        int PlayerHP { get; }
        int EnemyHP  { get; }
        void ApplyDamage(DamageTarget target, int amount);
    }

    public interface IHealEffects
    {
        void HealPlayer(int amount);
    }

    public interface IManaEffects
    {
        int PlayerMana    { get; }
        int PlayerMaxMana { get; }
        int EnemyMana     { get; }
        void SpendPlayerMana(int amount);
        void AddPlayerMaxMana(int amount);
        void StealEnemyMana(int amount);
    }

    public interface IHandEffects
    {
        void AddCardToPlayerHand(ICard card);
        void StealFromEnemyHand();
        void DrawOneCard();
        IReadOnlyList<ICard> GetEnemyHand();
        IReadOnlyList<ICard> GetPlayerHand();

        /// Discards one random card from whoever did NOT cast this card's hand entirely (not
        /// stolen into the caster's hand — see StealFromEnemyHand for that). Used by Chain Shot.
        void DiscardRandomFromOpponentHand();
    }

    public interface IDiscardEffects
    {
        int PlayerDeckCount { get; }
        int EnemyDeckCount  { get; }
        void RetrieveFromDiscard(int count);
        void ReturnFromSnapshot(int count);
    }

    public interface IStatusEffects
    {
        void SetSirenActive();
        void ApplyDot(DamageTarget target, int damagePerTurn, int turns, ShipStatusType source = ShipStatusType.None);
        void ClearComboStack(DamageTarget target);

        /// Registers a High Spirits play toward its permanent status icon count (distinct from
        /// AddPlayerMaxMana, which other effects like Treasure Chest also grant mana through).
        void RegisterHighSpiritsPlayed();
    }

    public interface IReactionChargeEffects
    {
        void AddDeadMansTurnCharge();
        void AddCounterGaleCharge();
    }

    public interface ITargetSelectionContext
    {
        /// The target the player explicitly chose via a targeting prompt (see
        /// CardSO.RequiresTargetSelection). Null for cards that don't require target selection.
        DamageTarget? SelectedTarget { get; }
    }

    public interface ICardEffectContext :
        IDamageEffects, IHealEffects, IManaEffects, IHandEffects,
        IDiscardEffects, IStatusEffects, IReactionChargeEffects, ITargetSelectionContext
    {
        /// Which side played the card that owns this effect — lets effects that benefit
        /// "whoever cast this" (Siren Song, Monkey Grab) work correctly for either caster.
        DamageTarget Caster { get; }
    }
}