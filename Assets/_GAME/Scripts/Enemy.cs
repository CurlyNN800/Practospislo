using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] float speed = 0.5f;
    [SerializeField] float attackDistance = 0.6f;
    [SerializeField] float attackCooldown = 1.5f;
    [SerializeField] int damage = 1;
    Transform player;
    float nextAttackTime;
    private void Start()
    {
        player = Camera.main.transform;
    }
    private void Update()
    {
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
            Debug.Log("Монстр укусил игрока на " + damage);
        }
    }

}
