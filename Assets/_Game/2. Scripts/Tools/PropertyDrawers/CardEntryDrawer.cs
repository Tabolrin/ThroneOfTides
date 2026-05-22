using UnityEditor;
using UnityEngine;
using ThroneOfTides.Data;

namespace ThroneOfTides.Tools
{
    // Draws CardEntry (Card + Count) as a compact single-row inline
    // instead of Unity's default two-field foldout
    [CustomPropertyDrawer(typeof(DeckDefinitionSO.CardEntry))]
    public class CardEntryDrawer : PropertyDrawer
    {
        private const float CountWidth   = 48f;
        private const float Spacing      = 4f;
        private const float PreviewSize  = 18f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight + 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var cardProp  = property.FindPropertyRelative("Card");
            var countProp = property.FindPropertyRelative("Count");

            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginProperty(position, label, property);

            // Thumbnail preview
            var previewRect = new Rect(position.x, position.y, PreviewSize, PreviewSize);
            var cardSO = cardProp.objectReferenceValue as CardSO;
            if (cardSO != null && cardSO.Art != null)
            {
                var tex = AssetPreview.GetAssetPreview(cardSO.Art)
                       ?? AssetPreview.GetMiniThumbnail(cardSO.Art);
                if (tex != null)
                    GUI.DrawTexture(previewRect, tex, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.3f, 0.3f, 0.3f, 0.4f));
            }

            float afterPreview  = position.x + PreviewSize + Spacing;
            float countX        = position.xMax - CountWidth;
            float cardFieldWidth= countX - afterPreview - Spacing;

            // Card object field
            var cardRect  = new Rect(afterPreview, position.y, cardFieldWidth, position.height);
            EditorGUI.PropertyField(cardRect, cardProp, GUIContent.none);

            // Count field — clamped to 1 min so designers can't accidentally set 0
            var countRect = new Rect(countX, position.y, CountWidth, position.height);
            countProp.intValue = Mathf.Max(1,
                EditorGUI.IntField(countRect, countProp.intValue));

            EditorGUI.EndProperty();
        }
    }
}