using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _turnIndicator;
        [SerializeField] private TMP_Text _deckCountLabel;

        // Assign the LiquidFill child Image - not the bottle root
        [SerializeField] private Image _playerHPLiquid;
        [SerializeField] private Image _enemyHPLiquid;

        public void Refresh(int playerHP, int maxPlayerHP,
            int enemyHP,  int maxEnemyHP,
            int deckCount, bool isPlayerTurn)
        {
            // Clamp prevents division by zero if maxHP misconfigured
            _playerHPLiquid.fillAmount = maxPlayerHP > 0 ? (float)playerHP / maxPlayerHP : 0f;
            _enemyHPLiquid.fillAmount  = maxEnemyHP  > 0 ? (float)enemyHP  / maxEnemyHP  : 0f;
            _deckCountLabel.text       = deckCount.ToString();
            _turnIndicator.text        = isPlayerTurn ? "Your Turn" : "Enemy Turn";
        }
    }
}