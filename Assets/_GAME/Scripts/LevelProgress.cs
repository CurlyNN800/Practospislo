using UnityEngine;

// Прогресс прохождения уровней в пределах одной сессии игры.
// Храним одно число — номер максимального открытого уровня (1 = открыт только первый).
// Между запусками игры не сохраняется: при каждом запуске открыт только уровень 1.
public static class LevelProgress
{
    // Старый ключ PlayerPrefs, в котором прогресс хранился раньше
    const string LegacyUnlockedKey = "UnlockedLevel";

    static int unlockedLevel = 1;

    public static int UnlockedLevel => unlockedLevel;

    // Сброс при каждом старте игры. SubsystemRegistration вызывается до загрузки первой сцены
    // и при каждом нажатии Play в редакторе, даже если Domain Reload выключен (тогда static-поля
    // сами не сбрасываются и прогресс перешёл бы в следующий запуск).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnStartup()
    {
        unlockedLevel = 1;
        DeleteLegacySave();
    }

    // levelNumber считается с 1: Уровень 1, Уровень 2, ...
    public static bool IsUnlocked(int levelNumber) => levelNumber <= unlockedLevel;

    // Вызывать при победе на уровне completedLevel: открывает следующий уровень
    public static void UnlockNextLevel(int completedLevel)
    {
        int next = completedLevel + 1;
        if (next <= unlockedLevel)
            return;

        unlockedLevel = next;
        Debug.Log($"[LevelProgress] Уровень {completedLevel} пройден — открыт уровень {next}.");
    }

    // Сброс прогресса (для отладки: CursedMansion → Debug → Reset Level Progress)
    public static void ResetProgress()
    {
        unlockedLevel = 1;
        DeleteLegacySave();
        Debug.Log("[LevelProgress] Прогресс сброшен.");
    }

    // Старое сохранение из PlayerPrefs больше не читается; удаляем его, чтобы не висело в реестре.
    // Ключ удаляется при первом запуске новой версии, дальше HasKey просто возвращает false.
    static void DeleteLegacySave()
    {
        if (!PlayerPrefs.HasKey(LegacyUnlockedKey))
            return;

        PlayerPrefs.DeleteKey(LegacyUnlockedKey);
        PlayerPrefs.Save();
        Debug.Log("[LevelProgress] Удалено старое сохранение прогресса из PlayerPrefs.");
    }
}
