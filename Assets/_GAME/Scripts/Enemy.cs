using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VRFPSKit;
public class Enemy : MonoBehaviour
{
    [SerializeField] float speed = 0.5f;
    [SerializeField] float attackDistance = 0.6f;
    [SerializeField] float attackCooldown = 1.5f;
    [SerializeField] int damage = 1;

    [Header("Анимации (состояния в Animator Controller и имена клипов)")]
    // Если состояния нет в контроллере — вместо клипа появления плавно растём из 0
    [SerializeField] string spawnState = "Spawn";
    [SerializeField] string spawnClip = "Spawn";
    [SerializeField] float spawnScaleDuration = 0.4f;
    [SerializeField] string attackState = "Attack";
    [SerializeField] string attackClip = "Headbutt";
    [SerializeField] string deathState = "Death";
    [SerializeField] string deathClip = "Death";
    // Состояние, в которое возвращаемся после атаки
    [SerializeField] string idleState = "CharacterArmature|Flying_Idle";
    // Время смерти, если клипа смерти нет
    [SerializeField] float fallbackDeathTime = 1f;

    Transform player;
    float nextAttackTime;
    bool isDead;
    // Пока монстр появляется из портала, он не двигается и не атакует
    bool isSpawning;
    Vector3 normalScale;
    Damageable damageable;
    PlayerHealth playerHealth;
    Animator[] animators;
    Coroutine returnToIdle;

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        animators = GetComponentsInChildren<Animator>();
        normalScale = transform.localScale;

        // Сразу в Awake (до первого кадра), чтобы монстр не мелькнул в полный размер
        isSpawning = true;
        // Сначала ищем клип в контроллере: это работает и до инициализации Animator
        float spawnLength = GetClipLength(spawnClip, -1f);
        if (spawnLength > 0f && PlayState(spawnState))
            StartCoroutine(SpawnByClip(spawnLength));
        else
            StartCoroutine(SpawnByScale());
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
        if (isSpawning || player == null) return;

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
            if (GameStateManager.Instance == null || GameStateManager.Instance.IsPlaying)
            {
                GameAudio.Instance?.PlayMonsterAttack(transform.position);
                PlayAttackAnimation();
            }
            Debug.Log("Монстр укусил игрока на " + damage);
        }
    }

    // --- Появление ---

    IEnumerator SpawnByClip(float length)
    {
        yield return new WaitForSeconds(length);
        isSpawning = false;
    }

    IEnumerator SpawnByScale()
    {
        transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < spawnScaleDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, normalScale, Mathf.SmoothStep(0f, 1f, t / spawnScaleDuration));
            yield return null;
        }
        transform.localScale = normalScale;
        isSpawning = false;
    }

    // --- Атака ---

    void PlayAttackAnimation()
    {
        if (!PlayState(attackState))
            return;

        if (returnToIdle != null)
            StopCoroutine(returnToIdle);
        returnToIdle = StartCoroutine(ReturnToIdleAfter(GetClipLength(attackClip, 1f)));
    }

    IEnumerator ReturnToIdleAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isDead)
            PlayState(idleState);
        returnToIdle = null;
    }

    // --- Смерть ---

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Монстр умер");

        // Останавливаем появление/возврат в idle; движение и атаки уже выключены флагом isDead
        StopAllCoroutines();
        isSpawning = false;

        // Пули больше не попадают в труп
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        GameAudio.Instance?.PlayMonsterDeath(transform.position);

        float deathTime = PlayState(deathState) ? GetClipLength(deathClip, fallbackDeathTime) : fallbackDeathTime;
        // Объект с Enemy живёт до конца клипа, поэтому волна ждёт не дольше анимации смерти.
        // StopLevel удаляет и умирающих монстров сразу (ищет всех Enemy).
        Destroy(gameObject, deathTime);
    }

    // --- Animator ---

    // Запускает состояние во всех Animator монстра, где оно есть. false — такого состояния нет нигде.
    bool PlayState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName))
            return false;

        int hash = Animator.StringToHash(stateName);
        bool played = false;
        foreach (Animator animator in animators)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                continue;
            if (!animator.HasState(0, hash))
                continue;
            animator.CrossFade(hash, 0.1f, 0);
            played = true;
        }
        return played;
    }

    // Длина клипа из контроллера; имя клипа в FBX вида "CharacterArmature|Death"
    float GetClipLength(string clipName, float fallback)
    {
        foreach (Animator animator in animators)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                continue;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && (clip.name == clipName || clip.name.EndsWith("|" + clipName)))
                    return clip.length;
            }
        }
        return fallback;
    }
}
