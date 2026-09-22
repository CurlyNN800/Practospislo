using UnityEngine;
using UnityEngine.SceneManagement;

public class VRSceneChanger : MonoBehaviour
{
    [Header("Настройки перехода")]
    public string HouseScene;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Триггер задет объектом: " + other.name);

        if (other.gameObject.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            LoadLevel();
        }
    }

    void LoadLevel()
    {
        if (!string.IsNullOrEmpty(HouseScene))
        {
            Debug.Log("Загрузка сцены: " + HouseScene);
            SceneManager.LoadScene(HouseScene);
        }
        else
        {
            Debug.LogError("Имя сцены не указано в инспекторе!");
        }
    }
}