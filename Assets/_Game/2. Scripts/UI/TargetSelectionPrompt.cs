// Assets/_Game/2. Scripts/UI/TargetSelectionPrompt.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    // Shown when a played card requires the player to choose a target ship (e.g. Tidal Wave).
    public class TargetSelectionPrompt : MonoBehaviour
    {
        [SerializeField] private Image           _cardArtDisplay;
        [SerializeField] private TextMeshProUGUI _cardNameLabel;
        [SerializeField] private Button          _targetSelfButton;
        [SerializeField] private Button          _targetEnemyButton;

        private System.Action<DamageTarget> _onTargetChosen;

        public void Show(CardSO card, System.Action<DamageTarget> onTargetChosen)
        {
            _onTargetChosen = onTargetChosen;

            _cardNameLabel.text = card.Name;

            if (card.Art != null)
                _cardArtDisplay.sprite = card.Art;

            _targetSelfButton.onClick.RemoveAllListeners();
            _targetEnemyButton.onClick.RemoveAllListeners();
            _targetSelfButton.onClick.AddListener(() => OnTargetClicked(DamageTarget.Player));
            _targetEnemyButton.onClick.AddListener(() => OnTargetClicked(DamageTarget.Enemy));

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void OnTargetClicked(DamageTarget target)
        {
            Hide();
            _onTargetChosen?.Invoke(target);
        }
    }
}
