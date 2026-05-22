using UnityEditor;
using UnityEngine;
using ThroneOfTides.Data;

namespace ThroneOfTides.Tools
{
    [CustomEditor(typeof(DeckDefinitionSO))]
    public class DeckDefinitionSOInspector : Editor
    {
        private const int MinViableDeckSize = 10;
        private SerializedProperty _cards;

        private void OnEnable()
        {
            _cards = serializedObject.FindProperty("Cards");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            int total      = CalculateTotalCount();
            bool hasNulls  = HasNullEntries();

            DrawSummaryHeader(total, hasNulls);
            EditorGUILayout.Space(6);
            DrawValidation(total, hasNulls);
            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(_cards, new GUIContent("Card Entries"), true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSummaryHeader(int total, bool hasNulls)
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            Color bgColor = hasNulls         ? new Color(0.6f, 0.1f, 0.1f, 0.3f)
                          : total < MinViableDeckSize ? new Color(0.6f, 0.5f, 0.0f, 0.3f)
                          : new Color(0.1f, 0.5f, 0.2f, 0.3f);

            var rect = EditorGUILayout.GetControlRect(false, 36);
            EditorGUI.DrawRect(rect, bgColor);
            EditorGUI.LabelField(rect, $"Total Cards in Deck:  {total}", style);
        }

        private void DrawValidation(int total, bool hasNulls)
        {
            if (hasNulls)
                EditorGUILayout.HelpBox("One or more entries have no card assigned. Fix these before building.", MessageType.Error);

            if (total == 0)
                EditorGUILayout.HelpBox("Deck is empty — no cards will be dealt.", MessageType.Warning);
            else if (total < MinViableDeckSize)
                EditorGUILayout.HelpBox($"Deck has only {total} cards. Minimum recommended is {MinViableDeckSize}.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox($"Deck looks good — {total} cards ready.", MessageType.Info);
        }

        private int CalculateTotalCount()
        {
            int total = 0;
            for (int i = 0; i < _cards.arraySize; i++)
            {
                var entry = _cards.GetArrayElementAtIndex(i);
                total += entry.FindPropertyRelative("Count").intValue;
            }
            return total;
        }

        private bool HasNullEntries()
        {
            for (int i = 0; i < _cards.arraySize; i++)
            {
                var entry = _cards.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("Card").objectReferenceValue == null)
                    return true;
            }
            return false;
        }
    }
}