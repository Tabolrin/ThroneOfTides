// Assets/_Game/2. Scripts/Systems/VFX/CardSfxPlayer.cs
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Plays one specific song from a shared MMSMPlaylist through MMSoundManager as a one-shot.
    /// Treats the playlist purely as a pool of clips to select from - it never drives
    /// MMSMPlaylistManager's persistent playback session, since that's built for continuous
    /// music/ambience rather than one-off card SFX.
    /// </summary>
    public static class CardSfxPlayer
    {
        public static void Play(CardSfxCue cue, Vector3 worldPosition)
        {
            if (cue?.Playlist == null) return;

            var songs = cue.Playlist.Songs;
            if (songs == null || songs.Count == 0) return;

            MMSMPlaylistSong song = ResolveSong(songs, cue.SongName);
            if (song?.Clip == null) return;

            var options = MMSoundManagerPlayOptions.Default;
            options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
            options.Location = worldPosition;
            options.Loop = false;
            options.Volume *= cue.VolumeMultiplier;
            options.Pitch *= cue.PitchMultiplier;

            MMSoundManagerSoundPlayEvent.Trigger(song.Clip, options);
        }

        private static MMSMPlaylistSong ResolveSong(List<MMSMPlaylistSong> songs, string songName)
        {
            if (string.IsNullOrEmpty(songName))
                return songs[Random.Range(0, songs.Count)];

            foreach (var song in songs)
            {
                if (song.Name == songName) return song;
            }

            Debug.LogWarning($"CardSfxPlayer: no song named '{songName}' found in the assigned playlist - falling back to a random song.");
            return songs[Random.Range(0, songs.Count)];
        }
    }
}
