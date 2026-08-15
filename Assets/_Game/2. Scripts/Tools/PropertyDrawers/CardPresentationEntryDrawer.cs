using UnityEditor;
using UnityEngine;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Tools
{
    // Draws a CardPresentationEntry as a bordered block of its own fields, hiding
    // World Particle Prefab unless Presentation Mode is Ui Sprite With World Particle.
    [CustomPropertyDrawer(typeof(CardPresentationEntry))]
    public class CardPresentationEntryDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float Spacing = 2f;
        private const float BoxPadding = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool showParticle = IsUiWithParticleMode(property);
            var sfx = property.FindPropertyRelative("_sfx");

            float height = BoxPadding * 2f;
            int simpleRowCount = showParticle ? 6 : 5; // caster, mode, anchor side, anchor point, sprite (+ particle)
            height += simpleRowCount * (LineHeight + Spacing);
            height += EditorGUI.GetPropertyHeight(sfx, true) + Spacing; // SFX foldout - variable height
            height += LineHeight + Spacing; // lifetime

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var casterFilter        = property.FindPropertyRelative("_casterFilter");
            var presentationMode    = property.FindPropertyRelative("_presentationMode");
            var anchorSide          = property.FindPropertyRelative("_anchorSide");
            var positionType        = property.FindPropertyRelative("_positionType");
            var spritePrefab        = property.FindPropertyRelative("_spritePrefab");
            var worldParticlePrefab = property.FindPropertyRelative("_worldParticlePrefab");
            var sfx                 = property.FindPropertyRelative("_sfx");
            var lifetime            = property.FindPropertyRelative("_lifetime");

            bool showParticle = IsUiWithParticleMode(property);

            GUI.Box(position, GUIContent.none);

            float x = position.x + BoxPadding;
            float width = position.width - BoxPadding * 2f;
            float y = position.y + BoxPadding;

            DrawField(ref y, x, width, casterFilter, "Caster");
            DrawField(ref y, x, width, presentationMode, "Mode");
            DrawField(ref y, x, width, anchorSide, "Anchor Side");
            DrawField(ref y, x, width, positionType, "Anchor Point");
            DrawField(ref y, x, width, spritePrefab, "Sprite Prefab");

            if (showParticle)
                DrawField(ref y, x, width, worldParticlePrefab, "World Particle");

            DrawVariableHeightField(ref y, x, width, sfx, "SFX");
            DrawField(ref y, x, width, lifetime, "Lifetime");

            EditorGUI.EndProperty();
        }

        private static bool IsUiWithParticleMode(SerializedProperty property)
        {
            var presentationMode = property.FindPropertyRelative("_presentationMode");
            return presentationMode.enumValueIndex == (int)CardPresentationMode.UiSpriteWithWorldParticle;
        }

        // Custom labels replace a property's default display name, which would otherwise
        // silently drop its [Tooltip] text too - pulling property.tooltip through keeps it.
        private static void DrawField(ref float y, float x, float width, SerializedProperty prop, string label)
        {
            var rect = new Rect(x, y, width, LineHeight);
            EditorGUI.PropertyField(rect, prop, new GUIContent(label, prop.tooltip), true);
            y += LineHeight + Spacing;
        }

        private static void DrawVariableHeightField(ref float y, float x, float width, SerializedProperty prop, string label)
        {
            float height = EditorGUI.GetPropertyHeight(prop, true);
            var rect = new Rect(x, y, width, height);
            EditorGUI.PropertyField(rect, prop, new GUIContent(label, prop.tooltip), true);
            y += height + Spacing;
        }
    }
}
