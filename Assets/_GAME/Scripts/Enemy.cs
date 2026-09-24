using UnityEngine;
using UnityEngine.InputSystem;
using VRFPSKit;
public class Enemy : MonoBehaviour
{
    [SerializeField] float speed = 0.5f;
    [SerializeField] float attackDistance = 0.6f;
    [SerializeField] float attackCooldown = 1.5f;
    [SerializeField] int damage = 1;
    Transform player;
    float nextAttackTime;
    bool isDead;
    Damageable damageable;
    PlayerHealth playerHealth;
    private void Awake()
    {
        damageable = GetComponent<Damageable>();
    }
    private void OnEnable()
    {
        damageable.DeathEvent += Die;
    }
    void OnDisable()
    {
        damageable.DeathEvent -= Die;
    }
    private void Start()
    {
        player = Camera.main.transform;
        playerHealth = Camera.main.GetComponent<PlayerHealth>();
        Debug.Log("Камера: " + Camera.main.name + ", PlayerHealth найден: " + (playerHealth != null));
    }
    private void Update()
    {
        if (isDead) return;

        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            damageable.TakeDamage(25);
            Debug.Log("Отладка: урон 25, осталось " + damageable.health);
        }
        if (player == null) return;

        Vector3 target = player.position;
        target.y -= 0.3f;
        transform.LookAt(target);
        float distance = Vector3.Distance(transform.position, target);
        if (distance > attackDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }
        else if(Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            playerHealth.TakeDamage(damage);
            Debug.Log("Монстр укусил игрока на " + damage);
        }
    }
    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Монстр умер");
        Destroy(gameObject);
    }
}
