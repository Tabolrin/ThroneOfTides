// Assets/_Game/2. Scripts/Data/PlayerInventory.cs
using System;
using System.Collections.Generic;
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [Serializable]
    public class PowerUpEntry
    {
        public PowerUpSO PowerUp;
        public int       Count;
    }

    [CreateAssetMenu(menuName = "ThroneOfTides/Data/PlayerInventory")]
    public class PlayerInventory : ScriptableObject
    {
        [Header("Card Collection")]
        [SerializeField] private List<CardSO> _collection = new List<CardSO>();

        [Header("Active Deck")]
        // The deck the player has configured — used by GameBootstrapper at match start
        [SerializeField] private DeckDefinitionSO _playerDeck;

        [Header("Materials")]
        [SerializeField] private int _rum;
        [SerializeField] private int _shipwrecks;

        [Header("Power-Ups")]
        [SerializeField] private List<PowerUpEntry> _powerUps = new List<PowerUpEntry>();

        [Header("Ship Upgrades")]
        [SerializeField] private int _hullReinforcementLevel;  // MaxHP
        [SerializeField] private int _expandedCargoHoldLevel;  // MaxStorage
        [SerializeField] private int _manaCrystalLevel;        // MaxMana

        // ── Properties ────────────────────────────────────────────────────────

        public IReadOnlyList<CardSO>      Collection             => _collection.AsReadOnly();
        public DeckDefinitionSO           PlayerDeck             => _playerDeck;
        public int                        Rum                    => _rum;
        public int                        Shipwrecks             => _shipwrecks;
        public IReadOnlyList<PowerUpEntry> PowerUps              => _powerUps.AsReadOnly();
        public int                        HullReinforcementLevel => _hullReinforcementLevel;
        public int                        ExpandedCargoHoldLevel => _expandedCargoHoldLevel;
        public int                        ManaCrystalLevel       => _manaCrystalLevel;

        // ── Collection ────────────────────────────────────────────────────────

        public void AddCards(List<CardSO> cards) => _collection.AddRange(cards);

        // Returns how many copies of this card the player owns
        public int CountOwned(CardSO card)
        {
            int count = 0;
            foreach (var c in _collection)
                if (c == card) count++;
            return count;
        }

        // ── Materials ─────────────────────────────────────────────────────────

        public void AddMaterials(int rum, int shipwrecks)
        {
            _rum        += rum;
            _shipwrecks += shipwrecks;
        }

        public bool CanAfford(int rum, int shipwrecks) =>
            _rum >= rum && _shipwrecks >= shipwrecks;

        public bool SpendMaterials(int rum, int shipwrecks)
        {
            if (!CanAfford(rum, shipwrecks)) return false;
            _rum        -= rum;
            _shipwrecks -= shipwrecks;
            return true;
        }

        // ── Upgrades ──────────────────────────────────────────────────────────

        public int GetUpgradeLevel(UpgradeType type) => type switch
        {
            UpgradeType.MaxHP      => _hullReinforcementLevel,
            UpgradeType.MaxStorage => _expandedCargoHoldLevel,
            UpgradeType.MaxMana    => _manaCrystalLevel,
            _                     => 0
        };

        // Spends materials and increments level — returns false if can't afford or already maxed
        public bool TryPurchaseUpgrade(UpgradeSO upgrade)
        {
            int currentLevel = GetUpgradeLevel(upgrade.Type);
            if (upgrade.IsMaxed(currentLevel)) return false;

            int rum = upgrade.GetRumCostToLevel(currentLevel);
            int sw  = upgrade.GetShipwreckCostToLevel(currentLevel);
            if (!SpendMaterials(rum, sw)) return false;

            IncrementUpgradeLevel(upgrade.Type);
            return true;
        }

        public void IncrementUpgradeLevel(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.MaxHP:      _hullReinforcementLevel++;  break;
                case UpgradeType.MaxStorage: _expandedCargoHoldLevel++; break;
                case UpgradeType.MaxMana:    _manaCrystalLevel++;        break;
            }
        }

        // ── Power-Ups ─────────────────────────────────────────────────────────

        public void AddPowerUp(PowerUpSO powerUp)
        {
            var existing = _powerUps.Find(p => p.PowerUp == powerUp);
            if (existing != null) existing.Count++;
            else _powerUps.Add(new PowerUpEntry { PowerUp = powerUp, Count = 1 });
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        public void Reset()
        {
            _collection.Clear();
            _powerUps.Clear();
            _rum                    = 0;
            _shipwrecks             = 0;
            _hullReinforcementLevel = 0;
            _expandedCargoHoldLevel = 0;
            _manaCrystalLevel       = 0;
        }
    }
}