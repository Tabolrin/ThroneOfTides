using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using ThroneOfTides.Data;
using ThroneOfTides.UI;
using UnityEngine;
using ThroneOfTides.Systems;
using UnityEngine.Serialization;
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
        [Tooltip("The Port is always optional and freely revisitable from the map - never gated behind a level and never accessible from the main menu, so this is the only entry point.")]
        [SerializeField] private Button     _portButton;
        [Tooltip("Small indicator shown on the Port node when there's coin or an unused unlocked card worth spending - a nudge, not a gate.")]
        [SerializeField] private GameObject _portNotificationBadge;
        [SerializeField] private PlayerInventory _playerInventory;

        [Header("Navigation")]
        [SerializeField] private Button _mainMenuButton;

        [Header("Ship")]
        [Tooltip("The small ship icon that travels across the map to the chosen node before the scene actually changes.")]
        [SerializeField] private RectTransform _shipIcon;
        [Tooltip("Screen units per second the ship sails at - travel duration for a trip is its route length divided by this, so longer trips naturally take longer at a constant felt speed.")]
        [SerializeField] private float _shipSpeed = 500f;
        [SerializeField] private Ease  _shipMoveEase = Ease.InOutSine;
        [Tooltip("Beat after the ship visibly arrives at its destination before the scene actually loads - long enough to read as 'docked', short enough not to feel like a stall.")]
        [SerializeField] private float _arrivalDelay = 0.15f;

        [Tooltip("Where the ship travels to for each node - drag the node's own RectTransform (Level1Node/Level2Node/Level3Node/Port), not its button.")]
        [SerializeField] private RectTransform _level1NodeAnchor;
        [SerializeField] private RectTransform _level2NodeAnchor;
        [SerializeField] private RectTransform _level3NodeAnchor;
        [SerializeField] private RectTransform _portNodeAnchor;

        [Header("Route Waypoints - Between Levels")]
        [Tooltip("The 3 level nodes sit in a fixed sailing order - Level 1, Level 2, Level 3 - so a trip between any two of them is just the sub-chain of legs between them (e.g. Level 1 to Level 3 flows through the Level-1-to-Level-2 leg's waypoints, past Level 2 itself, then the Level-2-to-Level-3 leg's waypoints). Each leg gets 2 waypoints so the route curves smoothly around the islands instead of cutting a straight line.")]
        [SerializeField] private RectTransform _level1ToLevel2WaypointA;
        [SerializeField] private RectTransform _level1ToLevel2WaypointB;
        [SerializeField] private RectTransform _level2ToLevel3WaypointA;
        [SerializeField] private RectTransform _level2ToLevel3WaypointB;

        [Header("Route Waypoints - To/From Port")]
        [Tooltip("The Port sits off on its own, connected to the level cluster by a single shared route rather than the level-to-level chain above. Whichever level the ship is coming from or going to, it always passes through the Regroup Point first (closest to the levels), then Waypoint A, then Waypoint B (closest to the Port) before reaching the Port - and the reverse when leaving the Port. From the Regroup Point, the ship sails directly to whichever level was requested, without passing through the other levels' nodes.")]
        [FormerlySerializedAs("_portMidwayAnchor")]
        [SerializeField] private RectTransform _portRegroupPoint;
        [FormerlySerializedAs("_portToLevel1WaypointA")]
        [SerializeField] private RectTransform _portWaypointA;
        [FormerlySerializedAs("_portToLevel1WaypointB")]
        [SerializeField] private RectTransform _portWaypointB;

        // Fixed sailing order used to decompose a level-to-level trip into consecutive adjacent
        // legs - purely an internal routing concept, unrelated to MapNode's own declaration
        // order. The Port is deliberately excluded - it never chains through the levels, see
        // BuildPortRoute.
        private static readonly MapNode[] RouteOrder = { MapNode.Level1, MapNode.Level2, MapNode.Level3 };

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
            // Both are RectTransforms on the same Screen Space - Overlay canvas, so their
            // .position values already share the same space - no local-point conversion needed.
            if (anchor != null) _shipIcon.position = anchor.position;
        }

        private RectTransform GetAnchor(MapNode node) => node switch
        {
            MapNode.Level1 => _level1NodeAnchor,
            MapNode.Level2 => _level2NodeAnchor,
            MapNode.Level3 => _level3NodeAnchor,
            MapNode.Port   => _portNodeAnchor,
            _              => null
        };

        // The 2 configured waypoints for one specific adjacent leg of RouteOrder, in the
        // direction actually being traveled (reversed if going "backwards" along the chain).
        // Port never appears here - see BuildPortRoute for its own dedicated hub route.
        private IEnumerable<RectTransform> GetLegWaypoints(MapNode from, MapNode to)
        {
            if (from == MapNode.Level1 && to == MapNode.Level2)
                return new[] { _level1ToLevel2WaypointA, _level1ToLevel2WaypointB };
            if (from == MapNode.Level2 && to == MapNode.Level1)
                return new[] { _level1ToLevel2WaypointB, _level1ToLevel2WaypointA };

            if (from == MapNode.Level2 && to == MapNode.Level3)
                return new[] { _level2ToLevel3WaypointA, _level2ToLevel3WaypointB };
            if (from == MapNode.Level3 && to == MapNode.Level2)
                return new[] { _level2ToLevel3WaypointB, _level2ToLevel3WaypointA };

            return Array.Empty<RectTransform>();
        }

        // Builds the full ordered list of positions to sail through between two nodes, excluding
        // the ship's own current position (DOPath animates FROM wherever the ship already is).
        // A multi-hop trip between levels (e.g. Level 1 to Level 3) naturally passes through
        // every node and every leg's waypoints in between. Any trip touching the Port instead
        // uses its own dedicated hub route (see BuildPortRoute) rather than this level-to-level
        // chain, since the Port isn't part of that chain.
        private List<Vector3> BuildRoute(MapNode from, MapNode to)
        {
            var points = new List<Vector3>();
            if (from == to) return points;

            if (from == MapNode.Port || to == MapNode.Port)
                return BuildPortRoute(from, to);

            int fromIndex = Array.IndexOf(RouteOrder, from);
            int toIndex   = Array.IndexOf(RouteOrder, to);

            if (fromIndex < 0 || toIndex < 0)
            {
                RectTransform anchor = GetAnchor(to);
                if (anchor != null) points.Add(anchor.position);
                return points;
            }

            int step  = toIndex > fromIndex ? 1 : -1;
            int index = fromIndex;

            while (index != toIndex)
            {
                MapNode current = RouteOrder[index];
                MapNode next    = RouteOrder[index + step];

                foreach (var waypoint in GetLegWaypoints(current, next))
                    if (waypoint != null) points.Add(waypoint.position);

                RectTransform nextAnchor = GetAnchor(next);
                if (nextAnchor != null) points.Add(nextAnchor.position);

                index += step;
            }

            return points;
        }

        // The Port connects to the level cluster through a single shared route rather than the
        // level-to-level chain: whichever level is involved, the ship always passes through the
        // Regroup Point, then Waypoint A, then Waypoint B on its way to/from the Port - and once
        // it reaches the Regroup Point on the way OUT of the Port, it sails directly to whichever
        // level was requested, without detouring through the other levels' nodes.
        private List<Vector3> BuildPortRoute(MapNode from, MapNode to)
        {
            var points = new List<Vector3>();
            bool towardPort = to == MapNode.Port;
            MapNode level = towardPort ? from : to;

            if (towardPort)
            {
                if (_portRegroupPoint != null) points.Add(_portRegroupPoint.position);
                if (_portWaypointA != null) points.Add(_portWaypointA.position);
                if (_portWaypointB != null) points.Add(_portWaypointB.position);

                RectTransform portAnchor = GetAnchor(MapNode.Port);
                if (portAnchor != null) points.Add(portAnchor.position);
            }
            else
            {
                if (_portWaypointB != null) points.Add(_portWaypointB.position);
                if (_portWaypointA != null) points.Add(_portWaypointA.position);
                if (_portRegroupPoint != null) points.Add(_portRegroupPoint.position);

                RectTransform levelAnchor = GetAnchor(level);
                if (levelAnchor != null) points.Add(levelAnchor.position);
            }

            return points;
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

        // Leaving the map entirely rather than travelling to a node on it - just fades out.
        private void OnMainMenuPressed() => SceneFader.LoadScene("MainMenu");

        // Sails the ship icon along the route between the current node and the clicked one,
        // flowing smoothly through each leg's waypoints (see BuildRoute), and only loads the
        // destination scene once it actually arrives plus a short arrival beat - the whole point
        // being that the scene change reads as "the ship sailed there", not an instant cut.
        // Buttons are disabled for the duration so a second click can't kick off another journey
        // mid-travel.
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
            _shipIcon.DOKill();

            List<Vector3> route = BuildRoute(GameSession.CurrentMapNode, destination);
            if (route.Count == 0) route.Add(targetAnchor.position);
            Vector3[] pathPoints = route.ToArray();

            Vector3 startPosition = _shipIcon.position;
            FaceShipTowards(startPosition.x, pathPoints[0].x);

            float totalDistance = Vector3.Distance(startPosition, pathPoints[0]);
            for (int i = 1; i < pathPoints.Length; i++)
                totalDistance += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);

            float duration = Mathf.Max(0.1f, totalDistance / Mathf.Max(1f, _shipSpeed));

            // DOPath needs at least 2 points to interpolate a curve through - a single-leg trip
            // (adjacent nodes with no configured waypoints) falls back to a plain straight move.
            Tween travelTween = pathPoints.Length >= 2
                ? _shipIcon.DOPath(pathPoints, duration, PathType.CatmullRom)
                    .SetEase(_shipMoveEase)
                    .OnWaypointChange(waypointIndex =>
                    {
                        if (waypointIndex + 1 < pathPoints.Length)
                            FaceShipTowards(pathPoints[waypointIndex].x, pathPoints[waypointIndex + 1].x);
                    })
                : _shipIcon.DOMove(pathPoints[0], duration).SetEase(_shipMoveEase);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(travelTween);
            sequence.AppendInterval(_arrivalDelay);
            sequence.OnComplete(() =>
            {
                GameSession.SetCurrentMapNode(destination);
                SceneFader.LoadScene(sceneName);
            });
        }

        // Flips the ship icon horizontally to face whichever way it's about to travel. The
        // sprite's native art faces left, so its authored resting scale (localScale.x = -1) is
        // what makes it face right - a negative X mirrors to face right, a positive X faces left.
        // No-op for a purely vertical leg (equal X) so the ship keeps whatever facing it already
        // had rather than snapping to a default.
        private void FaceShipTowards(float fromX, float toX)
        {
            if (_shipIcon == null) return;

            float deltaX = toX - fromX;
            if (Mathf.Approximately(deltaX, 0f)) return;

            Vector3 scale = _shipIcon.localScale;
            float absX = Mathf.Abs(scale.x);
            scale.x = deltaX > 0f ? -absX : absX;
            _shipIcon.localScale = scale;
        }

        private void SetNodesInteractable(bool value)
        {
            _level1Button.interactable = value;
            _level2Button.interactable = value && _progression.Level2Unlocked;
            _level3Button.interactable = value && _progression.Level3Unlocked;
            if (_portButton != null) _portButton.interactable = value;
            _mainMenuButton.interactable = value;
        }

        // A nudge, not a gate - lights up when there's coin sitting unspent or a card the
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