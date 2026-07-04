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
        private SerializedProperty _cardArtAnimator;

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
            _cardArtAnimator        = serializedObject.FindProperty("_cardArtAnimator");
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

        private void DrawFields(CardType type)
        {
            // ── Identity ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_name,        new GUIContent("Card Name"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));
            EditorGUILayout.PropertyField(_cardType,    new GUIContent("Card Type"));
            EditorGUILayout.Space(6);

            // ── Cost ──────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Cost", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_manaCost,    new GUIContent("Mana Cost"));
            EditorGUILayout.PropertyField(_storageCost, new GUIContent("Storage Cost (deck slots)"));

            // HP cost only shown when already non-zero or when the card type
            // could plausibly have one — keeps the inspector uncluttered
            if (_hpCost.intValue > 0 || type == CardType.Weapon || type == CardType.Action)
                EditorGUILayout.PropertyField(_hpCost, new GUIContent("HP Cost (0 = none)"));

            EditorGUILayout.Space(6);

            // ── Visuals ───────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_art,             new GUIContent("Card Art"));
            EditorGUILayout.PropertyField(_cardArtAnimator, new GUIContent("Art Animator (Eldar)"));
            EditorGUILayout.HelpBox(
                "Type symbol and banner colours are defined on the CardTypePaletteSO asset — not per card.",
                MessageType.None);
            EditorGUILayout.Space(6);

            // ── Type-specific fields ──────────────────────────────────────────
            switch (type)
            {
                case CardType.Weapon:
                    EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_damage, new GUIContent("Damage"));
                    break;

                case CardType.Combo:
                    EditorGUILayout.LabelField("Combo", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_damage,          new GUIContent("Base Damage"));
                    EditorGUILayout.PropertyField(_comboDamage,     new GUIContent("Combo Bonus Damage"));
                    EditorGUILayout.PropertyField(_comboStackBonus, new GUIContent("Stack Bonus per Primer"));
                    EditorGUILayout.PropertyField(_comboPartner,    new GUIContent("Partner Card"));
                    break;

                case CardType.Action:
                    EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_actionEffect,           new GUIContent("Effect"));
                    EditorGUILayout.PropertyField(_isEligibleAsActionPair, new GUIContent("Can Pair With Damage Card"));
                    break;

                case CardType.Reaction:
                    EditorGUILayout.LabelField("Reaction", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_actionEffect, new GUIContent("Charge Effect SO"));
                    EditorGUILayout.HelpBox(
                        "Reaction cards animate to the charge slot on draw — they never enter the hand.\n" +
                        "The Charge Effect SO adds a charge to GameState.",
                        MessageType.Info);
                    break;

                case CardType.DOT:
                    EditorGUILayout.LabelField("Damage Over Time", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_damage,           new GUIContent("Initial Hit Damage"));
                    EditorGUILayout.PropertyField(_dotDamagePerTurn, new GUIContent("Damage Per Turn"));
                    EditorGUILayout.PropertyField(_dotDuration,      new GUIContent("Duration (turns)"));
                    break;
            }
        }
    }
}