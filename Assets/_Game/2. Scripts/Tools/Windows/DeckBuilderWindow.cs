using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ThroneOfTides.Data;
using ThroneOfTides.Core;

namespace ThroneOfTides.Tools
{
    public class DeckBuilderWindow : EditorWindow
    {
        [MenuItem("ThroneOfTides/Deck Builder  %#d")]  // Ctrl+Shift+D
        public static void Open() => GetWindow<DeckBuilderWindow>("Deck Builder");

        // ── State ───────────────────────────────────────────────────────────────

        private List<CardSO>          _allCards       = new();
        private DeckDefinitionSO      _targetDeck;
        private List<DeckDefinitionSO.CardEntry> _workingEntries = new();

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        private string _searchFilter = "";
        private CardType? _typeFilter = null;

        private bool _isDirty;

        // ── Styles (lazy init to avoid constructor timing issues) ───────────────
        private GUIStyle _headerStyle;
        private GUIStyle _cardButtonStyle;
        private GUIStyle _sectionStyle;

        private static readonly Color _weaponColor  = new Color(0.3f,  0.5f,  1.0f,  0.25f);
        private static readonly Color _comboColor   = new Color(1.0f,  0.85f, 0.0f,  0.25f);
        private static readonly Color _actionColor  = new Color(0.3f,  0.8f,  0.4f,  0.25f);
        private static readonly Color _dotColor     = new Color(0.9f,  0.3f,  0.3f,  0.25f);

        // ── Lifecycle ───────────────────────────────────────────────────────────

        private void OnEnable()
        {
            RefreshCardList();
        }

        private void RefreshCardList()
        {
            _allCards = CardAssetSearch.LoadAll<CardSO>()
                .OrderBy(c => c.CardType.ToString())
                .ThenBy(c => c.Name)
                .ToList();
        }

        // ── Drawing ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            InitStyles();

            DrawToolbar();
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawDivider();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            DrawBottomBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Deck selector
            EditorGUILayout.LabelField("Deck:", GUILayout.Width(38));
            var newDeck = (DeckDefinitionSO)EditorGUILayout.ObjectField(
                _targetDeck, typeof(DeckDefinitionSO), false, GUILayout.Width(180));

            if (newDeck != _targetDeck)
                LoadDeck(newDeck);

            GUILayout.FlexibleSpace();

