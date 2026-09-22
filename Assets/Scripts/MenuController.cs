using UnityEngine;
using UnityEngine.SceneManagement;

// ВАЖНО: Мы убрали "namespace VRFPSKit", чтобы Unity точно распознал скрипт
public class DeathSceneLoader : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Имя сцены, куда перекинет при смерти (например, 'MainMenu' или 'Level1')")]
    public string targetSceneName = "MainMenu";

    // Этот метод можно вызвать из ЛЮБОГО скрипта здоровья
    public void OnPlayerDied()
    {
        Debug.Log("Игрок умер! Загружаем сцену: " + targetSceneName);
        SceneManager.LoadScene(targetSceneName);
    }
}