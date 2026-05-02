namespace ThroneOfTides.Core
{
    public interface ICard
    {
        string   Name             { get; }
        CardType CardType         { get; }
        int      Damage           { get; }
        int      ComboDamage      { get; }
        int      ComboStackBonus  { get; }
        ICard    ComboPartner     { get; }
        int      DotDamagePerTurn { get; }
        int      DotDuration      { get; }
        string   Description      { get; }
    }
}