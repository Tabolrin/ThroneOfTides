using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    // Shown during enemy attack if player holds Dead Man's Turn or Kraken
    public class DeadMansTurnPrompt : MonoBehaviour
    {
        [SerializeField] private Image           _cardArtDisplay;
        [SerializeField] private TextMeshProUGUI _attackDescription;
        [SerializeField] private TextMeshProUGUI _damagePreview;
        [SerializeField] private TextMeshProUGUI _blockCostLabel;
        [SerializeField] private Button          _negateButton;
        [SerializeField] private Button          _takeHitButton;

        private System.Action _onNegate;
        private System.Action _onTakeHit;

        private void Awake() => gameObject.SetActive(false);

        public void Show(CardSO attackCard, int damage, string blockCost,
            System.Action onNegate, System.Action onTakeHit)
        {
            _onNegate  = onNegate;
            _onTakeHit = onTakeHit;

            _attackDescription.text = $"{attackCard.Name}";
            _damagePreview.text     = $"Incoming damage: {damage}";
            _blockCostLabel.text    = $"Block cost: {blockCost}";

            if (attackCard.Art != null)
                _cardArtDisplay.sprite = attackCard.Art;

            _negateButton.onClick.RemoveAllListeners();
            _takeHitButton.onClick.RemoveAllListeners();
            _negateButton.onClick.AddListener(OnNegateClicked);
            _takeHitButton.onClick.AddListener(OnTakeHitClicked);

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void OnNegateClicked()
        {
            Hide();
            _onNegate?.Invoke();
        }

        private void OnTakeHitClicked()
        {
            Hide();
            _onTakeHit?.Invoke();
        }
    }
}