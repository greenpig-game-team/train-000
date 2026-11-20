using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GreenPig.Core
{
    /// <summary>
    /// 씬 전환 + 검은 화면 페이드. 게임 시작 시 자동 생성되고 씬이 바뀌어도 유지됨.
    /// 사용: SceneFader.LoadScene("Cabin");
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }
        public static bool IsTransitioning { get; private set; }

        const float DefaultFade = 0.6f;
        Image overlay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[SceneFader]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SceneFader>();
            Instance.BuildOverlay();
        }

        void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // 모든 UI 위
            gameObject.AddComponent<CanvasScaler>();

            var imgGo = new GameObject("Overlay", typeof(RectTransform));
            imgGo.transform.SetParent(transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            overlay = imgGo.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0f);
            overlay.raycastTarget = false;
        }

        public static void LoadScene(string sceneName, float fadeDuration = DefaultFade)
        {
            if (Instance == null) Bootstrap();
            if (IsTransitioning) return;
            Instance.StartCoroutine(Instance.LoadRoutine(sceneName, fadeDuration));
        }

        IEnumerator LoadRoutine(string sceneName, float duration)
        {
            IsTransitioning = true;
            overlay.raycastTarget = true; // 전환 중 클릭 막기

            yield return Fade(0f, 1f, duration);

            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            yield return null; // 새 씬 Start() 한 프레임 대기

            yield return Fade(1f, 0f, duration);

            overlay.raycastTarget = false;
            IsTransitioning = false;
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            SetAlpha(to);
        }

        void SetAlpha(float a)
        {
            var c = overlay.color;
            c.a = a;
            overlay.color = c;
        }
    }
}
