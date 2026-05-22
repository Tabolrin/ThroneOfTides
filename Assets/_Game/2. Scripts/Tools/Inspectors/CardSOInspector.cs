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
        private SerializedProperty _damage;
        private SerializedProperty _isEligibleAsActionPair;
        private SerializedProperty _actionEffect;
        private SerializedProperty _comboDamage;
        private SerializedProperty _comboStackBonus;
        private SerializedProperty _comboPartner;
        private SerializedProperty _dotDamagePerTurn;
        private SerializedProperty _dotDuration;
        private SerializedProperty _cardTypeSymbol;
        private SerializedProperty _cardArtAnimator;

        private static readonly Color _weaponColor = new Color(0.22f, 0.38f, 0.62f, 0.18f);
        private static readonly Color _comboColor  = new Color(0.72f, 0.62f, 0.10f, 0.18f);
        private static readonly Color _actionColor = new Color(0.20f, 0.60f, 0.28f, 0.18f);
        private static readonly Color _dotColor    = new Color(0.65f, 0.18f, 0.18f, 0.18f);

        private void OnEnable()
        {
            _name                  = serializedObject.FindProperty("_name");
            _description           = serializedObject.FindProperty("_description");
            _art                   = serializedObject.FindProperty("_art");
            _cardType              = serializedObject.FindProperty("_cardType");
            _damage                = serializedObject.FindProperty("_damage");
            _isEligibleAsActionPair= serializedObject.FindProperty("_isEligibleAsActionPair");
            _actionEffect          = serializedObject.FindProperty("_actionEffect");
            _comboDamage           = serializedObject.FindProperty("_comboDamage");
            _comboStackBonus       = serializedObject.FindProperty("_comboStackBonus");
            _comboPartner          = serializedObject.FindProperty("_comboPartner");
            _dotDamagePerTurn      = serializedObject.FindProperty("_dotDamagePerTurn");
            _dotDuration           = serializedObject.FindProperty("_dotDuration");
            _cardTypeSymbol        = serializedObject.FindProperty("_cardTypeSymbol");
            _cardArtAnimator       = serializedObject.FindProperty("_cardArtAnimator");
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
            DrawCoreFields(cardType);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTintedBackground(CardType cardType)
        {
            Color tint = cardType switch
            {
                CardType.Weapon => _weaponColor,
                CardType.Combo  => _comboColor,
                CardType.Action => _actionColor,
                CardType.DOT    => _dotColor,
                _               => Color.clear
            };
            // Full-width rect behind everything
            EditorGUI.DrawRect(new Rect(0, 0, Screen.width, Screen.height), tint);
        }

        private void DrawArtPreview()
        {
            var sprite = _art.objectReferenceValue as Sprite;
            Texture2D preview = sprite != null
                ? AssetPreview.GetAssetPreview(sprite) ?? AssetPreview.GetMiniThumbnail(sprite)
                : null;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (preview != null)
                GUILayout.Label(preview, GUILayout.Width(120), GUILayout.Height(120));
            else
            {
                // Placeholder box so layout stays stable even without art
                var placeholder = GUILayoutUtility.GetRect(120, 120, GUILayout.Width(120));
                EditorGUI.DrawRect(placeholder, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                EditorGUI.LabelField(placeholder, "No Art", new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    { fontSize = 11 });
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidation(CardType cardType)
        {
            if (string.IsNullOrWhiteSpace(_name.stringValue))
                EditorGUILayout.HelpBox("Card has no name — it won't display correctly in game.", MessageType.Error);

            if (string.IsNullOrWhiteSpace(_description.stringValue))
                EditorGUILayout.HelpBox("Description is empty — players won't know what this card does.", MessageType.Warning);

            if (_art.objectReferenceValue == null)
                EditorGUILayout.HelpBox("No art assigned. Card will appear blank in game.", MessageType.Warning);

            if (cardType == CardType.Combo && _comboPartner.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Combo card has no partner assigned — the combo will never trigger.", MessageType.Error);

            if (cardType == CardType.Action && _actionEffect.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Action card has no Effect SO — it will do nothing when played.", MessageType.Error);

            if (cardType == CardType.DOT && (_dotDuration.intValue == 0 || _dotDamagePerTurn.intValue == 0))
                EditorGUILayout.HelpBox("DOT card has zero duration or zero damage per turn — it will deal no damage over time.", MessageType.Warning);
        }

        private void DrawCoreFields(CardType cardType)
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_name,        new GUIContent("Card Name"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));
            EditorGUILayout.PropertyField(_cardType,    new GUIContent("Card Type"));
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_art,             new GUIContent("Card Art"));
            EditorGUILayout.PropertyField(_cardTypeSymbol,  new GUIContent("Type Symbol"));
            EditorGUILayout.PropertyField(_cardArtAnimator, new GUIContent("Art Animator"));
            EditorGUILayout.Space(6);

            switch (cardType)
            {
                case CardType.Weapon:
                    EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_damage, new GUIContent("Damage"));
                    break;

                case CardType.Combo:
                    EditorGUILayout.LabelField("Combo", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_damage,          new GUIContent("Base Damage"));
                    EditorGUILayout.PropertyField(_comboDamage,     new GUIContent("Combo Bonus Damage"));
                    EditorGUILayout.PropertyField(_comboStackBonus, new GUIContent("Stack Bonus"));
                    EditorGUILayout.PropertyField(_comboPartner,    new GUIContent("Partner Card"));
                    break;

                case CardType.Action:
                    EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_actionEffect,          new GUIContent("Effect"));
                    EditorGUILayout.PropertyField(_isEligibleAsActionPair, new GUIContent("Can Pair With Damage Card"));
                    break;

                case CardType.DOT:
                    EditorGUILayout.LabelField("DOT (Damage Over Time)", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_damage,           new GUIContent("Initial Hit Damage"));
                    EditorGUILayout.PropertyField(_dotDamagePerTurn, new GUIContent("Damage Per Turn"));
                    EditorGUILayout.PropertyField(_dotDuration,      new GUIContent("Duration (turns)"));
                    break;
            }
        }
    }
}