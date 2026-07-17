// Assets/_Game/2. Scripts/Data/PlayerInventory.cs
using System;
using System.Collections.Generic;
using MoreMountains.Tools;
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
        [SerializeField] private DeckDefinitionSO _playerDeck;

        [Header("Currency")]
        [SerializeField] private int _coins;

        [Header("Power-Ups")]
        [SerializeField] private List<PowerUpEntry> _powerUps = new List<PowerUpEntry>();

        [Header("Ship Upgrades")]
        [SerializeField] private int _hullReinforcementLevel;  // MaxHP
        [SerializeField] private int _expandedCargoHoldLevel;  // MaxStorage
        [SerializeField] private int _manaCrystalLevel;        // MaxMana

        [Header("Save/Load")]
        [Tooltip("Registry used to resolve a saved card collection (stored as CardId) back into real CardSO references on load.")]
        [SerializeField] private CardDatabaseSO _cardDatabase;

        // ── Properties ────────────────────────────────────────────────────────

        public IReadOnlyList<CardSO>      Collection             => _collection.AsReadOnly();
        public DeckDefinitionSO           PlayerDeck             => _playerDeck;
        public int                        Coins                  => _coins;
        public IReadOnlyList<PowerUpEntry> PowerUps              => _powerUps.AsReadOnly();
        public int                        HullReinforcementLevel => _hullReinforcementLevel;
        public int                        ExpandedCargoHoldLevel => _expandedCargoHoldLevel;
        public int                        ManaCrystalLevel       => _manaCrystalLevel;

        // ── Collection ────────────────────────────────────────────────────────

        public void AddCards(List<CardSO> cards)
        {
            _collection.AddRange(cards);
            Save();
        }

        public int CountOwned(CardSO card)
        {
            int count = 0;
            foreach (var c in _collection)
                if (c == card) count++;
            return count;
        }

        // ── Currency ──────────────────────────────────────────────────────────

        public void AddCoins(int amount)
        {
            _coins += amount;
            Save();
        }

        public bool CanAfford(int amount) => _coins >= amount;

        public bool SpendCoins(int amount)
        {
            if (!CanAfford(amount)) return false;
            _coins -= amount;
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

        public bool TryPurchaseUpgrade(UpgradeSO upgrade)
        {
            int currentLevel = GetUpgradeLevel(upgrade.Type);
            if (upgrade.IsMaxed(currentLevel)) return false;

            int cost = upgrade.GetCoinCostToLevel(currentLevel);
            if (!SpendCoins(cost)) return false;

            IncrementUpgradeLevel(upgrade.Type);
            Save();
            return true;
        }

        public void IncrementUpgradeLevel(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.MaxHP:      _hullReinforcementLevel++; break;
                case UpgradeType.MaxStorage: _expandedCargoHoldLevel++; break;
                case UpgradeType.MaxMana:    _manaCrystalLevel++;       break;
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
            _coins                  = 0;
            _hullReinforcementLevel = 0;
            _expandedCargoHoldLevel = 0;
            _manaCrystalLevel       = 0;
        }

        // ── Save/Load ─────────────────────────────────────────────────────────

        private const string SaveFileName   = "player.save";
        private const string SaveFolderName = "ThroneOfTides/";

        public void Save()
        {
            var data = new PlayerInventorySaveData
            {
                Coins                  = _coins,
                HullReinforcementLevel = _hullReinforcementLevel,
                ExpandedCargoHoldLevel = _expandedCargoHoldLevel,
                ManaCrystalLevel       = _manaCrystalLevel,
            };

            foreach (var card in _collection)
                if (card != null && card.Id != CardId.None)
                    data.CollectionCardIds.Add(card.Id);

            MMSaveLoadManager.Save(data, SaveFileName, SaveFolderName);
        }

        // Call once at game startup, before anything reads this inventory.
        public void LoadFromDisk()
        {
            var data = (PlayerInventorySaveData)MMSaveLoadManager.Load(typeof(PlayerInventorySaveData), SaveFileName, SaveFolderName);
            if (data == null) return;

            _coins                  = data.Coins;
            _hullReinforcementLevel = data.HullReinforcementLevel;
            _expandedCargoHoldLevel = data.ExpandedCargoHoldLevel;
            _manaCrystalLevel       = data.ManaCrystalLevel;

            _collection.Clear();
            if (_cardDatabase != null)
            {
                foreach (var id in data.CollectionCardIds)
                {
                    var card = _cardDatabase.GetById(id);
                    if (card != null) _collection.Add(card);
                }
            }
        }
    }
}