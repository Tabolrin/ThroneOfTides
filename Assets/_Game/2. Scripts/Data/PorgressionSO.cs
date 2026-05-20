using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/Progression")]
    public class ProgressionSO : ScriptableObject
    {
        [SerializeField] private bool _level1Beaten;
        [SerializeField] private bool _level2Beaten;
        [SerializeField] private bool _level3Beaten;

        public bool Level1Unlocked => true;
        public bool Level2Unlocked => _level1Beaten;
        public bool Level3Unlocked => _level2Beaten;

        public bool Level1Beaten => _level1Beaten;
        public bool Level2Beaten => _level2Beaten;
        public bool Level3Beaten => _level3Beaten;

        public void SetLevelBeaten(int levelIndex)
        {
            if      (levelIndex == 1) _level1Beaten = true;
            else if (levelIndex == 2) _level2Beaten = true;
            else if (levelIndex == 3) _level3Beaten = true;
        }

        public void Reset()
        {
            _level1Beaten = false;
            _level2Beaten = false;
            _level3Beaten = false;
        }
    }
}