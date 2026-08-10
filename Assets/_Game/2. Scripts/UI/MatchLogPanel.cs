// Assets/_Game/2. Scripts/UI/MatchLogPanel.cs
using System.Collections.Generic;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // A small scrolling history of "important" match events — card plays (hoverable for their
    // description), HP loss/gain, and misc one-off notes (e.g. Locker's Return recovering
    // cards). Deliberately leaves out noisier events (mana changes, DOT ticks, combo-stack
    // increments) that GameEventBus already exposes but would clutter a log meant to stay
    // readable at a glance.
    public class MatchLogPanel : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private Transform          _content;
        [SerializeField] private MatchLogEntryRow   _rowPrefab;
        [SerializeField] private ScrollRect         _scrollRect;
        [SerializeField] private EffectBadgeTooltip _tooltip;
        [Tooltip("Oldest entries are dropped past this count so the log doesn't grow unbounded over a long match.")]
        [SerializeField] private int _maxEntries = 60;

        private readonly Queue<MatchLogEntryRow> _rows = new Queue<MatchLogEntryRow>();

        private void OnEnable()
        {
            GameEventBus.OnCardPlayAccepted += OnPlayerCardPlayed;
            GameEventBus.OnEnemyCardPlayed  += OnEnemyCardPlayed;
            GameEventBus.OnDamageDealt      += OnDamageDealt;
            GameEventBus.OnHealApplied      += OnHealApplied;
            GameEventBus.OnMatchNote        += OnMatchNote;
            GameEventBus.OnMatchWin         += OnMatchWin;
            GameEventBus.OnMatchLoss        += OnMatchLoss;
        }

        private void OnDisable()
        {
            GameEventBus.OnCardPlayAccepted -= OnPlayerCardPlayed;
            GameEventBus.OnEnemyCardPlayed  -= OnEnemyCardPlayed;
            GameEventBus.OnDamageDealt      -= OnDamageDealt;
            GameEventBus.OnHealApplied      -= OnHealApplied;
            GameEventBus.OnMatchNote        -= OnMatchNote;
            GameEventBus.OnMatchWin         -= OnMatchWin;
            GameEventBus.OnMatchLoss        -= OnMatchLoss;
        }

        private void OnPlayerCardPlayed(ICard card, DamageTarget? target) => AddEntry($"Player played {card.Name}", card as CardSO);
        private void OnEnemyCardPlayed(ICard card, DamageTarget? target)  => AddEntry($"Enemy played {card.Name}", card as CardSO);

        private void OnDamageDealt(DamageTarget target, int amount)
        {
            if (amount <= 0) return;
            AddEntry($"{SideLabel(target)} took {amount} damage.", null);
        }

        private void OnHealApplied(DamageTarget target, int amount)
        {
            if (amount <= 0) return;
            AddEntry($"{SideLabel(target)} healed {amount} HP.", null);
        }

        private void OnMatchNote(string message) => AddEntry(message, null);
        private void OnMatchWin()  => AddEntry("Victory!", null);
        private void OnMatchLoss() => AddEntry("Defeat...", null);

        private static string SideLabel(DamageTarget side) => side == DamageTarget.Player ? "Player" : "Enemy";

        private void AddEntry(string text, CardSO card)
        {
            if (_rowPrefab == null || _content == null) return;

            var row = Instantiate(_rowPrefab, _content);
            row.Setup(text, card, _tooltip);
            _rows.Enqueue(row);

            if (_rows.Count > _maxEntries)
                Destroy(_rows.Dequeue().gameObject);

            // Snap to the newest entry — layout needs to rebuild first or the scroll position
            // computed this frame would still reflect last frame's content height.
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
