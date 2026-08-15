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
    /// Spawns its own CardView instances directly (not through HandLayoutManager's hand pool -
    /// this is a short-lived, independent display, not part of either hand's live layout).
    /// Driven directly by ReconParrotVFXController (via CardEffectSpawnContext.ShowEnemyHandReveal)
    /// rather than a global event, so its on-screen timing can be sequenced around the parrot's
    /// own flight animation instead of popping up the instant the underlying effect resolves.
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
        private System.Action _onDismissed;

        private void Awake()
        {
            if (_visualRoot != null) _visualRoot.SetActive(false);
            if (_dismissButton != null) _dismissButton.onClick.AddListener(Hide);
            if (_backgroundButton != null) _backgroundButton.onClick.AddListener(Hide);
        }

        /// <summary>
        /// Shows the panel and invokes onDismissed once the player closes it (Close button or
        /// clicking the dimmed background) - for callers (e.g. ReconParrotVFXController) that
        /// need to sequence their own animation around the panel's lifetime instead of it firing
        /// on a global event the instant the underlying effect resolves.
        /// </summary>
        public void ShowAndAwaitDismiss(IReadOnlyList<ICard> cards, System.Action onDismissed)
        {
            _onDismissed = onDismissed;
            Show(cards);
        }

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

                // Added after positioning - CardHoverEffect captures its base position in
                // OnEnable, which fires synchronously the instant AddComponent runs.
                view.gameObject.AddComponent<CardHoverEffect>();

                _spawned.Add(view);
            }
        }

        public void Hide()
        {
            Clear();
            if (_visualRoot != null) _visualRoot.SetActive(false);

            var callback = _onDismissed;
            _onDismissed = null;
            callback?.Invoke();
        }

        private void Clear()
        {
            foreach (var view in _spawned)
                if (view != null) Destroy(view.gameObject);
            _spawned.Clear();
        }
    }
}
