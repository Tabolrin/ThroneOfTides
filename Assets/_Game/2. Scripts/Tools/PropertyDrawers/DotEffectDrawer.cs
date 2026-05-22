using UnityEditor;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Tools
{
    // Draws DotEffect fields inline: Target | Dmg/turn | Turns
    // Requires [Serializable] on DotEffect struct
    [CustomPropertyDrawer(typeof(DotEffect))]
    public class DotEffectDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight + 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var target    = property.FindPropertyRelative("Target");
            var dmg       = property.FindPropertyRelative("DamagePerTurn");
            var turns     = property.FindPropertyRelative("TurnsRemaining");

            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginProperty(position, label, property);

            // Label
            float labelW = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(position.x, position.y, labelW, position.height);
            EditorGUI.LabelField(labelRect, label);

            // Split remaining space: Target 40% | Dmg/turn 30% | Turns 30%
            float fieldsX = position.x + labelW + 4f;
            float fieldsW = position.width - labelW - 4f;
            float col     = fieldsW / 3f;

            var targetRect = new Rect(fieldsX,          position.y, col - 4f, position.height);
            var dmgRect    = new Rect(fieldsX + col,    position.y, col - 4f, position.height);
            var turnsRect  = new Rect(fieldsX + col*2f, position.y, col,      position.height);

            EditorGUI.PropertyField(targetRect, target, GUIContent.none);

            // Friendly labels above numeric fields
            EditorGUI.LabelField(new Rect(dmgRect.x, dmgRect.y - 1, dmgRect.width, 10),
                "dmg/turn", EditorStyles.centeredGreyMiniLabel);
            dmg.intValue = Mathf.Max(0, EditorGUI.IntField(dmgRect, dmg.intValue));

            EditorGUI.LabelField(new Rect(turnsRect.x, turnsRect.y - 1, turnsRect.width, 10),
                "turns", EditorStyles.centeredGreyMiniLabel);
            turns.intValue = Mathf.Max(0, EditorGUI.IntField(turnsRect, turns.intValue));

            EditorGUI.EndProperty();
        }
    }
}