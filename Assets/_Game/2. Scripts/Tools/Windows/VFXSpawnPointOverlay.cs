using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using ThroneOfTides.Systems;

namespace ThroneOfTides.Tools
{
    // Scene view overlay — draws labeled gizmos for every VFXSpawnPoint in the scene
    // and shows a live legend panel in the Scene view corner.
    [Overlay(typeof(SceneView), "VFX Spawn Points", true)]
    [Icon("Assets/Editor Default Resources/Icons/d_ParticleSystem Icon.png")]
    public class VFXSpawnPointOverlay : IMGUIOverlay, ITransientOverlay
    {
        // Colour per spawn point type — matches CardVFXHandler usage intent
        private static readonly Color _hitColor     = new Color(1.00f, 0.25f, 0.25f, 1f); // red
        private static readonly Color _deckColor    = new Color(1.00f, 0.80f, 0.10f, 1f); // yellow
        private static readonly Color _frontColor   = new Color(0.25f, 0.70f, 1.00f, 1f); // blue
        private static readonly Color _surfaceColor = new Color(0.25f, 0.90f, 0.45f, 1f); // green

        private const float GizmoRadius = 0.12f;
        private const float LabelOffset = 0.22f;

        // Cached query result and the time it was last refreshed.
        // FindObjectsByType is not free in larger scenes; limiting it to once per second
        // while the overlay is visible keeps repaint cost negligible.
        private VFXSpawnPosition[] _cachedPoints    = new VFXSpawnPosition[0];
        private double          _lastCacheTime   = -1.0;
        private const double    CacheIntervalSec = 1.0;

        // Lazily created and reused across repaints — GUIStyle allocates on construction.
        private GUIStyle _labelStyle;

        // ITransientOverlay — hide the panel when no VFXSpawnPoints exist in the scene
        public bool visible
        {
            get
            {
                RefreshCacheIfStale();
                return _cachedPoints.Length > 0;
            }
        }

        // ── IMGUI panel content ─────────────────────────────────────────────

        public override void OnGUI()
        {
            RefreshCacheIfStale();

            EditorGUILayout.LabelField("Spawn Point Legend", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            DrawLegendRow("ShipHit",    _hitColor);
            DrawLegendRow("ShipDeck",   _deckColor);
            DrawLegendRow("ShipFront",  _frontColor);
            DrawLegendRow("SeaSurface", _surfaceColor);

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField($"Total in scene: {_cachedPoints.Length}",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(2);

            // Ping all spawn points in hierarchy on button click
            if (GUILayout.Button("Select All Spawn Points"))
            {
                var gos = System.Array.ConvertAll(_cachedPoints, p => (Object)p.gameObject);
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

        // Throttles the FindObjectsByType call to at most once per CacheIntervalSec.
        // This is safe for editor tooling where sub-second accuracy is not required.
        private void RefreshCacheIfStale()
        {
            if (EditorApplication.timeSinceStartup - _lastCacheTime < CacheIntervalSec) return;
            _cachedPoints  = Object.FindObjectsByType<VFXSpawnPosition>(FindObjectsSortMode.None);
            _lastCacheTime = EditorApplication.timeSinceStartup;
        }

        // ── Scene view gizmo drawing ────────────────────────────────────────

        // [DrawGizmo] registers this method with the SceneView repaint loop.
        // Using the attribute instead of OnDrawGizmos means gizmos render for all
        // VFXSpawnPoints in the scene regardless of which object is selected.
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable,
            typeof(VFXSpawnPosition))]
        private static void DrawVFXSpawnGizmo(VFXSpawnPosition point, GizmoType gizmoType)
        {
            Color color = point.Type switch
            {
                VFXSpawnPosition.SpawnPositionType.ShipHit    => _hitColor,
                VFXSpawnPosition.SpawnPositionType.ShipDeck   => _deckColor,
                VFXSpawnPosition.SpawnPositionType.ShipFront  => _frontColor,
                VFXSpawnPosition.SpawnPositionType.SeaSurface => _surfaceColor,
                _                                       => Color.white
            };

            Vector3 pos = point.transform.position;

            // Solid disc at the spawn position
            Handles.color = color;
            Handles.DrawSolidDisc(pos, Vector3.forward, GizmoRadius);

            // Slightly larger wireframe ring so it reads against any background
            Handles.color = new Color(color.r, color.g, color.b, 0.4f);
            Handles.DrawWireDisc(pos, Vector3.forward, GizmoRadius + 0.04f);

            // Label above the disc — style allocated once per gizmo draw call.
            // [DrawGizmo] methods are static so the per-instance cache on the overlay
            // class is unavailable here; the allocation is editor-only and infrequent.
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal    = { textColor = color },
                fontStyle = FontStyle.Bold,
                fontSize  = 9
            };

            Handles.Label(pos + Vector3.up * LabelOffset, point.Type.ToString(), labelStyle);

            // When selected, draw a line to the parent ship for hierarchy context
            if ((gizmoType & GizmoType.Selected) != 0 && point.transform.parent != null)
            {
                Handles.color = new Color(color.r, color.g, color.b, 0.3f);
                Handles.DrawDottedLine(pos, point.transform.parent.position, 3f);
            }
        }
    }
}