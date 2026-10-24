// Assets/_Game/2. Scripts/Data/GameplaySettingsSO.cs
using MoreMountains.Tools;
using UnityEngine;

namespace ThroneOfTides.Data
{
    /// <summary>
    /// Player-facing gameplay preferences (as opposed to ProgressionSO's campaign-state or
    /// PlayerInventory's economy) - currently just how the enemy's played-card reveal is
    /// dismissed. Edited via OptionsPanel, read by HandLayoutManager.
    /// </summary>
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/GameplaySettings")]
    public class GameplaySettingsSO : ScriptableObject
    {
        [Tooltip("If true, the enemy's played-card reveal stays up until the player clicks to dismiss it. If false, it auto-dismisses after EnemyCardAutoDismissDuration.")]
        [SerializeField] private bool _requireClickToDismissEnemyCard = false;

        [Tooltip("Seconds the enemy's played-card reveal stays up before auto-dismissing. Ignored if RequireClickToDismissEnemyCard is true.")]
        [SerializeField] private float _enemyCardAutoDismissDuration = 1.5f;

        // Same fix as ProgressionSO.OnEnable - the sea map and Port scenes don't reference this
        // asset, so a built game unloads it there and reloads the build defaults on the next
        // battle, silently undoing the player's saved setting. Re-reading the save on every load
        // keeps it correct.
        private void OnEnable()
        {
            if (Application.isPlaying) LoadFromDisk();
        }

        public bool  RequireClickToDismissEnemyCard => _requireClickToDismissEnemyCard;
        public float EnemyCardAutoDismissDuration   => _enemyCardAutoDismissDuration;

        public void SetRequireClickToDismissEnemyCard(bool value)
        {
            _requireClickToDismissEnemyCard = value;
            Save();
        }

        public void SetEnemyCardAutoDismissDuration(float value)
        {
            _enemyCardAutoDismissDuration = Mathf.Max(0.1f, value);
            Save();
        }

        // ── Save/Load ─────────────────────────────────────────────────────────

        private const string SaveFileName   = "gameplay.save";
        private const string SaveFolderName = "ThroneOfTides/";

        public void Save()
        {
            var data = new GameplaySettingsSaveData
            {
                RequireClickToDismissEnemyCard = _requireClickToDismissEnemyCard,
                EnemyCardAutoDismissDuration   = _enemyCardAutoDismissDuration,
            };
            MMSaveLoadManager.Save(data, SaveFileName, SaveFolderName);
        }

        // Call once at game startup, before anything reads this.
        public void LoadFromDisk()
        {
            var data = (GameplaySettingsSaveData)MMSaveLoadManager.Load(typeof(GameplaySettingsSaveData), SaveFileName, SaveFolderName);
            if (data == null) return;

            _requireClickToDismissEnemyCard = data.RequireClickToDismissEnemyCard;
            _enemyCardAutoDismissDuration   = data.EnemyCardAutoDismissDuration;
        }
    }
}
