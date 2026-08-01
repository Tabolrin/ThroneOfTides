// Assets/_Game/2. Scripts/Core/GameDebug.cs
using System.Diagnostics;

namespace ThroneOfTides.Core
{
    // Flavor/narration logging for gameplay resolution paths (combat, card effects, turn flow).
    // Compiled out entirely outside the editor and development builds, so shipped release
    // builds pay no string-interpolation or call cost for these.
    public static class GameDebug
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message) => UnityEngine.Debug.Log(message);
    }
}
