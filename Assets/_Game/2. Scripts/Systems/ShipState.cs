// Assets/_Game/2. Scripts/Systems/ShipState.cs
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems
{
    // Per-ship HP/Mana/Combo state. GameState holds one instance for Player and one for Enemy
    // (via GetSide(DamageTarget)) instead of duplicating this math across mirrored
    // Player*/Enemy* methods. Pure state + clamping — event firing and card-specific damage
    // formulas stay in GameState, which already owns that policy.
    public class ShipState
    {
        public int HP      { get; private set; }
        public int MaxHP   { get; private set; }
        public int Mana    { get; private set; }
        public int MaxMana { get; private set; }

        public int    ComboStackCount { get; private set; }
        public CardSO ActiveComboCard { get; private set; }

        public ShipState(int startingHP, int startingMaxMana)
        {
            HP = MaxHP = startingHP;
            Mana = MaxMana = startingMaxMana;
        }

        // ── HP ────────────────────────────────────────────────────────────────

        public void ApplyDamage(int amount) => HP = Mathf.Max(0, HP - amount);
        public void Heal(int amount)        => HP = Mathf.Min(HP + amount, MaxHP);

        public void SetMaxHP(int amount)
        {
            MaxHP = Mathf.Max(1, amount);
            HP    = Mathf.Min(HP, MaxHP);
        }

        public void CheatAddHP(int amount) => HP = Mathf.Clamp(HP + amount, 0, MaxHP);

        // ── Mana ──────────────────────────────────────────────────────────────

        public void ResetMana() => Mana = MaxMana;

        public bool SpendMana(int amount)
        {
            if (Mana < amount) return false;
            Mana -= amount;
            return true;
        }

        public void AddMaxMana(int amount)
        {
            MaxMana += amount;
            Mana    += amount;
        }

        public void SetMaxMana(int amount)
        {
            MaxMana = Mathf.Max(0, amount);
            Mana    = Mathf.Min(Mana, MaxMana);
        }

        public void CheatAddMana(int amount) => Mana = Mathf.Clamp(Mana + amount, 0, MaxMana);

        // Mana floor of 1 on the sender — cannot be fully drained by Stolen Wind/Essence
        // Plunder. Returns the amount actually transferred (may be less than requested, or 0).
        public int TransferManaTo(ShipState receiver, int amount)
        {
            int actual = Mathf.Min(amount, Mana - 1);
            if (actual <= 0) return 0;
            Mana           -= actual;
            receiver.Mana  += actual;
            return actual;
        }

        // ── Combo ─────────────────────────────────────────────────────────────

        public void IncrementCombo(CardSO card)
        {
            ActiveComboCard = card;
            ComboStackCount++;
        }

        public void ResetCombo()
        {
            ComboStackCount = 0;
            ActiveComboCard = null;
        }
    }
}
