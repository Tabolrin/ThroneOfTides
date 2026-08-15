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

        [Tooltip("The collection a Reset (see OptionsPanel's 'Reset Player Data') restores. " +
                 "_collection is mutated at runtime and persisted over the top of itself - " +
                 "without a separate untouched baseline, there'd be nothing left to reset back to.")]
        [SerializeField] private List<CardSO> _startingCollection = new List<CardSO>();

        [Tooltip("A dedicated DeckDefinitionSO asset used ONLY as the Reset template - never " +
                 "assigned as _playerDeck, an enemy deck, or a build-profile override, and never " +
                 "written to by any code path. Deliberately a separate asset (not an embedded " +
                 "list here) rather than a copy of _playerDeck's own composition: PortDeckEditor " +
                 "mutates _playerDeck's Cards list directly and in-memory, and Unity does not " +
                 "revert ScriptableObject asset edits made in Play Mode the way it does scene " +
                 "objects - editing the deck while testing in the Editor can permanently corrupt " +
                 "whatever asset _playerDeck points at. Keeping the template on a wholly separate " +
                 "asset that nothing ever edits means Reset can always restore the real balanced " +
                 "starting deck, even after that corruption.")]
        [SerializeField] private DeckDefinitionSO _starterDeckTemplate;

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

        // Fired whenever Coins actually changes - lets a live HUD (e.g. the Match scene's coin
        // counter) reflect Treasure Chest/Kraken/upgrade-purchase changes without polling.
        public event Action<int> OnCoinsChanged;

        public IReadOnlyList<CardSO>      Collection             => _collection.AsReadOnly();
        public DeckDefinitionSO           PlayerDeck             => _playerDeck;
        public int                        Coins                  => _coins;
        public IReadOnlyList<PowerUpEntry> PowerUps              => _powerUps.AsReadOnly();
        public int                        HullReinforcementLevel => _hullReinforcementLevel;
        public int                        ExpandedCargoHoldLevel => _expandedCargoHoldLevel;
        public int                        ManaCrystalLevel       => _manaCrystalLevel;

        // ── Collection ────────────────────────────────────────────────────────

        // Ownership is per card TYPE, not per copy - the Port deck editor lets a player add as
        // many copies of an owned card as they want (bounded by storage and MaxCopiesInDeck),
        // it's not limited by how many times they happen to own that card. Adding an
        // already-owned reward card again (e.g. a duplicate future reward) is a no-op rather
        // than an accumulating duplicate entry.
        public void AddCards(List<CardSO> cards)
        {
            foreach (var card in cards)
                if (card != null && !_collection.Contains(card))
                    _collection.Add(card);
            Save();
        }

        public bool IsUnlocked(CardSO card) => _collection.Contains(card);

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
            OnCoinsChanged?.Invoke(_coins);
            Save();
        }

        public bool CanAfford(int amount) => _coins >= amount;

        public bool SpendCoins(int amount)
        {
            if (!CanAfford(amount)) return false;
            _coins -= amount;
            OnCoinsChanged?.Invoke(_coins);
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

        // Playtest-only "clean slate" - see OptionsPanel's Reset Player Data button. Restores
        // the collection and deck to their designer-authored starting state (not empty - an
        // empty deck/collection would softlock deck-building) and wipes everything earned since.
        public void Reset()
        {
            _collection.Clear();
            _collection.AddRange(_startingCollection);

            if (_playerDeck != null && _starterDeckTemplate != null)
            {
                _playerDeck.Cards.Clear();
                _playerDeck.Cards.AddRange(_starterDeckTemplate.Cards);
            }

            _powerUps.Clear();
            _coins                  = 0;
            _hullReinforcementLevel = 0;
            _expandedCargoHoldLevel = 0;
            _manaCrystalLevel       = 0;
            OnCoinsChanged?.Invoke(_coins);
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

            // The deck editor (PortDeckEditor) mutates _playerDeck.Cards directly at runtime -
            // that in-memory ScriptableObject edit only survives an actual build if it's also
            // captured here. Without this, deck changes appeared to save (AssetDatabase.Save is
            // Editor-only) but silently reverted to the shipped default on every relaunch.
            if (_playerDeck != null)
            {
                foreach (var entry in _playerDeck.Cards)
                    if (entry.Card != null && entry.Card.Id != CardId.None && entry.Count > 0)
                        data.DeckCards.Add(new DeckCardSaveEntry { CardId = entry.Card.Id, Count = entry.Count });
            }

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

                if (_playerDeck != null && data.DeckCards.Count > 0)
                {
                    _playerDeck.Cards.Clear();
                    foreach (var saved in data.DeckCards)
                    {
                        var card = _cardDatabase.GetById(saved.CardId);
                        if (card != null)
                            _playerDeck.Cards.Add(new DeckDefinitionSO.CardEntry { Card = card, Count = saved.Count });
                    }
                }
            }
        }
    }
}