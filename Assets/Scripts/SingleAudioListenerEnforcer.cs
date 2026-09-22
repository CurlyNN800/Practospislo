using UnityEngine;

public static class SingleAudioListenerEnforcer
{
    // Этот метод вызовется автоматически при запуске сцены
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void RemoveExtraAudioListeners()
    {
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>();
        
        if (listeners.Length > 1)
        {
            AudioListener mainListener = null;

            // Предпочитаем оставить тот, что висит на MainCamera
            foreach (var listener in listeners)
            {
                if (listener.CompareTag("MainCamera"))
                {
                    mainListener = listener;
                    break;
                }
            }

            // Если не нашли на MainCamera, оставляем просто первый попавшийся
            if (mainListener == null)
            {
                mainListener = listeners[0];
            }

            // Удаляем все остальные
            foreach (var listener in listeners)
            {
                if (listener != mainListener)
                {
                    Debug.Log($"[SingleAudioListenerEnforcer] Удален лишний AudioListener с объекта: {listener.gameObject.name}");
                    Object.Destroy(listener);
                }
            }
        }
    }
}
