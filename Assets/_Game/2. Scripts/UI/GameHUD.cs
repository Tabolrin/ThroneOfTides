using TMPro;
using UnityEngine;

namespace ThroneOfTides.UI
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _turnIndicator;
        [SerializeField] private TextMeshProUGUI _deckCountLabel;
        [SerializeField] private TextMeshProUGUI _playerHPLabel;
        [SerializeField] private TextMeshProUGUI _enemyHPLabel;
        [SerializeField] private TextMeshProUGUI _playerManaLabel;

        public void Refresh(int playerHP, int maxPlayerHP,
            int enemyHP,  int maxEnemyHP,
            int deckCount, bool isPlayerTurn,
            int playerMana = 0, int playerMaxMana = 0)
        {
            _playerHPLabel.text  = $"HP: {playerHP} / {maxPlayerHP}";
            _enemyHPLabel.text   = $"HP: {enemyHP} / {maxEnemyHP}";
            _deckCountLabel.text = $"Deck: {deckCount}";
            _turnIndicator.text  = isPlayerTurn ? "Your Turn" : "Enemy Turn";

            if (_playerManaLabel != null)
                _playerManaLabel.text = $"Mana: {playerMana} / {playerMaxMana}";
        }
    }
}