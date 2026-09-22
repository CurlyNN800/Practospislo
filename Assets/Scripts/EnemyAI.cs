using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // Машина состояний
    public enum EnemyState { Idle, Chase, Attack }
    public EnemyState currentState = EnemyState.Idle;

    [Header("Targeting")]
    public Transform player; // Сюда перетащи Main Camera из твоего XR Origin
    public float detectionRadius = 15f; // Радиус обнаружения игрока
    public float attackRadius = 1.5f;   // Дистанция для атаки (ближний бой)

    [Header("Vertical combat (floors)")]
    [Tooltip("Макс. |ΔY| для обнаружения (тело игрока, не камера). Между этажами обычно >3 м.")]
    public float detectionHeightRange = 1.35f;
    [Tooltip("Макс. |ΔY| для атаки и урона — не бьёт сквозь пол/потолок.")]
    public float attackHeightRange = 1.1f;
    [Tooltip("Высота «центра» моба от pivot (ног) для сравнения с игроком.")]
    [SerializeField] float enemyBodyHeightOffset = 0.9f;

    [Header("Debug")]
    public bool debugLogs = true;

    [Header("Animations")]
    public Animator animator; // Компонент Animator на мобе
    public string idleAnimName = "Old Man Idle";     // Название стейта Idle в Animator
    public string chaseAnimName = "Walking";   // Название стейта Chase в Animator
    public string attackAnimName = "Zombie Attack"; // Название стейта Attack в Animator

    [Header("Attack Settings")]
    public float timeBetweenAttacks = 2f;
    public float damagePerHit = 10f;
    private float attackTimer;

    [Header("Visual size")]
    [Tooltip("Масштаб модели — заметно выше игрока (~2 м у VR Player).")]
    [SerializeField] float visualScale = 1.2f;

    [Header("Navigation")]
    [Tooltip("Радиус NavMeshAgent — узкий проход в дверях; не влияет на размер модели и хитбоксов.")]
    [SerializeField] float navAgentRadius = 0.18f;
    [SerializeField] float navAgentHeight = 1.65f;
    [Tooltip("Поиск NavMesh вокруг позиции моба при старте / активации.")]
    [SerializeField] float navMeshSampleRadius = 3f;
    [Tooltip("Не привязывать к NavMesh другого этажа (|ΔY| больше — агент остаётся на месте).")]
    [SerializeField] float maxNavMeshVerticalSnap = 1.25f;

    [Header("Hit detection")]
    [Tooltip("Коллайдеры по всему телу для физических пуль (VRFPSKit Bullet → OnCollisionEnter).")]
    [SerializeField] bool useFullBodyHitVolumes = true;

    const string HitVolumeChildName = "CM_HitVolume";

    private NavMeshAgent agent;
    private Component playerDamagable;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        if (agent != null)
        {
            agent.radius = navAgentRadius;
            agent.height = navAgentHeight;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.stoppingDistance = attackRadius * 0.8f;
        }

        ApplyVisualScale();
        ConfigureBodyColliders();

        // Отключаем Root Motion через код, чтобы анимация не уносила саму модель далеко от коллайдера/агента
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        FindPlayer();
        CachePlayerDamagable();
        SyncAgentToNavMesh();
        EnterIdleNavigation();

        if (debugLogs)
        {
            Debug.Log($"[EnemyAI] Start on {name}. player={(player != null ? player.name : "NULL")}, damageable={(playerDamagable != null ? playerDamagable.GetType().Name : "NULL")}, onNavMesh={(agent != null && agent.isOnNavMesh)}");
        }

        PlayAnimation(idleAnimName);
    }

    void OnEnable()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        SyncAgentToNavMesh();
        if (currentState == EnemyState.Idle)
            EnterIdleNavigation();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            if (player != null)
            {
                CachePlayerDamagable();
                if (debugLogs) Debug.Log($"[EnemyAI] Player reacquired: {player.name}");
            }
            return;
        }

        if (playerDamagable == null)
        {
            CachePlayerDamagable();
        }

        // XZ-дистанция (основание цилиндра)
        // В VR голова (камера) высоко над ногами — XZ позволяет правильно измерять горизонталь
        Vector3 enemyPosXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 playerPosXZ = new Vector3(player.position.x, 0f, player.position.z);
        float distanceToPlayer = Vector3.Distance(enemyPosXZ, playerPosXZ);

        float heightDiff = GetVerticalCombatDelta();

        switch (currentState)
        {
            case EnemyState.Idle:
                EnterIdleNavigation();

                if (distanceToPlayer <= detectionRadius && heightDiff <= detectionHeightRange)
                {
                    ChangeState(EnemyState.Chase);
                    if (debugLogs) Debug.Log($"[EnemyAI] {name}: Idle -> Chase (xzDist={distanceToPlayer:F2}, hDiff={heightDiff:F2})");
                }
                break;

            case EnemyState.Chase:
                if (agent != null)
                {
                    if (!agent.isOnNavMesh)
                        SyncAgentToNavMesh();

                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        if (TryGetChaseDestination(out var chaseDest))
                            agent.SetDestination(chaseDest);
                    }
                    else
                    {
                        EnterIdleNavigation();
                    }
                }

                if (distanceToPlayer <= attackRadius && IsWithinAttackHeight(heightDiff))
                {
                    ChangeState(EnemyState.Attack);
                    attackTimer = timeBetweenAttacks;
                    if (debugLogs) Debug.Log($"[EnemyAI] {name}: Chase -> Attack (xzDist={distanceToPlayer:F2}, hDiff={heightDiff:F2})");
                }
                else if (distanceToPlayer > detectionRadius || heightDiff > detectionHeightRange)
                {
                    ChangeState(EnemyState.Idle);
                    if (debugLogs) Debug.Log($"[EnemyAI] {name}: Chase -> Idle (xzDist={distanceToPlayer:F2}, hDiff={heightDiff:F2})");
                }
                break;

            case EnemyState.Attack:
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

                if (distanceToPlayer > attackRadius || !IsWithinAttackHeight(heightDiff))
                {
                    ChangeState(EnemyState.Chase);
                    if (debugLogs) Debug.Log($"[EnemyAI] {name}: Attack -> Chase (xzDist={distanceToPlayer:F2}, hDiff={heightDiff:F2})");
                    break;
                }

                Vector3 direction = (player.position - transform.position).normalized;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
                }

                AttackPlayer(heightDiff);
                break;
        }
    }

    void EnterIdleNavigation()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    bool TrySampleNavPosition(Vector3 near, out NavMeshHit hit)
    {
        if (!NavMesh.SamplePosition(near, out hit, navMeshSampleRadius, NavMesh.AllAreas))
            return false;

        return Mathf.Abs(hit.position.y - near.y) <= maxNavMeshVerticalSnap;
    }

    void SyncAgentToNavMesh()
    {
        if (agent == null || !agent.enabled) return;

        if (!TrySampleNavPosition(transform.position, out var hit))
            return;

        if (!agent.isOnNavMesh || Vector3.SqrMagnitude(agent.nextPosition - hit.position) > 0.04f)
            agent.Warp(hit.position);
    }

    bool TryGetChaseDestination(out Vector3 destination)
    {
        destination = default;
        if (player == null) return false;

        var target = player.position;
        var cc = player.GetComponentInParent<CharacterController>();
        if (cc != null)
            target = cc.transform.position + cc.center;

        target.y = transform.position.y;

        if (TrySampleNavPosition(target, out var hit))
            destination = hit.position;
        else
            destination = target;

        return true;
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        if (newState == EnemyState.Idle)
            EnterIdleNavigation();

        // Включаем соответствующую анимацию
        switch (currentState)
        {
            case EnemyState.Idle:
                PlayAnimation(idleAnimName);
                break;
            case EnemyState.Chase:
                PlayAnimation(chaseAnimName);
                break;
            case EnemyState.Attack:
                PlayAnimation(attackAnimName);
                break;
        }
    }

    private void PlayAnimation(string animName)
    {
        if (animator != null && !string.IsNullOrEmpty(animName))
        {
            if (animator.runtimeAnimatorController == null)
            {
                if (debugLogs) Debug.LogWarning($"[EnemyAI] {name}: В компоненте Animator не назначен контроллер (Controller)! Анимации не будут работать.");
                return;
            }

            // Плавно переключаем анимацию (crossfade)
            animator.CrossFadeInFixedTime(animName, 0.2f); // CrossFadeInFixedTime иногда работает надежнее с импортированными FBX
            if (debugLogs) Debug.Log($"[EnemyAI] {name}: Пытаюсь включить анимацию '{animName}'");
        }
        else if (animator == null)
        {
            if (debugLogs) Debug.LogWarning($"[EnemyAI] {name}: Компонент Animator не найден! Назначьте его в инспекторе.");
        }
    }

    private void FindPlayer()
    {
        if (player != null) return;

        // Ищем по тегу Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            player = playerObj.transform;
            return;
        }

        // В VR-проектах часто забывают поставить тег Player, но всегда есть MainCamera (голова игрока)
        if (Camera.main != null)
        {
            player = Camera.main.transform;
        }
    }

    private void AttackPlayer(float heightDiff)
    {
        if (!IsWithinAttackHeight(heightDiff))
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer >= timeBetweenAttacks)
        {
            if (playerDamagable != null)
                playerDamagable.SendMessage("TakeDamage", damagePerHit, SendMessageOptions.DontRequireReceiver);

            if (debugLogs)
                Debug.Log($"[EnemyAI] {name}: hit player for {damagePerHit} (hDiff={heightDiff:F2})");

            attackTimer = 0f;
        }
    }

    float GetVerticalCombatDelta()
    {
        float enemyY = transform.position.y + enemyBodyHeightOffset;
        float playerY = GetPlayerBodyWorldY();
        return Mathf.Abs(enemyY - playerY);
    }

    float GetPlayerBodyWorldY()
    {
        if (player == null) return 0f;

        var cc = player.GetComponentInParent<CharacterController>();
        if (cc != null)
            return cc.transform.position.y + cc.center.y;

        // Камера VR — приблизительно центр туловища
        return player.position.y - 1.45f;
    }

    bool IsWithinAttackHeight(float heightDiff) => heightDiff <= attackHeightRange;

    private void CachePlayerDamagable()
    {
        if (player == null)
        {
            playerDamagable = null;
            return;
        }

        // Ищем в родителях (в VRFPS Kit компонент называется Damageable)
        foreach (var component in player.GetComponentsInParent<MonoBehaviour>(true))
        {
            if (component != null && (component.GetType().Name == "Damagable" || component.GetType().Name == "Damageable"))
            {
                playerDamagable = component;
                return;
            }
        }
        
        // Так же ищем в дочерних объектах
        foreach (var component in player.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component != null && (component.GetType().Name == "Damagable" || component.GetType().Name == "Damageable"))
            {
                playerDamagable = component;
                return;
            }
        }

        playerDamagable = null;
    }

    public void ApplyVisualScale()
    {
        transform.localScale = Vector3.one * visualScale;
    }

    /// <summary>Узкая навигация + полное тело для пуль. Вызывается из Start и editor-меню.</summary>
    public void ConfigureBodyColliders(bool rebuildHitVolumes = false)
    {
        RemoveMovementPhysicsCollider();
        DisableRootMeshCollider();

        if (useFullBodyHitVolumes)
            EnsureCompoundHitVolume(rebuildHitVolumes);
        else
            ClearHitVolumeColliders();
    }

    void RemoveMovementPhysicsCollider()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule == null) return;

        if (Application.isPlaying)
            Destroy(capsule);
        else
            DestroyImmediate(capsule);
    }

    void DisableRootMeshCollider()
    {
        var rootMesh = GetComponent<MeshCollider>();
        if (rootMesh != null)
            rootMesh.enabled = false;
    }

    void EnsureCompoundHitVolume(bool forceRebuild)
    {
        var hitRoot = transform.Find(HitVolumeChildName);
        if (hitRoot == null)
        {
            var go = new GameObject(HitVolumeChildName);
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;
            hitRoot = go.transform;
        }

        if (!forceRebuild && hitRoot.GetComponents<Collider>().Length > 0)
            return;

        ClearCollidersOn(hitRoot.gameObject);
        // Локальные размеры (масштабируются вместе с visualScale).
        AddHitBox(hitRoot.gameObject, new Vector3(0f, 0.42f, 0f), new Vector3(0.55f, 0.95f, 0.5f));
        AddHitCapsule(hitRoot.gameObject, new Vector3(0f, 1.05f, 0f), 0.42f, 1.6f);
        AddHitBox(hitRoot.gameObject, new Vector3(0f, 1.82f, 0.04f), new Vector3(0.48f, 0.42f, 0.48f));
        AddHitBox(hitRoot.gameObject, new Vector3(-0.58f, 1.22f, 0f), new Vector3(0.32f, 1.05f, 0.32f));
        AddHitBox(hitRoot.gameObject, new Vector3(0.58f, 1.22f, 0f), new Vector3(0.32f, 1.05f, 0.32f));
    }

    void ClearHitVolumeColliders()
    {
        var hitRoot = transform.Find(HitVolumeChildName);
        if (hitRoot != null)
            ClearCollidersOn(hitRoot.gameObject);
    }

    static void ClearCollidersOn(GameObject target)
    {
        foreach (var col in target.GetComponents<Collider>())
        {
            if (Application.isPlaying)
                Destroy(col);
            else
                DestroyImmediate(col);
        }
    }

    static void AddHitBox(GameObject target, Vector3 center, Vector3 size)
    {
        var box = target.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        box.isTrigger = false;
    }

    static void AddHitCapsule(GameObject target, Vector3 center, float radius, float height)
    {
        var cap = target.AddComponent<CapsuleCollider>();
        cap.center = center;
        cap.radius = radius;
        cap.height = height;
        cap.direction = 1;
        cap.isTrigger = false;
    }

    // Цилиндр XZ + лимит по Y — как в Update (не сфера сквозь этажи).
    private void OnDrawGizmosSelected()
    {
        DrawCombatCylinderGizmo(Color.yellow, detectionRadius, detectionHeightRange);
        DrawCombatCylinderGizmo(Color.red, attackRadius, attackHeightRange);
    }

    void DrawCombatCylinderGizmo(Color color, float xzRadius, float verticalHalfExtent)
    {
        Gizmos.color = color;
        var center = transform.position + Vector3.up * enemyBodyHeightOffset;
        var top = center + Vector3.up * verticalHalfExtent;
        var bottom = center - Vector3.up * verticalHalfExtent;

        DrawWireDisc(center, xzRadius);
        DrawWireDisc(top, xzRadius);
        DrawWireDisc(bottom, xzRadius);

        const int spokes = 4;
        for (var i = 0; i < spokes; i++)
        {
            var angle = i * Mathf.PI * 2f / spokes;
            var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * xzRadius;
            Gizmos.DrawLine(bottom + offset, top + offset);
        }
    }

    static void DrawWireDisc(Vector3 center, float radius, int segments = 24)
    {
        if (radius <= 0f) return;

        var prev = center + new Vector3(radius, 0f, 0f);
        for (var i = 1; i <= segments; i++)
        {
            var angle = i * Mathf.PI * 2f / segments;
            var next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}