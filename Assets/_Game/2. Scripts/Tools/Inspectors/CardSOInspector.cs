// Assets/_Game/2. Scripts/Tools/Inspectors/CardSOInspector.cs
using UnityEditor;
using UnityEngine;
using ThroneOfTides.Data;
using ThroneOfTides.Core;

namespace ThroneOfTides.Tools
{
    [CustomEditor(typeof(CardSO))]
    public class CardSOInspector : Editor
    {
        private SerializedProperty _name;
        private SerializedProperty _description;
        private SerializedProperty _art;
        private SerializedProperty _cardType;
        private SerializedProperty _manaCost;
        private SerializedProperty _storageCost;
        private SerializedProperty _hpCost;
        private SerializedProperty _damage;
        private SerializedProperty _isEligibleAsActionPair;
        private SerializedProperty _actionEffect;
        private SerializedProperty _comboDamage;
        private SerializedProperty _comboStackBonus;
        private SerializedProperty _comboPartner;
        private SerializedProperty _dotDamagePerTurn;
        private SerializedProperty _dotDuration;
        private SerializedProperty _presentationEntries;
        private SerializedProperty _requiresTargetSelection;
        private SerializedProperty _statusType;
        private SerializedProperty _aiPlayBeforeAttack;
        private SerializedProperty _id;

        private static readonly Color WeaponColor   = new Color(0.22f, 0.38f, 0.62f, 0.18f);
        private static readonly Color ComboColor    = new Color(0.72f, 0.62f, 0.10f, 0.18f);
        private static readonly Color ActionColor   = new Color(0.20f, 0.60f, 0.28f, 0.18f);
        private static readonly Color DotColor      = new Color(0.65f, 0.18f, 0.18f, 0.18f);
        private static readonly Color ReactionColor = new Color(0.55f, 0.20f, 0.75f, 0.18f);

        private void OnEnable()
        {
            _name                   = serializedObject.FindProperty("_name");
            _description            = serializedObject.FindProperty("_description");
            _art                    = serializedObject.FindProperty("_art");
            _cardType               = serializedObject.FindProperty("_cardType");
            _manaCost               = serializedObject.FindProperty("_manaCost");
            _storageCost            = serializedObject.FindProperty("_storageCost");
            _hpCost                 = serializedObject.FindProperty("_hpCost");
            _damage                 = serializedObject.FindProperty("_damage");
            _isEligibleAsActionPair = serializedObject.FindProperty("_isEligibleAsActionPair");
            _actionEffect           = serializedObject.FindProperty("_actionEffect");
            _comboDamage            = serializedObject.FindProperty("_comboDamage");
            _comboStackBonus        = serializedObject.FindProperty("_comboStackBonus");
            _comboPartner           = serializedObject.FindProperty("_comboPartner");
            _dotDamagePerTurn       = serializedObject.FindProperty("_dotDamagePerTurn");
            _dotDuration            = serializedObject.FindProperty("_dotDuration");
            _presentationEntries    = serializedObject.FindProperty("_presentationEntries");
            _requiresTargetSelection = serializedObject.FindProperty("_requiresTargetSelection");
            _statusType              = serializedObject.FindProperty("_statusType");
            _aiPlayBeforeAttack      = serializedObject.FindProperty("_aiPlayBeforeAttack");
            _id                      = serializedObject.FindProperty("_id");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var cardType = (CardType)_cardType.enumValueIndex;

            DrawTintedBackground(cardType);
            DrawArtPreview();
            EditorGUILayout.Space(4);
            DrawValidation(cardType);
            EditorGUILayout.Space(6);
            DrawFields(cardType);

            serializedObject.ApplyModifiedProperties();
        }

        // ── Section drawing ────────────────────────────────────────────────────

        private void DrawTintedBackground(CardType type)
        {
            Color tint = type switch
            {
                CardType.Weapon   => WeaponColor,
                CardType.Combo    => ComboColor,
                CardType.Action   => ActionColor,
                CardType.DOT      => DotColor,
                CardType.Reaction => ReactionColor,
                _                 => Color.clear
            };
            EditorGUI.DrawRect(new Rect(0, 0, Screen.width, Screen.height), tint);
        }

