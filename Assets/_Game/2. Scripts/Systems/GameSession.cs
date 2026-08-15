using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    // Which map node the ship icon is currently sitting at - persisted here (rather than on
    // LevelSelectManager itself) because the LevelSelect scene fully reloads every time the
    // player returns to it, so the ship's position would otherwise reset every visit instead of
    // reflecting wherever it last actually traveled to.
    public enum MapNode { Level1, Level2, Level3, Port }

    // Holds transient session data passed between scenes
    // Not persistent - resets on game launch
    public static class GameSession
    {
        public static CaptainSO SelectedCaptain    { get; private set; }
        public static int       SelectedLevelIndex { get; private set; }
        // Ship starts docked at the Port before the player has ever challenged a level.
        public static MapNode   CurrentMapNode      { get; private set; } = MapNode.Port;

        public static void SetLevel(CaptainSO captain, int levelIndex)
        {
            SelectedCaptain    = captain;
            SelectedLevelIndex = levelIndex;
        }

        public static void SetCurrentMapNode(MapNode node) => CurrentMapNode = node;

        public static void Clear()
        {
            SelectedCaptain    = null;
            SelectedLevelIndex = 0;
        }
    }
}