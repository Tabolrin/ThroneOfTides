using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using ThroneOfTides.Systems;

namespace ThroneOfTides.Tools
{
    // Scene view overlay — draws labeled gizmos for every VFXSpawnPoint in the scene
    // and shows a live legend panel in the Scene view corner
    [Overlay(typeof(SceneView), "VFX Spawn Points", true)]
    [Icon("Assets/Editor Default Resources/Icons/d_ParticleSystem Icon.png")]
    public class VFXSpawnPointOverlay : IMGUIOverlay, ITransientOverlay
    {
        // Colour per spawn point type — matches CardVFXHandler usage intent
        private static readonly Color _hitColor     = new Color(1.00f, 0.25f, 0.25f, 1f); // red
        private static readonly Color _deckColor    = new Color(1.00f, 0.80f, 0.10f, 1f); // yellow
        private static readonly Color _frontColor   = new Color(0.25f, 0.70f, 1.00f, 1f); // blue
        private static readonly Color _surfaceColor = new Color(0.25f, 0.90f, 0.45f, 1f); // green

        private static readonly float _gizmoRadius  = 0.12f;
        private static readonly float _labelOffset  = 0.22f;

        // ITransientOverlay — hide the panel when no VFXSpawnPoints exist in scene
        public bool visible => Object.FindObjectsByType<VFXSpawnPoint>(FindObjectsSortMode.None).Length > 0;

        // ── IMGUI panel content ─────────────────────────────────────────────

        public override void OnGUI()
        {
            EditorGUILayout.LabelField("Spawn Point Legend", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            DrawLegendRow("ShipHit",    _hitColor);
            DrawLegendRow("ShipDeck",   _deckColor);
            DrawLegendRow("ShipFront",  _frontColor);
            DrawLegendRow("SeaSurface", _surfaceColor);

            EditorGUILayout.Space(4);

            var points = Object.FindObjectsByType<VFXSpawnPoint>(FindObjectsSortMode.None);
            EditorGUILayout.LabelField($"Total in scene: {points.Length}",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(2);

            // Ping all spawn points in hierarchy on button click
            if (GUILayout.Button("Select All Spawn Points"))
            {
                var gos = System.Array.ConvertAll(points, p => (Object)p.gameObject);
                Selection.objects = gos;
            }
        }

        private static void DrawLegendRow(string label, Color color)
        {
            EditorGUILayout.BeginHorizontal();

            // Coloured swatch
            var swatchRect = EditorGUILayout.GetControlRect(false, 14, GUILayout.Width(14));
            EditorGUI.DrawRect(swatchRect, color);

            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ── Scene view gizmo drawing ────────────────────────────────────────

        // DrawGizmos is called by the SceneView for every repaint
        // Registered via InitializeOnLoad so it persists without a selected object
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable,
            typeof(VFXSpawnPoint))]
        private static void DrawVFXSpawnGizmo(VFXSpawnPoint point, GizmoType gizmoType)
        {
            Color color = point.Type switch
            {
                VFXSpawnPoint.SpawnPointType.ShipHit    => _hitColor,
                VFXSpawnPoint.SpawnPointType.ShipDeck   => _deckColor,
                VFXSpawnPoint.SpawnPointType.ShipFront  => _frontColor,
                VFXSpawnPoint.SpawnPointType.SeaSurface => _surfaceColor,
                _                                       => Color.white
            };

            Vector3 pos = point.transform.position;

            // Solid disc at the spawn position
            Handles.color = color;
            Handles.DrawSolidDisc(pos, Vector3.forward, _gizmoRadius);

            // Slightly larger wireframe ring so it reads against any background
            Handles.color = new Color(color.r, color.g, color.b, 0.4f);
            Handles.DrawWireDisc(pos, Vector3.forward, _gizmoRadius + 0.04f);

            // Label above the disc
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal    = { textColor = color },
                fontStyle = FontStyle.Bold,
                fontSize  = 9
            };

            Handles.Label(pos + Vector3.up * _labelOffset, (string)point.Type.ToString(), labelStyle);
            
            // When selected, draw a line to parent ship for context
            if ((gizmoType & GizmoType.Selected) != 0 && point.transform.parent != null)
            {
                Handles.color = new Color(color.r, color.g, color.b, 0.3f);
                Handles.DrawDottedLine(pos, point.transform.parent.position, 3f);
            }
        }
    }
}