using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThroneOfTides.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        public void OnPlayPressed()
        {
            // TODO - replace with level select scene when built
            SceneManager.LoadScene("CanvasBasedGameScene");
        }

        public void OnPortPressed()
        {
            // TODO - replace with port scene when built
            Debug.Log("Port - not yet implemented");
        }

        public void OnOptionsPressed()
        {
            // TODO - replace with options panel when built
            Debug.Log("Options - not yet implemented");
        }

        public void OnQuitPressed()
        {
            Application.Quit();
            // Editor only - stops play mode
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}