using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/PowerUp")]
    public class PowerUpSO : ScriptableObject
    {
        [SerializeField] private string _powerUpName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private string _description;

        public string PowerUpName => _powerUpName;
        public Sprite Icon        => _icon;
        public string Description => _description;
    }
}