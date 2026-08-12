using DG.Tweening;
using TMPro;
using ThroneOfTides.Data;
using ThroneOfTides.UI;
using UnityEngine;
using ThroneOfTides.Systems;
using UnityEngine.UI;

namespace ThroneOfTides.Systems
{
    public class LevelSelectManager : MonoBehaviour
    {
        [Header("Progression")]
        [SerializeField] private ProgressionSO _progression;

        [Header("Captains")]
        [SerializeField] private CaptainSO _captain1;
        [SerializeField] private CaptainSO _captain2;
        [SerializeField] private CaptainSO _captain3;

        [Header("Level Nodes")]
        [SerializeField] private Button          _level1Button;
        [SerializeField] private Button          _level2Button;
        [SerializeField] private Button          _level3Button;
        [SerializeField] private TextMeshProUGUI _level1Label;
        [SerializeField] private TextMeshProUGUI _level2Label;
        [SerializeField] private TextMeshProUGUI _level3Label;
        [SerializeField] private GameObject      _level2Lock;
        [SerializeField] private GameObject      _level3Lock;

        [Header("Port")]
        [Tooltip("The Port is always optional and freely revisitable from the map — never gated behind a level and never accessible from the main menu, so this is the only entry point.")]
        [SerializeField] private Button     _portButton;
        [Tooltip("Small indicator shown on the Port node when there's coin or an unused unlocked card worth spending — a nudge, not a gate.")]
        [SerializeField] private GameObject _portNotificationBadge;
        [SerializeField] private PlayerInventory _playerInventory;

        [Header("Navigation")]
        [SerializeField] private Button _mainMenuButton;

        [Header("Ship")]
        [Tooltip("The small ship icon that travels across the map to the chosen node before the scene actually changes.")]
        [SerializeField] private RectTransform _shipIcon;
        [SerializeField] private float _shipMoveDuration = 0.8f;
        [SerializeField] private Ease  _shipMoveEase = Ease.InOutSine;

        [Tooltip("Where the ship travels to for each node — drag the node's own RectTransform (Level1Node/Level2Node/Level3Node/Port), not its button.")]
        [SerializeField] private RectTransform _level1NodeAnchor;
        [SerializeField] private RectTransform _level2NodeAnchor;
        [SerializeField] private RectTransform _level3NodeAnchor;
        [SerializeField] private RectTransform _portNodeAnchor;

        [Tooltip("Waypoint every trip to/from the Port routes through, so the ship doesn't cut straight over land — the Port sits in an awkward spot relative to the other nodes. Not used for level-to-level travel.")]
        [SerializeField] private RectTransform _portMidwayAnchor;

        private void Start()
        {
            RefreshNodes();
            RefreshPortBadge();
            SnapShipToCurrentNode();

            _level1Button.onClick.AddListener(() => OnLevelSelected(_captain1, 1));
            _level2Button.onClick.AddListener(() => OnLevelSelected(_captain2, 2));
            _level3Button.onClick.AddListener(() => OnLevelSelected(_captain3, 3));
            if (_portButton != null) _portButton.onClick.AddListener(OnPortSelected);
            _mainMenuButton.onClick.AddListener(OnMainMenuPressed);
        }

        // The LevelSelect scene fully reloads every time the player returns to it, so without
        // this the ship would snap back to whatever position it happens to be authored at in the
        // scene instead of reflecting wherever it last actually traveled to.
        private void SnapShipToCurrentNode()
        {
            if (_shipIcon == null) return;
            RectTransform anchor = GetAnchor(GameSession.CurrentMapNode);
            if (anchor != null) _shipIcon.anchoredPosition = ToLocalPoint(anchor);
        }

        private RectTransform GetAnchor(MapNode node) => node switch
        {
            MapNode.Level1 => _level1NodeAnchor,
            MapNode.Level2 => _level2NodeAnchor,
            MapNode.Level3 => _level3NodeAnchor,
            MapNode.Port   => _portNodeAnchor,
            _              => null
        };

