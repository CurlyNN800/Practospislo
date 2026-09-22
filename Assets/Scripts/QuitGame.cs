using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void Exit()
    {
        Debug.Log("Выход из игры...");
        GameApplicationQuit.Quit();
    }
}
