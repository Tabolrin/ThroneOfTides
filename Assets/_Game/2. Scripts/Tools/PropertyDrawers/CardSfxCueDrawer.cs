using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoreMountains.Tools;
using ThroneOfTides.Data;

namespace ThroneOfTides.Tools
{
    // Draws CardSfxCue as a foldout with a Playlist field and a Song dropdown populated from
    // that playlist's actual song names — avoids hand-typing a name that might not exist.
    [CustomPropertyDrawer(typeof(CardSfxCue))]
    public class CardSfxCueDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float Spacing = 2f;
        private const int FieldCountWhenExpanded = 4; // playlist, song, volume, pitch

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return LineHeight;
            return (LineHeight + Spacing) * (FieldCountWhenExpanded + 1); // +1 for the foldout row itself
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var playlistProp = property.FindPropertyRelative("_playlist");
            var songNameProp = property.FindPropertyRelative("_songName");
            var volumeProp   = property.FindPropertyRelative("_volumeMultiplier");
            var pitchProp    = property.FindPropertyRelative("_pitchMultiplier");

            var foldoutRect = new Rect(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float y = position.y + LineHeight + Spacing;
            var indented = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, LineHeight));

            EditorGUI.PropertyField(indented, playlistProp, new GUIContent("Playlist", playlistProp.tooltip));
            y += LineHeight + Spacing;

            var songRect = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, LineHeight));
            DrawSongPopup(songRect, playlistProp, songNameProp);
            y += LineHeight + Spacing;

            var volumeRect = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, LineHeight));
            EditorGUI.PropertyField(volumeRect, volumeProp, new GUIContent("Volume Multiplier", volumeProp.tooltip));
            y += LineHeight + Spacing;

            var pitchRect = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, LineHeight));
            EditorGUI.PropertyField(pitchRect, pitchProp, new GUIContent("Pitch Multiplier", pitchProp.tooltip));

            EditorGUI.EndProperty();
        }

        private static void DrawSongPopup(Rect rect, SerializedProperty playlistProp, SerializedProperty songNameProp)
        {
            var playlist = playlistProp.objectReferenceValue as MMSMPlaylist;
            var content = new GUIContent("Song", songNameProp.tooltip);

            if (playlist == null || playlist.Songs == null || playlist.Songs.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.Popup(rect, content.text, 0, new[] { "Assign a Playlist first" });
                }
                return;
            }

            var names = new List<string> { "(Random from playlist)" };
            foreach (var song in playlist.Songs)
                names.Add(string.IsNullOrEmpty(song.Name) ? "(unnamed song)" : song.Name);

            int currentIndex = string.IsNullOrEmpty(songNameProp.stringValue)
                ? 0
                : Mathf.Max(0, names.IndexOf(songNameProp.stringValue));

            int selected = EditorGUI.Popup(rect, content, currentIndex, ToContentArray(names));
            songNameProp.stringValue = selected == 0 ? string.Empty : names[selected];
        }

        private static GUIContent[] ToContentArray(List<string> names)
        {
            var content = new GUIContent[names.Count];
            for (int i = 0; i < names.Count; i++)
                content[i] = new GUIContent(names[i]);
            return content;
        }
    }
}
