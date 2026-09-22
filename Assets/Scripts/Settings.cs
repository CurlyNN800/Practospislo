using UnityEngine;
using UnityEngine.UI; // ����������� ��� Slider � Toggle

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject settingsMenu;

    [Header("��������� ���������")]
    public GameObject monsterSpawner; // ���� ��������� ������, ������� ������� �����

    // 1. ����� ��� �������� ���������
    public void ChangeVolume(float volume)
    {
        AudioListener.volume = volume; // ������ ����� ��������� ���� (0.0 �� 1.0)
        Debug.Log("���������: " + volume);
    }

    // 2. ����� ��� ������ (����� / � ������)
    public void SetPeacefulMode(bool isPeaceful)
    {
        if (isPeaceful)
        {
            Debug.Log("�����: �����");
            if (monsterSpawner != null) monsterSpawner.SetActive(false);
        }
        else
        {
            Debug.Log("�����: � ������");
            if (monsterSpawner != null) monsterSpawner.SetActive(true);
        }
    }

    // ������ ������������ ���� (������� ��� �������)
    public void OpenSettings()
    {
        if (mainMenu != null)
            mainMenu.SetActive(false);
        if (settingsMenu != null)
            settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsMenu != null)
            settingsMenu.SetActive(false);
        if (mainMenu != null)
            mainMenu.SetActive(true);
    }

    /// <summary> ������ UI: ���/���� ������� (���� ��������). </summary>
    public void ToggleMobs()
    {
        if (monsterSpawner != null)
            monsterSpawner.SetActive(!monsterSpawner.activeSelf);
    }
}