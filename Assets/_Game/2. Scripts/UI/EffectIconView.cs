// Assets/_Game/2. Scripts/UI/EffectIconView.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // One icon+badge pair in ActiveEffectsBar's dynamic status row.
    public class EffectIconView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _countLabel;

        public void Setup(Sprite icon, int count)
        {
            if (_icon != null) _icon.sprite = icon;
            SetCount(count);
        }

        public void SetCount(int count)
        {
            if (_countLabel != null) _countLabel.text = count > 1 ? $"×{count}" : string.Empty;
        }
    }
}
