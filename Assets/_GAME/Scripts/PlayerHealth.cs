using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int health = 5;
    int maxHealth;
    bool isDead = false;

    // Здоровье дошло до 0
    public event Action Died;

    public int Health => health;
    public int MaxHealth => maxHealth;

    void Awake()
    {
        // Стартовое значение из инспектора считаем максимумом
        maxHealth = health;
    }

    public void TakeDamage(int amount)
    {
        if (isDead == true) return;
        // Урон проходит только во время игры (не в меню, паузе, на экранах победы/поражения)
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        health -= amount;
        Debug.Log("Осталось здоровья " + health);
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
    }
}
