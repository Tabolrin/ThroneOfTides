// Assets/_Game/Scripts/Tools/Windows/ShipOscillatorOverlay.cs
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using ThroneOfTides.Systems;

namespace ThroneOfTides.Tools
{
    // Scene view overlay — appears when a GameObject with ShipOscillator is selected
    // Allows live tuning of oscillation values with immediate visual feedback in Play Mode
    [Overlay(typeof(SceneView), "Ship Oscillator Tuner", true)]
    public class ShipOscillatorOverlay : IMGUIOverlay, ITransientOverlay
    {
        // ITransientOverlay — only show when a ShipOscillator is selected
        public bool visible
        {
            get
            {
                if (Selection.activeGameObject == null) return false;
                return Selection.activeGameObject.GetComponent<ShipOscillator>() != null;
            }
        }

        // ── IMGUI panel ─────────────────────────────────────────────────────

        public override void OnGUI()
        {
            if (Selection.activeGameObject == null) return;

            var oscillator = Selection.activeGameObject.GetComponent<ShipOscillator>();
            if (oscillator == null) return;

            var so = new SerializedObject(oscillator);
            so.Update();

            EditorGUILayout.LabelField(
                $"Tuning: {oscillator.gameObject.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawAxis(so,
                "Vertical Bob",
                "_shouldDriftVert", "_yAmplitude", "_yDuration",
                new Color(0.3f, 0.8f, 1.0f, 1f));

            EditorGUILayout.Space(4);

            DrawAxis(so,
                "Horizontal Drift",
                "_shouldDriftHoriz", "_xAmplitude", "_xDuration",
                new Color(0.4f, 1.0f, 0.5f, 1f));

            EditorGUILayout.Space(4);

            DrawAxis(so,
                "Rotation Sway",
                "_shouldRotate", "_rotAmplitude", "_rotDuration",
                new Color(1.0f, 0.8f, 0.3f, 1f));

            bool changed = so.ApplyModifiedProperties();

            EditorGUILayout.Space(6);

            if (Application.isPlaying)
            {
                // Restart tweens live so changes are visible immediately
                if (changed || GUILayout.Button("Restart Oscillation"))
                    RestartOscillation(oscillator);

                EditorGUILayout.LabelField(
                    "✓ Live — changes apply immediately",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                        { normal = { textColor = new Color(0.4f, 0.9f, 0.4f) } });
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("Restart Oscillation");
                GUI.enabled = true;

                EditorGUILayout.LabelField(
                    "Enter Play Mode to preview live",
                    EditorStyles.centeredGreyMiniLabel);
            }
        }

        // ── Per-axis section ────────────────────────────────────────────────

        private static void DrawAxis(SerializedObject so,
            string label,
            string enabledProp,
            string ampProp,
            string durProp,
            Color accentColor)
        {
            var enabled   = so.FindProperty(enabledProp);
            var amplitude = so.FindProperty(ampProp);
            var duration  = so.FindProperty(durProp);

            // Coloured section header with enable toggle inline
            EditorGUILayout.BeginHorizontal();

            // Accent strip
            var stripRect = EditorGUILayout.GetControlRect(false, 14, GUILayout.Width(4));
            EditorGUI.DrawRect(stripRect, accentColor);

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            enabled.boolValue = EditorGUILayout.Toggle(enabled.boolValue, GUILayout.Width(16));
            EditorGUILayout.EndHorizontal();

            // Disable controls when axis is toggled off
            GUI.enabled = enabled.boolValue;

            // Amplitude slider — clamped to sensible range per axis
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Amplitude", EditorStyles.miniLabel, GUILayout.Width(64));
            amplitude.floatValue = EditorGUILayout.Slider(amplitude.floatValue, 0f, 2f);
            EditorGUILayout.EndHorizontal();

            // Duration slider
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Duration",  EditorStyles.miniLabel, GUILayout.Width(64));
            duration.floatValue = EditorGUILayout.Slider(duration.floatValue, 0.3f, 8f);
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        // ── Live tween restart ──────────────────────────────────────────────

        // Kills all active DOTween tweens on the ship and restarts oscillation
        // with current serialized values — only valid in Play Mode
        private static void RestartOscillation(ShipOscillator oscillator)
        {
            // Kill existing tweens via DOTween
            DG.Tweening.DOTween.Kill(oscillator.transform);

            // Reset to start position before restarting so amplitude reads correctly
            // We use SendMessage to call the private StartOscillation method
            // TODO: expose a public Restart() method on ShipOscillator for a cleaner call
            oscillator.SendMessage("StartOscillation", SendMessageOptions.DontRequireReceiver);
        }
    }
}