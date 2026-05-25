using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using ThroneOfTides.Data;

namespace ThroneOfTides.Tools
{
    public class ThroneOfTidesBuildPipeline : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static readonly string[] Scenes =
        {
            "Assets/_Game/7. Scenes/MainMenu.unity",
            "Assets/_Game/7. Scenes/LevelSelect.unity",
            "Assets/_Game/7. Scenes/Match.unity"
        };

        private const int PlaytestStartingHP  = 40;
        private const int PlaytestMaxHandSize = 6;

        // ── IPreprocessBuildWithReport ──────────────────────────────────────────

        // Runs automatically before every build triggered by any path
        public void OnPreprocessBuild(BuildReport report)
        {
            var (errors, warnings) = CardAssetSearch.ValidateAllData();

            if (warnings.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var w in warnings) sb.AppendLine("  ⚠ " + w);
                Debug.LogWarning($"[ThroneOfTides] {warnings.Count} pre-build warning(s):\n{sb}");
            }

            if (errors.Count == 0) return;

            var errorSb = new StringBuilder();
            foreach (var e in errors) errorSb.AppendLine("• " + e);

            bool cancel = EditorUtility.DisplayDialog(
                "Build Blocked — Data Errors Found",
                $"{errors.Count} error(s) found:\n\n{errorSb}\nCancel to fix, or build anyway.",
                "Cancel Build",
                "Build Anyway");

