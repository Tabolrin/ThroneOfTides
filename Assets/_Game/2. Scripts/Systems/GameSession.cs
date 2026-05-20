using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    // Holds transient session data passed between scenes
    // Not persistent - resets on game launch
    public static class GameSession
    {
        public static CaptainSO SelectedCaptain    { get; private set; }
        public static int       SelectedLevelIndex { get; private set; }

        public static void SetLevel(CaptainSO captain, int levelIndex)
        {
            SelectedCaptain    = captain;
            SelectedLevelIndex = levelIndex;
        }

        public static void Clear()
        {
            SelectedCaptain    = null;
            SelectedLevelIndex = 0;
        }
    }
}