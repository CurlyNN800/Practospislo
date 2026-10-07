using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int health = 5;
    int maxHealth;
    bool isDead = false;

    // Получен урон (сколько)
    public event Action<int> Damaged;
    // Изменилось здоровье: (текущее, максимум) — при уроне и при сбросе
    public event Action<int, int> HealthChanged;
    // Здоровье дошло до 0
    public event Action Died;

    public int Health => health;
    public int MaxHealth => maxHealth;

    void Awake()
    {
        // Стартовое значение из инспектора считаем максимумом
        maxHealth = health;

        // Обратная связь кита (часы на руке, красная виньетка, звук ранения)
        if (GetComponent<PlayerHealthFeedback>() == null)
            gameObject.AddComponent<PlayerHealthFeedback>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead == true) return;
        // Урон проходит только во время игры (не в меню, паузе, на экранах победы/поражения)
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        health = Mathf.Max(0, health - amount);
        Debug.Log("Осталось здоровья " + health);
        Damaged?.Invoke(amount);
        HealthChanged?.Invoke(health, maxHealth);

        if (health <= 0)
        {
            Debug.Log("Игрок погиб");
            isDead = true;
            Died?.Invoke();
        }
    }

    // Сброс здоровья до максимума (перезапуск уровня / выход в меню)
    public void ResetHealth()
    {
        health = maxHealth;
        isDead = false;
        HealthChanged?.Invoke(health, maxHealth);
    }
}
