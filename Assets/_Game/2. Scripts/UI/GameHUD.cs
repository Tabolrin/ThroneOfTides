// Assets/_Game/2. Scripts/UI/GameHUD.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using _Game._2._Scripts.rum;

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

        [Header("Player Mana")]
        [SerializeField] private TextMeshProUGUI _playerManaLabel;
        [SerializeField] private Image           _playerManaFill;

        [Header("Enemy Mana")]
        [SerializeField] private TextMeshProUGUI _enemyManaLabel;
        [SerializeField] private Image           _enemyManaFill;

        public void Refresh(int playerHP,   int maxPlayerHP,
            int enemyHP,    int maxEnemyHP,
            int deckCount,  bool isPlayerTurn,
            int playerMana, int playerMaxMana,
            int enemyMana,  int enemyMaxMana)
        {
            _playerHPLabel.text  = $"HP: {playerHP} / {maxPlayerHP}";
            _enemyHPLabel.text   = $"HP: {enemyHP} / {maxEnemyHP}";
            _deckCountLabel.text = $"Deck: {deckCount}";
            _turnIndicator.text  = isPlayerTurn ? "Your Turn" : "Enemy Turn";

            SetFillAmount(_playerHPFill, playerHP, maxPlayerHP);
            SetFillAmount(_enemyHPFill,  enemyHP,  maxEnemyHP);

            SetFillAmount(_playerManaFill, playerMana, playerMaxMana);
            SetFillAmount(_enemyManaFill,  enemyMana,  enemyMaxMana);

            if (_playerManaLabel != null)
                _playerManaLabel.text = $"{playerMana} / {playerMaxMana}";
            if (_enemyManaLabel != null)
                _enemyManaLabel.text = $"{enemyMana} / {enemyMaxMana}";
        }

        // Guards against div-by-zero and clamps to a valid 0–1 range for Image.fillAmount.
        // If a LiquidFillAnimator sits on the same object (idle wave wobble on the liquid art),
        // route through it instead of tweening the Image directly - its own Update() loop
        // continuously re-applies its last-known target fill, which would otherwise fight and
        // undo DOTween's tween every frame.
        private static void SetFillAmount(Image image, int current, int max, float duration = 0.3f)
        {
            if (image == null) return;
            float target = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

            var liquidAnimator = image.GetComponent<LiquidFillAnimator>();
            if (liquidAnimator != null)
            {
                liquidAnimator.SetFill(target, duration);
                return;
            }

            image.DOFillAmount(target, duration).SetEase(Ease.OutQuad);
        }
    }
}