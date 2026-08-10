// Assets/_Game/2. Scripts/UI/EnemyHandRevealPanel.cs
using System.Collections.Generic;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    /// <summary>
    /// Modal popup for Recon Parrot: shows the enemy's revealed hand centered over the play
    /// zone until the player dismisses it (Close button or clicking the dimmed background).
    /// Spawns its own CardView instances directly (not through HandLayoutManager's hand pool —
    /// this is a short-lived, independent display, not part of either hand's live layout).
    /// The root GameObject stays active at all times so OnEnable can subscribe to
    /// GameEventBus once at scene start; a separate Visual child is what actually toggles, to
    /// avoid the classic "SetActive(true) re-triggers Awake/OnEnable synchronously" trap.
    /// </summary>
    public class EnemyHandRevealPanel : MonoBehaviour
    {
        [SerializeField] private GameObject     _visualRoot;
        [SerializeField] private RectTransform  _cardContainer;
        [SerializeField] private CardView       _cardPrefab;
        [SerializeField] private Button         _dismissButton;
        [SerializeField] private Button         _backgroundButton;
        [SerializeField] private float          _cardSpacing = 330f;

        private readonly List<CardView> _spawned = new List<CardView>();

        private void Awake()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_dismissButton != null) _dismissButton.onClick.AddListener(Hide);
            if (_backgroundButton != null) _backgroundButton.onClick.AddListener(Hide);
        }

        private void OnEnable()  => GameEventBus.OnEnemyHandRevealed += Show;
        private void OnDisable() => GameEventBus.OnEnemyHandRevealed -= Show;

        public void Show(IReadOnlyList<ICard> cards)
        {
            Clear();
            if (_visualRoot != null) _visualRoot.SetActive(true);
            if (_cardContainer == null || _cardPrefab == null) return;

            float totalWidth = (cards.Count - 1) * _cardSpacing;
            float startX     = -totalWidth / 2f;

            for (int i = 0; i < cards.Count; i++)
            {
                if (!(cards[i] is CardSO cardSO)) continue;

                CardView view = Instantiate(_cardPrefab, _cardContainer);
                view.Setup(cardSO);

                var drag = view.GetComponent<CardDragHandler>();
                if (drag != null) Destroy(drag);

                var rect = view.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(startX + i * _cardSpacing, 0f);

                // Added after positioning — CardHoverEffect captures its base position in
                // OnEnable, which fires synchronously the instant AddComponent runs.
                view.gameObject.AddComponent<CardHoverEffect>();

                _spawned.Add(view);
            }
        }

        public void Hide()
        {
            Clear();
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        private void Clear()
        {
            foreach (var view in _spawned)
                if (view != null) Destroy(view.gameObject);
            _spawned.Clear();
        }
    }
}
