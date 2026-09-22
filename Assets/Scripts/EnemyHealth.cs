using UnityEngine;
using UnityEngine.AI;
using VRFPSKit;

[RequireComponent(typeof(Damageable))]
[RequireComponent(typeof(EnemyAI))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Death")]
    public string deathAnimName = "Death";
    [Tooltip("0 = исчезает сразу после смерти.")]
    public float destroyDelay = 0f;

    private Damageable _damageable;
    private EnemyAI _enemyAI;
    private NavMeshAgent _agent;
    private Animator _animator;
    private Renderer[] _renderers;
    private bool _isDead;

    void Awake()
    {
        _damageable = GetComponent<Damageable>();
        _enemyAI = GetComponent<EnemyAI>();
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _renderers = GetComponentsInChildren<Renderer>();
    }

    void OnEnable()
    {
        _damageable.DeathEvent += OnDeath;
    }

    void OnDisable()
    {
        _damageable.DeathEvent -= OnDeath;
    }

    private void OnDeath()
    {
        if (_isDead) return;
        _isDead = true;

        _enemyAI.enabled = false;

        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        GameManager.Instance?.OnEnemyKilled(gameObject);

        if (destroyDelay <= 0f)
        {
            HideRenderers();
            Destroy(gameObject);
            return;
        }

        if (_animator != null && !string.IsNullOrEmpty(deathAnimName))
            _animator.CrossFadeInFixedTime(deathAnimName, 0.2f);

        Invoke(nameof(HideRenderers), Mathf.Max(0f, destroyDelay - 1f));
        Destroy(gameObject, destroyDelay);
    }

    private void HideRenderers()
    {
        foreach (var r in _renderers)
            if (r != null) r.enabled = false;
    }
}