            // Filter by type
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(38));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField,
                GUILayout.Width(140));

            if (GUILayout.Button("All",    EditorStyles.toolbarButton, GUILayout.Width(30))) _typeFilter = null;
            if (GUILayout.Button("⚔",     EditorStyles.toolbarButton, GUILayout.Width(24))) _typeFilter = CardType.Weapon;
            if (GUILayout.Button("💥",    EditorStyles.toolbarButton, GUILayout.Width(24))) _typeFilter = CardType.Combo;
            if (GUILayout.Button("✦",     EditorStyles.toolbarButton, GUILayout.Width(24))) _typeFilter = CardType.Action;
            if (GUILayout.Button("☠",     EditorStyles.toolbarButton, GUILayout.Width(24))) _typeFilter = CardType.DOT;

            if (GUILayout.Button("↺ Refresh", EditorStyles.toolbarButton, GUILayout.Width(72)))
                RefreshCardList();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.45f));
            EditorGUILayout.LabelField("All Cards", _headerStyle);

            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            var filtered = _allCards
                .Where(c => string.IsNullOrEmpty(_searchFilter) ||
                            c.Name.ToLower().Contains(_searchFilter.ToLower()))
                .Where(c => _typeFilter == null || c.CardType == _typeFilter)
                .ToList();

            foreach (var card in filtered)
                DrawAvailableCard(card);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawAvailableCard(CardSO card)
        {
            var bgColor = GetCardColor(card.CardType);
            var bgRect  = EditorGUILayout.BeginHorizontal(_cardButtonStyle);
            EditorGUI.DrawRect(bgRect, bgColor);

            // Art thumbnail
            DrawMiniThumb(card, 28);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(card.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"{card.CardType}  ·  {DamageLabel(card)}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUI.enabled = _targetDeck != null;
            if (GUILayout.Button("+ Add", GUILayout.Width(52)))
                AddCardToDeck(card);
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(1);
        }

        private void DrawDivider()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(2),
                GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            int total = _workingEntries.Sum(e => e.Count);
            EditorGUILayout.LabelField(
                _targetDeck != null ? $"Deck: {_targetDeck.name}  ({total} cards)" : "← Select a deck",
                _headerStyle);

            if (_targetDeck == null)
            {
                EditorGUILayout.HelpBox("Select a DeckDefinitionSO from the toolbar to start editing.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawDeckStats(total);
            EditorGUILayout.Space(4);

            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
            for (int i = _workingEntries.Count - 1; i >= 0; i--)
                DrawDeckEntry(i);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawDeckStats(int total)
        {
            // Type distribution bar
            if (total == 0) return;

            int weapons = _workingEntries.Where(e => e.Card != null && e.Card.CardType == CardType.Weapon).Sum(e => e.Count);
            int combos  = _workingEntries.Where(e => e.Card != null && e.Card.CardType == CardType.Combo).Sum(e => e.Count);
            int actions = _workingEntries.Where(e => e.Card != null && e.Card.CardType == CardType.Action).Sum(e => e.Count);
            int dots    = _workingEntries.Where(e => e.Card != null && e.Card.CardType == CardType.DOT).Sum(e => e.Count);

            EditorGUILayout.LabelField("Card Type Distribution", EditorStyles.miniLabel);
            var barRect = EditorGUILayout.GetControlRect(false, 14);
            DrawDistributionBar(barRect, total, weapons, combos, actions, dots);

            EditorGUILayout.LabelField(
                $"⚔ Weapon: {weapons}   💥 Combo: {combos}   ✦ Action: {actions}   ☠ DOT: {dots}",
                EditorStyles.centeredGreyMiniLabel);
        }

        private static void DrawDistributionBar(Rect bar, int total, int w, int co, int a, int d)
        {
            float x = bar.x;
            void Segment(int count, Color color)
            {
                if (count <= 0) return;
                float width = (float)count / total * bar.width;
                EditorGUI.DrawRect(new Rect(x, bar.y, width, bar.height), color);
                x += width;
            }
            Segment(w,  new Color(0.3f, 0.5f, 1.0f, 0.9f));
            Segment(co, new Color(1.0f, 0.8f, 0.0f, 0.9f));
            Segment(a,  new Color(0.3f, 0.8f, 0.4f, 0.9f));
            Segment(d,  new Color(0.9f, 0.3f, 0.3f, 0.9f));
        }

        private void DrawDeckEntry(int index)
        {
            var entry  = _workingEntries[index];
            var bgRect = EditorGUILayout.BeginHorizontal(_cardButtonStyle);
            if (entry.Card != null)
                EditorGUI.DrawRect(bgRect, GetCardColor(entry.Card.CardType));

            DrawMiniThumb(entry.Card, 22);

            EditorGUILayout.LabelField(
                entry.Card != null ? entry.Card.Name : "(missing card)",
                entry.Card != null ? EditorStyles.boldLabel : EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            // Count stepper
            if (GUILayout.Button("−", GUILayout.Width(22)))
            {
                if (entry.Count <= 1)
                    RemoveCardFromDeck(index);
                else
                    SetEntryCount(index, entry.Count - 1);
            }

            EditorGUILayout.LabelField(entry.Count.ToString(), EditorStyles.centeredGreyMiniLabel,
                GUILayout.Width(24));

            if (GUILayout.Button("+", GUILayout.Width(22)))
                SetEntryCount(index, entry.Count + 1);

            if (GUILayout.Button("✕", GUILayout.Width(22)))
                RemoveCardFromDeck(index);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(1);
        }

        private void DrawBottomBar()
        {
            if (_targetDeck == null || !_isDirty) return;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("⚠ Unsaved changes", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Discard", EditorStyles.toolbarButton, GUILayout.Width(72)))
                LoadDeck(_targetDeck);

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(72)))
                SaveDeck();

            EditorGUILayout.EndHorizontal();
        }

        // ── Operations ──────────────────────────────────────────────────────────

        private void LoadDeck(DeckDefinitionSO deck)
        {
            if (_isDirty && _targetDeck != null)
            {
                bool discard = EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    $"You have unsaved changes to '{_targetDeck.name}'. Discard them?",
                    "Discard Changes",
                    "Keep Editing");
                if (!discard) return;
            }

            _targetDeck     = deck;
            _workingEntries = deck != null
                ? deck.Cards.Select(e => new DeckDefinitionSO.CardEntry
                    { Card = e.Card, Count = e.Count }).ToList()
                : new List<DeckDefinitionSO.CardEntry>();
            _isDirty = false;
        }

        private void SaveDeck()
        {
            if (_targetDeck == null) return;

            int total = _workingEntries.Sum(e => e.Count);
            if (total < 10)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Small Deck Warning",
                    $"This deck only has {total} cards (minimum recommended: 10). Save anyway?",
                    "Save Anyway",
                    "Keep Editing");
                if (!proceed) return;
            }

            Undo.RecordObject(_targetDeck, "Save Deck");
            _targetDeck.Cards = new List<DeckDefinitionSO.CardEntry>(_workingEntries);
            EditorUtility.SetDirty(_targetDeck);
            AssetDatabase.SaveAssets();
            _isDirty = false;
            Debug.Log($"[ThroneOfTides] Deck '{_targetDeck.name}' saved — {total} cards.");
        }

        private void AddCardToDeck(CardSO card)
        {
            int existing = _workingEntries.FindIndex(e => e.Card == card);
            if (existing >= 0)
                SetEntryCount(existing, _workingEntries[existing].Count + 1);
            else
                _workingEntries.Add(new DeckDefinitionSO.CardEntry { Card = card, Count = 1 });
            _isDirty = true;
        }

        private void RemoveCardFromDeck(int index)
        {
            _workingEntries.RemoveAt(index);
            _isDirty = true;
        }

        private void SetEntryCount(int index, int count)
        {
            var entry     = _workingEntries[index];
            entry.Count   = count;
            _workingEntries[index] = entry;
            _isDirty = true;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private void InitStyles()
        {
            _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 13, margin = new RectOffset(4, 4, 4, 4) };

            _cardButtonStyle ??= new GUIStyle(GUIStyle.none)
                { margin = new RectOffset(2, 2, 1, 1), padding = new RectOffset(4, 4, 3, 3) };

            _sectionStyle ??= new GUIStyle(EditorStyles.helpBox);
        }

        private static void DrawMiniThumb(CardSO card, int size)
        {
            Texture2D tex = null;
            if (card?.Art != null)
                tex = AssetPreview.GetAssetPreview(card.Art) ?? AssetPreview.GetMiniThumbnail(card.Art);

            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (tex != null) GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
            else             EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f, 0.5f));
        }

        private static Color GetCardColor(CardType type) => type switch
        {
            CardType.Weapon => _weaponColor,
            CardType.Combo  => _comboColor,
            CardType.Action => _actionColor,
            CardType.DOT    => _dotColor,
            _               => Color.clear
        };

        private static string DamageLabel(CardSO card) => card.CardType switch
        {
            CardType.DOT    => $"{card.DotDamagePerTurn} dmg/turn × {card.DotDuration}t",
            CardType.Combo  => $"{card.Damage} + {card.ComboDamage} combo",
            CardType.Action => "effect",
            _               => $"{card.Damage} dmg"
        };
    }
}