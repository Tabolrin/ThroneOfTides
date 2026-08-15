// Assets/_Game/2. Scripts/UI/CardCheatEntryButton.cs
using TMPro;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // One row in the card-cheat scroll list - name label + click-to-add button.
    // Deliberately minimal (no art/cost/type banner) since this is a dev tool, not a
    // content-browsing UI; mirrors the plain Setup(...) shape used by PortCardRow.
    public class CardCheatEntryButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private Button          _button;

        public void Setup(CardSO card, System.Action<CardSO> onClick)
        {
            if (_nameLabel != null) _nameLabel.text = card.Name;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke(card));
        }
    }
}