        private Vector2 ToLocalPoint(RectTransform target)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_shipIcon.parent, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        private void RefreshNodes()
        {
            _level1Label.text = _captain1.CaptainName;
            _level2Label.text = _captain2.CaptainName;
            _level3Label.text = _captain3.CaptainName;

            _level2Button.interactable = _progression.Level2Unlocked;
            _level3Button.interactable = _progression.Level3Unlocked;

            _level2Lock.SetActive(!_progression.Level2Unlocked);
            _level3Lock.SetActive(!_progression.Level3Unlocked);
        }

        private void OnLevelSelected(CaptainSO captain, int levelIndex)
        {
            GameSession.SetLevel(captain, levelIndex);

            MapNode destination = levelIndex switch
            {
                1 => MapNode.Level1,
                2 => MapNode.Level2,
                _ => MapNode.Level3
            };

            MoveShipThenLoad(destination, "Match");
        }

        private void OnPortSelected() => MoveShipThenLoad(MapNode.Port, "Port");

        // Leaving the map entirely rather than travelling to a node on it — just fades out.
        private void OnMainMenuPressed() => SceneFader.LoadScene("MainMenu");

        // Tweens the ship icon to the clicked node's position and only loads the destination
        // scene once it actually arrives — the whole point being that the scene change reads as
        // "the ship sailed there", not an instant cut. Buttons are disabled for the duration so a
        // second click can't kick off another journey mid-travel. Any trip with the Port as
        // either end routes through _portMidwayAnchor first so it doesn't cut across land.
        private void MoveShipThenLoad(MapNode destination, string sceneName)
        {
            RectTransform targetAnchor = GetAnchor(destination);

            if (_shipIcon == null || targetAnchor == null)
            {
                GameSession.SetCurrentMapNode(destination);
                SceneFader.LoadScene(sceneName);
                return;
            }

            SetNodesInteractable(false);

            bool routeViaPort = _portMidwayAnchor != null
                && GameSession.CurrentMapNode != destination
                && (GameSession.CurrentMapNode == MapNode.Port || destination == MapNode.Port);

            _shipIcon.DOKill();
            Sequence sequence = DOTween.Sequence();

            if (routeViaPort)
                sequence.Append(_shipIcon.DOAnchorPos(ToLocalPoint(_portMidwayAnchor), _shipMoveDuration).SetEase(_shipMoveEase));

            sequence.Append(_shipIcon.DOAnchorPos(ToLocalPoint(targetAnchor), _shipMoveDuration).SetEase(_shipMoveEase));
            sequence.OnComplete(() =>
            {
                GameSession.SetCurrentMapNode(destination);
                SceneFader.LoadScene(sceneName);
            });
        }

        private void SetNodesInteractable(bool value)
        {
            _level1Button.interactable = value;
            _level2Button.interactable = value && _progression.Level2Unlocked;
            _level3Button.interactable = value && _progression.Level3Unlocked;
            if (_portButton != null) _portButton.interactable = value;
            _mainMenuButton.interactable = value;
        }

        // A nudge, not a gate — lights up when there's coin sitting unspent or a card the
        // player has unlocked but never actually put in their deck.
        private void RefreshPortBadge()
        {
            if (_portNotificationBadge == null || _playerInventory == null) return;

            bool hasUnspentCoins = _playerInventory.Coins > 0;
            bool hasUnusedCard   = false;

            if (_playerInventory.PlayerDeck != null)
            {
                foreach (var card in _playerInventory.Collection)
                {
                    if (!_playerInventory.PlayerDeck.Cards.Exists(e => e.Card == card))
                    {
                        hasUnusedCard = true;
                        break;
                    }
                }
            }

            _portNotificationBadge.SetActive(hasUnspentCoins || hasUnusedCard);
        }
    }
}