// Assets/_Game/2. Scripts/UI/EffectBadgeView.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // One instantiated badge — an icon plus an optional count label. Spawned on demand by
    // ActiveEffectsBar when a status/reaction becomes active, destroyed when it clears.
    public class EffectBadgeView : MonoBehaviour
    {
        [SerializeField] private Image           _icon;
        [SerializeField] private TextMeshProUGUI _countLabel;

        public void Setup(Sprite icon, int? count)
        {
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }

            if (_countLabel != null)
                _countLabel.text = count.HasValue ? count.Value.ToString() : string.Empty;
        }
    }
}
