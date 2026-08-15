// Assets/_Game/2. Scripts/UI/SceneFader.cs
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // A single persistent full-screen black overlay that fades in on every scene load and can
    // fade out to black before loading the next one. Self-bootstraps before the very first scene
    // loads via RuntimeInitializeOnLoadMethod, so it needs no prefab or per-scene setup - every
    // scene gets the same simple fade in/out for free just by routing its scene transitions
    // through SceneFader.LoadScene instead of calling SceneManager.LoadScene directly.
    public class SceneFader : MonoBehaviour
    {
        private const float FadeDuration = 0.4f;
        private const int   FadeSortingOrder = 32760; // above every other Canvas in the project

        private static SceneFader _instance;

        private CanvasGroup _canvasGroup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;

            var go = new GameObject("SceneFader");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SceneFader>();
            _instance.BuildUI();
        }

        private void BuildUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = FadeSortingOrder;

            var imageGO = new GameObject("FadeImage", typeof(RectTransform));
            imageGO.transform.SetParent(transform, false);

            var rect = (RectTransform)imageGO.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = imageGO.AddComponent<Image>();
            image.color = Color.black;

            _canvasGroup = imageGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f; // start opaque so the first scene fades in from black too

            SceneManager.sceneLoaded += (scene, mode) => StartCoroutine(FadeInNextFrame());
            StartCoroutine(FadeInNextFrame());
        }

        // sceneLoaded fires the instant the new scene's objects exist, before the engine has
        // actually rendered a frame of it - starting the fade right then let it finish revealing
        // a scene that hadn't visibly appeared yet, which read as "no fade-in at all". Waiting a
        // frame first (and letting the current frame actually finish rendering) makes sure
        // there's something real on screen for the fade to reveal.
        private IEnumerator FadeInNextFrame()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            FadeIn();
        }

        private void FadeIn()
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(0f, FadeDuration)
                .OnComplete(() => _canvasGroup.blocksRaycasts = false);
        }

        /// <summary>Fades to black, then loads the given scene. Use instead of calling
        /// SceneManager.LoadScene directly so every scene transition gets the same fade.</summary>
        public static void LoadScene(string sceneName)
        {
            if (_instance == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            _instance._canvasGroup.blocksRaycasts = true;
            _instance._canvasGroup.DOKill();
            _instance._canvasGroup.DOFade(1f, FadeDuration)
                .OnComplete(() => SceneManager.LoadScene(sceneName));
        }
    }
}
