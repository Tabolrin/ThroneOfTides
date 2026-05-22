using ThroneOfTides.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // Always available - ends match as a loss
    // Separate from End Turn - no card play requirement
    public class ConcedeButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private void Awake() =>
            _button.onClick.AddListener(OnConcedePressed);

        private void OnDestroy() =>
            _button.onClick.RemoveAllListeners();

        private void OnConcedePressed() =>
            GameEventBus.FireMatchLoss();
    }
}