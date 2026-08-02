// Assets/_Game/2. Scripts/Data/BuildDeckProfileSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThroneOfTides.Data
{
    public enum BuildProfileType { Development, Release, Playtest, WebGL }

    [Serializable]
    public class BuildDeckProfile
    {
        public BuildProfileType Type;
        [Tooltip("Leave empty to use whatever deck the scene's GameBootstrapper is already wired to.")]
        public DeckDefinitionSO PlayerDeck;
        [Tooltip("Leave empty to use the active Captain's own deck.")]
        public DeckDefinitionSO EnemyDeck;
    }

    [CreateAssetMenu(menuName = "ThroneOfTides/Data/Build Deck Profiles")]
    public class BuildDeckProfileSO : ScriptableObject
    {
        [SerializeField] private List<BuildDeckProfile> _profiles = new List<BuildDeckProfile>();

        public BuildDeckProfile GetProfile(BuildProfileType type) =>
            _profiles.Find(p => p.Type == type);
    }
}
