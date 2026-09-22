using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedMansion
{
    public class SceneTransitionRunner : MonoBehaviour
    {
        static SceneTransitionRunner s_Instance;
        static bool s_Loading;

        public static void ResetForNewRoadScene() => s_Loading = false;

        public static void LoadHouseFromRoad(float fadeToBlackSeconds, float fadeInSeconds) =>
            TransitionTo(GameScenes.HouseScene, "FromRoad", fadeToBlackSeconds, fadeInSeconds);

        public static void TransitionTo(
            string sceneName,
            string spawnId,
            float fadeOutSeconds,
            float fadeInSeconds)
        {
            if (s_Loading || string.IsNullOrEmpty(sceneName))
                return;

            s_Loading = true;
            Ensure().StartCoroutine(TransitionRoutine(sceneName, spawnId, fadeOutSeconds, fadeInSeconds));
        }

        static SceneTransitionRunner Ensure()
        {
            if (s_Instance != null)
                return s_Instance;

            var go = new GameObject(nameof(SceneTransitionRunner));
            s_Instance = go.AddComponent<SceneTransitionRunner>();
            return s_Instance;
        }

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        static IEnumerator TransitionRoutine(
            string sceneName,
            string spawnId,
            float fadeOutSeconds,
            float fadeInSeconds)
        {
            Debug.Log($"[SceneTransition] → {sceneName} (spawn: {spawnId})");

            // 1) Сначала полное затемнение (машина ещё едет / игрок в кадре).
            if (fadeOutSeconds > 0f)
            {
                var fade = ScreenFade.Ensure();
                yield return RunWithTimeout(fade.FadeToBlack(fadeOutSeconds), fadeOutSeconds + 3f);
            }
            else
            {
                var fade = ScreenFade.Ensure();
                yield return fade.FadeToBlack(0f);
            }

            // 2) На чёрном экране — отвязка рига и загрузка сцены.
            SceneTransfer.PreparePlayerForLoad();
            SceneTransfer.SetPendingSpawn(spawnId);

            if (sceneName == GameScenes.HouseScene && GameProgress.Instance != null)
                GameProgress.Instance.EnterMansionExterior();

            ScreenFade.RequestFadeInAfterLoad(fadeInSeconds);

            Debug.Log($"[SceneTransition] LoadScene (экран чёрный): {sceneName}");
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            s_Loading = false;
        }

        static IEnumerator RunWithTimeout(IEnumerator routine, float timeoutSeconds)
        {
            if (routine == null)
                yield break;

            float elapsed = 0f;
            while (routine.MoveNext())
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= timeoutSeconds)
                {
                    Debug.LogWarning("[SceneTransition] Fade timeout — продолжаем загрузку.");
                    yield break;
                }

                yield return routine.Current;
            }
        }
    }
}
