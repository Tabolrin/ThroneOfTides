using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [Serializable]
    public class PowerUpEntry
    {
        public PowerUpSO PowerUp;
        public int       Count;
    }

    // Runtime persistent inventory - lives as a ScriptableObject asset
    // Note: persists between editor sessions but resets in builds - save system needed post-playtest
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/PlayerInventory")]
    public class PlayerInventory : ScriptableObject
    {
        [Header("Card Collection")]
        [SerializeField] private List<CardSO> _collection = new List<CardSO>();

        [Header("Materials")]
        [SerializeField] private int _rum;
        [SerializeField] private int _shipwrecks;

        [Header("Power-Ups")]
        [SerializeField] private List<PowerUpEntry> _powerUps = new List<PowerUpEntry>();

        [Header("Ship Upgrades")]
        [SerializeField] private int _hullReinforcementLevel;
        [SerializeField] private int _expandedCargoHoldLevel;

        public IReadOnlyList<CardSO>      Collection               => _collection.AsReadOnly();
        public int                         Rum                      => _rum;
        public int                         Shipwrecks               => _shipwrecks;
        public IReadOnlyList<PowerUpEntry> PowerUps                 => _powerUps.AsReadOnly();
        public int                         HullReinforcementLevel   => _hullReinforcementLevel;
        public int                         ExpandedCargoHoldLevel   => _expandedCargoHoldLevel;

        public void AddCards(List<CardSO> cards)
        {
            _collection.AddRange(cards);
        }

        public void AddMaterials(int rum, int shipwrecks)
        {
            _rum        += rum;
            _shipwrecks += shipwrecks;
        }

        public void AddPowerUp(PowerUpSO powerUp)
        {
            var existing = _powerUps.Find(p => p.PowerUp == powerUp);
            if (existing != null)
                existing.Count++;
            else
                _powerUps.Add(new PowerUpEntry { PowerUp = powerUp, Count = 1 });
        }

        public bool SpendMaterials(int rum, int shipwrecks)
        {
            if (_rum < rum || _shipwrecks < shipwrecks) return false;
            _rum        -= rum;
            _shipwrecks -= shipwrecks;
            return true;
        }

        // For testing - resets all inventory data
        public void Reset()
        {
            _collection.Clear();
            _powerUps.Clear();
            _rum                     = 0;
            _shipwrecks              = 0;
            _hullReinforcementLevel  = 0;
            _expandedCargoHoldLevel  = 0;
        }
    }
}