            // BuildFailedException is the correct way to abort a build from a preprocessor
            if (cancel)
                throw new BuildFailedException(
                    $"[ThroneOfTides] Build cancelled — {errors.Count} unresolved error(s).");
        }

        // ── IPostprocessBuild ───────────────────────────────────────────────────

        public void OnPostprocessBuild(BuildReport report)
        {
            var cards = CardAssetSearch.LoadAll<CardSO>();
            var decks = CardAssetSearch.LoadAll<DeckDefinitionSO>();

            int totalCards = 0;
            foreach (var deck in decks) totalCards += deck.BuildDeck().Count;

            Debug.Log(
                $"[ThroneOfTides] Build complete → {report.summary.platform}\n" +
                $"  Output:           {report.summary.outputPath}\n" +
                $"  Duration:         {report.summary.totalTime.TotalSeconds:F1}s\n" +
                $"  Unique cards:     {cards.Count}\n" +
                $"  Deck definitions: {decks.Count}\n" +
                $"  Total deck cards: {totalCards}");
        }

        // ── Build A: Development (Windows) ─────────────────────────────────────

        [MenuItem("ThroneOfTides/Build/Development Build (Windows)  %#1")]
        public static void BuildDevelopment()
        {
            if (!RunValidationDialog()) return;

            string path = PickOutputPath("ThroneOfTides_Dev", "exe");
            if (string.IsNullOrEmpty(path)) return;

            var options = new BuildPlayerOptions
            {
                scenes           = Scenes,
                locationPathName = path,
                target           = BuildTarget.StandaloneWindows64,
                // Profiler + script debugger enabled — not for distribution
                options          = BuildOptions.Development
                                 | BuildOptions.ConnectWithProfiler
                                 | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.DisplayDialog("Dev Build Complete",
                    $"Development build ready:\n{path}\n\nProfiler and debugger enabled.",
                    "OK");
        }

        // ── Build B: Release (Windows) ──────────────────────────────────────────

        [MenuItem("ThroneOfTides/Build/Release Build (Windows)  %#2")]
        public static void BuildRelease()
        {
            if (!RunValidationDialog()) return;

            bool proceed = EditorUtility.DisplayDialog(
                "Release Build",
                "Builds without profiler or debugger.\nConfirm all art and audio are final.",
                "Build Release",
                "Cancel");

            if (!proceed) return;

            string path = PickOutputPath("ThroneOfTides", "exe");
            if (string.IsNullOrEmpty(path)) return;

            var options = new BuildPlayerOptions
            {
                scenes           = Scenes,
                locationPathName = path,
                target           = BuildTarget.StandaloneWindows64,
                options          = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.DisplayDialog("Release Build Complete",
                    $"Release build ready:\n{path}", "OK");
        }

        // ── Build C: Playtest Config (Windows) ──────────────────────────────────
        // Temporarily overrides GameConfigSO values before building, then restores them.
        // The SO is modified via SerializedObject so the change is tracked by Unity
        // and restored cleanly without leaving the asset dirty after the build.
        // try/finally guarantees the restore executes even if BuildPlayer throws,
        // preventing the asset from being left in the overridden state on disk.

        [MenuItem("ThroneOfTides/Build/Playtest Config Build (Windows)  %#3")]
        public static void BuildPlaytest()
        {
            if (!RunValidationDialog()) return;

            var config = LoadGameConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("Config Not Found",
                    "No GameConfigSO found in project. Create one at Assets/_Game/Data/.",
                    "OK");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog(
                "Playtest Config Build",
                $"Current config:\n" +
                $"  Starting HP:   {config.StartingHP}\n" +
                $"  Max Hand Size: {config.MaxHandSize}\n\n" +
                $"Playtest overrides (restored after build):\n" +
                $"  Starting HP:   {PlaytestStartingHP}\n" +
                $"  Max Hand Size: {PlaytestMaxHandSize}",
                "Build With Overrides",
                "Cancel");

            if (!proceed) return;

            string path = PickOutputPath("ThroneOfTides_Playtest", "exe");
            if (string.IsNullOrEmpty(path)) return;

            int originalHP   = config.StartingHP;
            int originalHand = config.MaxHandSize;

            ApplyConfigOverride(config, PlaytestStartingHP, PlaytestMaxHandSize);

            BuildReport report;
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes           = Scenes,
                    locationPathName = path,
                    target           = BuildTarget.StandaloneWindows64,
                    options          = BuildOptions.Development | BuildOptions.ConnectWithProfiler
                };

                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                // Restore executes regardless of whether the build succeeded, failed,
                // or threw an exception — the asset will never be left dirty on disk.
                ApplyConfigOverride(config, originalHP, originalHand);
            }

            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.DisplayDialog("Playtest Build Complete",
                    $"Playtest build ready:\n{path}\n\n" +
                    $"Built with HP={PlaytestStartingHP}, HandSize={PlaytestMaxHandSize}.\n" +
                    "GameConfigSO restored to original values.",
                    "OK");
        }

        // ── Build D: WebGL ──────────────────────────────────────────────────────

        [MenuItem("ThroneOfTides/Build/WebGL Build (Browser)  %#4")]
        public static void BuildWebGL()
        {
            if (!RunValidationDialog()) return;

            bool proceed = EditorUtility.DisplayDialog(
                "WebGL Build",
                "Builds for browser play.\nRequires WebGL module installed in Unity Hub.\nBuild time is significantly longer.",
                "Build WebGL",
                "Cancel");

            if (!proceed) return;

            // WebGL outputs a folder, not a single executable
            string folder = EditorUtility.SaveFolderPanel(
                "Choose WebGL Output Folder", "", "ThroneOfTides_WebGL");
            if (string.IsNullOrEmpty(folder)) return;

            var options = new BuildPlayerOptions
            {
                scenes           = Scenes,
                locationPathName = folder,
                target           = BuildTarget.WebGL,
                options          = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.DisplayDialog("WebGL Build Complete",
                    $"WebGL build ready:\n{folder}\n\nUpload the entire folder to itch.io or a static file server.",
                    "OK");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        // SerializedObject is used here (instead of direct field assignment) so Unity
        // tracks the modification for dirty-marking and Undo — critical for SO editing
        // at editor-time without leaving assets in an unintended modified state.
        // GameConfigSO uses public fields (no _ prefix), so FindProperty takes the
        // exact public field name as declared on the class.
        private static void ApplyConfigOverride(GameConfigSO config, int hp, int handSize)
        {
            var so = new SerializedObject(config);
            so.FindProperty("StartingHP").intValue  = hp;
            so.FindProperty("MaxHandSize").intValue = handSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static GameConfigSO LoadGameConfig()
        {
            var guids = AssetDatabase.FindAssets("t:GameConfigSO");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<GameConfigSO>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static bool RunValidationDialog()
        {
            var (errors, _) = CardAssetSearch.ValidateAllData();

            if (errors.Count == 0) return true;

            var sb = new StringBuilder();
            foreach (var e in errors) sb.AppendLine("• " + e);

            bool cancel = EditorUtility.DisplayDialog(
                "Data Errors Found",
                $"{errors.Count} error(s):\n\n{sb}\nCancel to fix, or build anyway?",
                "Cancel",
                "Build Anyway");

            return !cancel;
        }

        private static string PickOutputPath(string defaultName, string extension)
            => EditorUtility.SaveFilePanel("Choose Build Location", "", defaultName, extension);
    }
}