using UnityEngine;

// Прогресс прохождения уровней, хранится в PlayerPrefs.
// Храним одно число — номер максимального открытого уровня (1 = открыт только первый).
public static class LevelProgress
{
    const string UnlockedKey = "UnlockedLevel";

    public static int UnlockedLevel => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedKey, 1));

    // levelNumber считается с 1: Уровень 1, Уровень 2, ...
    public static bool IsUnlocked(int levelNumber) => levelNumber <= UnlockedLevel;

    // Вызывать при победе на уровне completedLevel: открывает следующий уровень
    public static void UnlockNextLevel(int completedLevel)
    {
        int next = completedLevel + 1;
        if (next <= UnlockedLevel)
            return;

        PlayerPrefs.SetInt(UnlockedKey, next);
        PlayerPrefs.Save();
        Debug.Log($"[LevelProgress] Уровень {completedLevel} пройден — открыт уровень {next}.");
    }

    // Сброс прогресса (для отладки)
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(UnlockedKey);
        PlayerPrefs.Save();
        Debug.Log("[LevelProgress] Прогресс сброшен.");
    }
}
