// Assets/_Game/2. Scripts/Data/CardTagSO.cs
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/CardTag")]
    public class CardTagSO : ScriptableObject
    {
        [SerializeField] private string _tagName;
        [SerializeField] private Sprite _symbol;
        [SerializeField] private string _description;

        public string TagName    => _tagName;
        public Sprite Symbol     => _symbol;
        public string Description=> _description;
    }
}