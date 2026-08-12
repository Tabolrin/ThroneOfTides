using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    // Shown during an enemy attack when the player can react to it — either the full
    // Dead Man's Turn / Counter Gale choice, or (reusing this same panel) the Kraken-vs-Kraken
    // negate prompt. Negate and Counter Gale are independently optional: pass a null action for
    // either to hide that button entirely, so a single panel serves every case (only Dead Man's
    // Turn available, only Counter Gale available, both available, or the Kraken's own
    // negate-only standoff) without the caller needing to guess which combination is live.
    public class DeadMansTurnPrompt : MonoBehaviour
    {
        [SerializeField] private Image           _cardArtDisplay;
        [SerializeField] private TextMeshProUGUI _attackDescription;
        [SerializeField] private TextMeshProUGUI _damagePreview;

        [SerializeField] private Button          _negateButton;
        [SerializeField] private TextMeshProUGUI _negateButtonLabel;
        [SerializeField] private Button          _counterGaleButton;
        [SerializeField] private TextMeshProUGUI _counterGaleButtonLabel;
        [SerializeField] private Button          _takeHitButton;

        private System.Action _onNegate;
        private System.Action _onCounterGale;
        private System.Action _onTakeHit;

        public void Show(
            CardSO attackCard, int damage,
            string negateLabel, System.Action onNegate,
            string counterGaleLabel, System.Action onCounterGale,
            System.Action onTakeHit)
        {
            _onNegate      = onNegate;
            _onCounterGale = onCounterGale;
            _onTakeHit     = onTakeHit;

            _attackDescription.text = attackCard.Name;
            _damagePreview.text     = $"Incoming damage: {damage}";

            if (attackCard.Art != null)
                _cardArtDisplay.sprite = attackCard.Art;

            bool showNegate      = onNegate != null;
            bool showCounterGale = onCounterGale != null;

            _negateButton.gameObject.SetActive(showNegate);
            _counterGaleButton.gameObject.SetActive(showCounterGale);

            _negateButton.onClick.RemoveAllListeners();
            _counterGaleButton.onClick.RemoveAllListeners();
            _takeHitButton.onClick.RemoveAllListeners();

            if (showNegate)
            {
                if (_negateButtonLabel != null) _negateButtonLabel.text = negateLabel;
                _negateButton.onClick.AddListener(OnNegateClicked);
            }

            if (showCounterGale)
            {
                if (_counterGaleButtonLabel != null) _counterGaleButtonLabel.text = counterGaleLabel;
                _counterGaleButton.onClick.AddListener(OnCounterGaleClicked);
            }

            _takeHitButton.onClick.AddListener(OnTakeHitClicked);

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void OnNegateClicked()
        {
            Hide();
            _onNegate?.Invoke();
        }

        private void OnCounterGaleClicked()
        {
            Hide();
            _onCounterGale?.Invoke();
        }

        private void OnTakeHitClicked()
        {
            Hide();
            _onTakeHit?.Invoke();
        }
    }
}
