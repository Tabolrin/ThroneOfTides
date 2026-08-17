using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using ThroneOfTides.App;
using ThroneOfTides.Data;

namespace ThroneOfTides.Tools
{
    public class ThroneOfTidesBuildPipeline : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        // Matches the order already set up in Unity's own Build Settings (File > Build Settings) -
        // kept as an explicit list (rather than reading EditorBuildSettings.scenes directly) so a
        // scene can be temporarily disabled there for quick iteration without silently dropping
        // out of these custom build menu items too.
        private static readonly string[] Scenes =
        {
            "Assets/_Game/7. Scenes/MainMenu.unity",
            "Assets/_Game/7. Scenes/LevelSelect.unity",
            "Assets/_Game/7. Scenes/Port.unity",
            "Assets/_Game/7. Scenes/Match.unity"
        };

        private const int PlaytestStartingHP  = 40;
        private const int PlaytestMaxHandSize = 6;

        private const string MatchScenePath       = "Assets/_Game/7. Scenes/Match.unity";
        private const string BuildProfilesAssetPath = "Assets/_Game/4. Data/BuildDeckProfiles.asset";

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
                "Build Blocked - Data Errors Found",
                $"{errors.Count} error(s) found:\n\n{errorSb}\nCancel to fix, or build anyway.",
                "Cancel Build",
                "Build Anyway");

            // BuildFailedException is the correct way to abort a build from a preprocessor
            if (cancel)
                throw new BuildFailedException(
                    $"[ThroneOfTides] Build cancelled - {errors.Count} unresolved error(s).");
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
        // Deliberately does NOT call ApplySceneOverrides - unlike every other build type here,
        // this one builds Match.unity exactly as it's currently authored in the Editor (whatever
        // deck overrides, if any, are already sitting in GameBootstrapper's Inspector fields, and
        // whatever active/inactive state the CheatsPanel/CardCheatPanel/ForceEnemyCardCheatPanel
        // GameObjects are already in). Cheats still work regardless of their GameObject's own
        // active state, since each panel's own Awake() only hides itself when
        // !Debug.isDebugBuild - BuildOptions.Development below keeps that true, so they show up
        // as long as nothing has explicitly deactivated them in the scene.

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
                // Profiler + script debugger enabled - not for distribution
                options          = BuildOptions.Development
                                 | BuildOptions.ConnectWithProfiler
                                 | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.DisplayDialog("Dev Build Complete",
                    $"Development build ready:\n{path}\n\n" +
                    "Built exactly as currently configured in the Editor - no deck/config overrides applied.\n" +
                    "Profiler, debugger, and cheats enabled.",
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

            var backup = ApplySceneOverrides(BuildProfileType.Release, disableCheats: true);
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                backup.Restore();
            }

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
            var backup = ApplySceneOverrides(BuildProfileType.Playtest, disableCheats: false);

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
                // or threw an exception - the asset will never be left dirty on disk.
                ApplyConfigOverride(config, originalHP, originalHand);
                backup.Restore();
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

            var backup = ApplySceneOverrides(BuildProfileType.WebGL, disableCheats: true);
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                backup.Restore();
            }

            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.DisplayDialog("WebGL Build Complete",
                    $"WebGL build ready:\n{folder}\n\nUpload the entire folder to itch.io or a static file server.",
                    "OK");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        // SerializedObject is used here (instead of direct field assignment) so Unity
        // tracks the modification for dirty-marking and Undo - critical for SO editing
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

        // ── Per-Build-Type Scene Overrides ──────────────────────────────────────
        // Unity's BuildPipeline reads scenes from disk, not the in-memory Editor state, so any
        // build-specific customization (which deck each side uses, whether Cheats is reachable)
        // has to be written into Match.unity before the build and reverted after - otherwise the
        // override would leak into the next Editor session or the next, differently-typed build.

        private class SceneOverrideBackup
        {
            public GameBootstrapper Bootstrapper;
            public DeckDefinitionSO OriginalPlayerDeckOverride;
            public DeckDefinitionSO OriginalEnemyDeckOverride;
            public Component        CheatsPanelComponent;
            public bool             OriginalCheatsActive;

            public void Restore()
            {
                if (Bootstrapper == null) return;

                var so = new SerializedObject(Bootstrapper);
                so.FindProperty("_playerDeckOverride").objectReferenceValue = OriginalPlayerDeckOverride;
                so.FindProperty("_enemyDeckOverride").objectReferenceValue  = OriginalEnemyDeckOverride;
                so.ApplyModifiedPropertiesWithoutUndo();

                if (CheatsPanelComponent != null)
                    CheatsPanelComponent.gameObject.SetActive(OriginalCheatsActive);

                EditorSceneManager.MarkAllScenesDirty();
                EditorSceneManager.SaveOpenScenes();
            }
        }

        private static SceneOverrideBackup ApplySceneOverrides(BuildProfileType profileType, bool disableCheats)
        {
            EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);

            var bootstrapper = Object.FindAnyObjectByType<GameBootstrapper>(FindObjectsInactive.Include);
            if (bootstrapper == null)
            {
                Debug.LogWarning("[ThroneOfTides] No GameBootstrapper found in Match.unity - build profile overrides skipped.");
                return new SceneOverrideBackup();
            }

            var so         = new SerializedObject(bootstrapper);
            var playerProp = so.FindProperty("_playerDeckOverride");
            var enemyProp  = so.FindProperty("_enemyDeckOverride");
            var cheatsProp = so.FindProperty("_cheatsPanel");

            var backup = new SceneOverrideBackup
            {
                Bootstrapper                = bootstrapper,
                OriginalPlayerDeckOverride  = playerProp.objectReferenceValue as DeckDefinitionSO,
                OriginalEnemyDeckOverride   = enemyProp.objectReferenceValue as DeckDefinitionSO,
                CheatsPanelComponent        = cheatsProp.objectReferenceValue as Component,
            };
            backup.OriginalCheatsActive = backup.CheatsPanelComponent != null
                && backup.CheatsPanelComponent.gameObject.activeSelf;

            var profiles = AssetDatabase.LoadAssetAtPath<BuildDeckProfileSO>(BuildProfilesAssetPath);
            var profile  = profiles != null ? profiles.GetProfile(profileType) : null;

            if (profile?.PlayerDeck != null) playerProp.objectReferenceValue = profile.PlayerDeck;
            if (profile?.EnemyDeck  != null) enemyProp.objectReferenceValue  = profile.EnemyDeck;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (disableCheats && backup.CheatsPanelComponent != null)
                backup.CheatsPanelComponent.gameObject.SetActive(false);

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();

            return backup;
        }
    }
}