        private void DrawArtPreview()
        {
            var sprite  = _art.objectReferenceValue as Sprite;
            var preview = sprite != null
                ? AssetPreview.GetAssetPreview(sprite) ?? AssetPreview.GetMiniThumbnail(sprite)
                : null;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (preview != null)
            {
                GUILayout.Label(preview, GUILayout.Width(120), GUILayout.Height(120));
            }
            else
            {
                var rect = GUILayoutUtility.GetRect(120, 120, GUILayout.Width(120));
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                EditorGUI.LabelField(rect, "No Art",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 });
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidation(CardType type)
        {
            if (string.IsNullOrWhiteSpace(_name.stringValue))
                EditorGUILayout.HelpBox("Card has no name.", MessageType.Error);

            if (string.IsNullOrWhiteSpace(_description.stringValue))
                EditorGUILayout.HelpBox("Description is empty.", MessageType.Warning);

            if (_art.objectReferenceValue == null)
                EditorGUILayout.HelpBox("No art assigned.", MessageType.Warning);

            if ((type == CardType.Action || type == CardType.Reaction)
                && _actionEffect.objectReferenceValue == null)
                EditorGUILayout.HelpBox("No Effect SO — card will do nothing.", MessageType.Error);

            if (type == CardType.Combo && _comboPartner.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Combo card has no partner.", MessageType.Error);

            if (type == CardType.DOT
                && (_dotDuration.intValue == 0 || _dotDamagePerTurn.intValue == 0))
                EditorGUILayout.HelpBox("DOT has zero duration or damage.", MessageType.Warning);

            if (_storageCost.intValue == 0)
                EditorGUILayout.HelpBox("Storage cost is 0 — intentional?", MessageType.Info);
        }

        // Custom labels replace a property's default display name, which would otherwise
        // silently drop its [Tooltip] text too — pulling property.tooltip through keeps it.
        private static GUIContent Label(SerializedProperty property, string text)
            => new GUIContent(text, property.tooltip);

        private void DrawFields(CardType type)
        {
            // ── Identity ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_name,        Label(_name, "Card Name"));
            EditorGUILayout.PropertyField(_id,          Label(_id, "Card Id (code identity — leave None only for unused/generic cards)"));
            EditorGUILayout.PropertyField(_description, Label(_description, "Description"));
            EditorGUILayout.PropertyField(_cardType,    Label(_cardType, "Card Type"));
            EditorGUILayout.Space(6);

            // ── Cost ──────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Cost", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_manaCost,    Label(_manaCost, "Mana Cost"));
            EditorGUILayout.PropertyField(_storageCost, Label(_storageCost, "Storage Cost (deck slots)"));

            // HP cost only shown when already non-zero or when the card type
            // could plausibly have one — keeps the inspector uncluttered
            if (_hpCost.intValue > 0 || type == CardType.Weapon || type == CardType.Action)
                EditorGUILayout.PropertyField(_hpCost, Label(_hpCost, "HP Cost (0 = none)"));

            EditorGUILayout.Space(6);

            // ── Visuals ───────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_art, Label(_art, "Card Art"));
            EditorGUILayout.HelpBox(
                "The type symbol icon is defined on the CardTypePaletteSO asset, not per card. " +
                "Banner background color no longer varies by type — only the Port deck-list row still uses a type band color.",
                MessageType.None);
            EditorGUILayout.Space(6);

            // ── Ship Status ───────────────────────────────────────────────────
            EditorGUILayout.LabelField("Ship Status", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_statusType, Label(_statusType, "Status Icon"));
            EditorGUILayout.PropertyField(_requiresTargetSelection, Label(_requiresTargetSelection, "Requires Target Selection"));
            EditorGUILayout.Space(6);

            // ── Play Presentation ─────────────────────────────────────────────
            EditorGUILayout.LabelField("Play Presentation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_presentationEntries, Label(_presentationEntries, "Presentation Entries"), true);
            EditorGUILayout.Space(6);

            // ── Type-specific fields ──────────────────────────────────────────
            switch (type)
            {
                case CardType.Weapon:
                    EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_damage, Label(_damage, "Damage"));
                    EditorGUILayout.PropertyField(_actionEffect, Label(_actionEffect, "Effect (optional — overrides legacy weapon logic)"));
                    break;

                case CardType.Combo:
                    EditorGUILayout.LabelField("Combo", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_damage,          Label(_damage, "Base Damage"));
                    EditorGUILayout.PropertyField(_comboDamage,     Label(_comboDamage, "Combo Bonus Damage"));
                    EditorGUILayout.PropertyField(_comboStackBonus, Label(_comboStackBonus, "Stack Bonus per Primer"));
                    EditorGUILayout.PropertyField(_comboPartner,    Label(_comboPartner, "Partner Card"));
                    break;

                case CardType.Action:
                    EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_actionEffect,           Label(_actionEffect, "Effect"));
                    EditorGUILayout.PropertyField(_isEligibleAsActionPair, Label(_isEligibleAsActionPair, "Can Pair With Damage Card"));
                    EditorGUILayout.PropertyField(_aiPlayBeforeAttack,     Label(_aiPlayBeforeAttack, "AI: Play Before Attack"));
                    break;

                case CardType.Reaction:
                    EditorGUILayout.LabelField("Reaction", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_actionEffect, Label(_actionEffect, "Charge Effect SO"));
                    EditorGUILayout.HelpBox(
                        "Reaction cards animate to the charge slot on draw — they never enter the hand.\n" +
                        "The Charge Effect SO adds a charge to GameState.",
                        MessageType.Info);
                    break;

                case CardType.DOT:
                    EditorGUILayout.LabelField("Damage Over Time", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_damage,           Label(_damage, "Initial Hit Damage"));
                    EditorGUILayout.PropertyField(_dotDamagePerTurn, Label(_dotDamagePerTurn, "Damage Per Turn"));
                    EditorGUILayout.PropertyField(_dotDuration,      Label(_dotDuration, "Duration (turns)"));
                    break;
            }
        }
    }
}
