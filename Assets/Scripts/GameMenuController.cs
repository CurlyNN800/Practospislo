using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CursedMansion
{
    public class GameMenuController : MonoBehaviour
    {
        private const string VolumeKey = "MasterVolume";
        private const string NextScene = "Road";

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Settings")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle peacefulToggle;

        public static bool PeacefulMode { get; private set; }

        private AsyncOperation _preload;

        private void Start()
        {
            float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            AudioListener.volume = savedVolume;

            if (volumeSlider != null)
                volumeSlider.value = savedVolume;

            if (peacefulToggle != null)
                peacefulToggle.isOn = PeacefulMode;

            ShowMain();
            StartCoroutine(PreloadNextScene());
        }

        private IEnumerator PreloadNextScene()
        {
            _preload = SceneManager.LoadSceneAsync(NextScene);
            _preload.allowSceneActivation = false;
            yield return _preload;
        }

        public void StartGame()
        {
            Time.timeScale = 1f;
            if (_preload != null)
                _preload.allowSceneActivation = true;
            else
                SceneManager.LoadScene(NextScene);
        }

        public void ShowMain()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void ShowSettings()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void SetVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VolumeKey, value);
            PlayerPrefs.Save();
        }

        public void SetPeacefulMode(bool value)
        {
            PeacefulMode = value;
        }

        public void QuitGame() => GameApplicationQuit.Quit();
    }
}
