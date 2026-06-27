using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThroneOfTides.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        public void OnPlayPressed() =>
            SceneManager.LoadScene("LevelSelect");

        public void OnPortPressed() =>
            SceneManager.LoadScene("Port");

        public void OnOptionsPressed()
        {
            // TODO - replace with options panel when built
            Debug.Log("Options - not yet implemented");
        }

        public void OnQuitPressed()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}