using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int health = 5;
    bool isDead = false;
    public void TakeDamage(int amount)
    {
        if (isDead == true) return;
        health -= amount;
        Debug.Log("Осталось здоровья " + health);
        if (health <= 0)
        {
            Debug.Log("Игрок погиб");
            isDead = true;
        }
    }
   
}
