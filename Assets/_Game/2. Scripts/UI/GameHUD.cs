// Assets/_Game/2. Scripts/UI/GameHUD.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ThroneOfTides.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Turn & Deck")]
        [SerializeField] private TextMeshProUGUI _turnIndicator;
        [SerializeField] private TextMeshProUGUI _deckCountLabel;

        [Header("Player HP")]
        [SerializeField] private TextMeshProUGUI _playerHPLabel;
        [SerializeField] private Image           _playerHPFill;

        [Header("Enemy HP")]
        [SerializeField] private TextMeshProUGUI _enemyHPLabel;
        [SerializeField] private Image           _enemyHPFill;

        [Header("Mana")]
        [SerializeField] private TextMeshProUGUI _playerManaLabel;

        public void Refresh(int playerHP,   int maxPlayerHP,
            int enemyHP,    int maxEnemyHP,
            int deckCount,  bool isPlayerTurn,
            int playerMana, int playerMaxMana)
        {
            _playerHPLabel.text  = $"HP: {playerHP} / {maxPlayerHP}";
            _enemyHPLabel.text   = $"HP: {enemyHP} / {maxEnemyHP}";
            _deckCountLabel.text = $"Deck: {deckCount}";
            _turnIndicator.text  = isPlayerTurn ? "Your Turn" : "Enemy Turn";

            SetFillAmount(_playerHPFill, playerHP, maxPlayerHP);
            SetFillAmount(_enemyHPFill,  enemyHP,  maxEnemyHP);

            if (_playerManaLabel != null)
                _playerManaLabel.text = $"Mana: {playerMana} / {playerMaxMana}";
        }

        // Guards against div-by-zero and clamps to a valid 0–1 range for Image.fillAmount
        private static void SetFillAmount(Image image, int current, int max, float duration = 0.3f)
        {
            if (image == null) return;
            float target = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            image.DOFillAmount(target, duration).SetEase(Ease.OutQuad);
        }
    }
}