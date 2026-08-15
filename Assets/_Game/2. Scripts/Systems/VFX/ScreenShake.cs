// Assets/_Game/2. Scripts/Systems/VFX/ScreenShake.cs
using MoreMountains.Feedbacks;

namespace ThroneOfTides.Systems.VFX
{
    // A card's "how strong should this hit feel" decision, expressed the way designers actually
    // think about it (1 weakest - 5 strongest) instead of every controller inventing its own
    // duration/amplitude/frequency numbers. Level 5 is the strongest shake in the game.
    public enum ScreenShakeLevel
    {
        Level1 = 1,
        Level2 = 2,
        Level3 = 3,
        Level4 = 4,
        Level5 = 5
    }

    public static class ScreenShake
    {
        // Amplitude is in world units, applied directly as the camera's position-wiggle range
        // (see MMCameraShaker.ShakeCamera) - with the match camera at orthographicSize 5.4 (10.8
        // units tall), these were previously tuned far too low to read as a hit (Level 1 at 0.3
        // is under 3% of the screen's half-height) and were inconsistently used besides - most
        // weapon VFX controllers had their own unrelated, ad-hoc duration/amplitude/frequency
        // fields instead of going through this shared table at all. Bumped across the board and
        // consolidated so every card's shake now comes from here.
        private static readonly (float duration, float amplitude, float frequency)[] Presets =
        {
            (0.15f, 0.45f, 25f), // Level 1
            (0.20f, 0.85f, 28f), // Level 2
            (0.25f, 1.35f, 30f), // Level 3
            (0.30f, 1.9f,  35f), // Level 4 - very strong
            (0.40f, 2.7f,  40f), // Level 5 - strongest in the game
        };

        public static void Trigger(ScreenShakeLevel level)
        {
            var (duration, amplitude, frequency) = Presets[(int)level - 1];
            MMCameraShakeEvent.Trigger(duration, amplitude, frequency, 0f, 0f, 0f);
        }
    }
}
