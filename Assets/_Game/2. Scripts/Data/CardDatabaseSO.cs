// Assets/_Game/2. Scripts/Data/CardDatabaseSO.cs
using System.Collections.Generic;
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    /// <summary>
    /// Flat registry of every CardSO in the game, keyed by its stable CardId. Lets the save
    /// system reconstruct a saved card collection (a list of CardId) back into real CardSO
    /// references without needing Resources/Addressables — populate by dragging every CardSO
    /// asset into _allCards once.
    /// </summary>
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/CardDatabase")]
    public class CardDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<CardSO> _allCards = new List<CardSO>();

        private Dictionary<CardId, CardSO> _byId;

        public CardSO GetById(CardId id)
        {
            if (_byId == null) BuildLookup();
            _byId.TryGetValue(id, out var card);
            return card;
        }

        private void BuildLookup()
        {
            _byId = new Dictionary<CardId, CardSO>();
            foreach (var card in _allCards)
            {
                if (card == null || card.Id == CardId.None) continue;
                if (!_byId.ContainsKey(card.Id))
                    _byId.Add(card.Id, card);
            }
        }
    }
}
