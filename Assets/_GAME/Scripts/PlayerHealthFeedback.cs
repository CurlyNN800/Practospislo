using TMPro;
using UnityEngine;
using VRFPSKit;

// Мост между PlayerHealth и обратной связью VRFPS Kit.
// Кит показывает HP, виньетку и звук ранения от Damageable на корне VR Player, но урон игроку идёт
// через PlayerHealth (Damageable обезврежен). Здесь те же элементы кита подключаются к PlayerHealth:
//  - часы на руке (HealthText) показывают "HP текущее/максимум";
//  - при уроне перед камерой вспыхивает красная плоскость (DamageFlash). Виньетка кита
//    (DamagePostProcessFilter) не используется: она требует Post Processing, а он выключен ради passthrough;
//  - при уроне играет AudioSource кита из DamageSound (bullet wound.wav).
// Добавляется автоматически из PlayerHealth.Awake, если его нет на объекте.
[RequireComponent(typeof(PlayerHealth))]
public class PlayerHealthFeedback : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float woundVolume = 1f;

    PlayerHealth playerHealth;
    TMP_Text healthText;
    DamageFlash damageFlash;
    AudioSource woundSource;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();

        // Корень рига VR Player — объект с Damageable (иначе просто корень иерархии)
        Damageable rootDamageable = GetComponentInParent<Damageable>();
        Transform rigRoot = rootDamageable != null ? rootDamageable.transform : transform.root;

        // Часы: забираем текст у DamageableHealthTextUI и отключаем его,
        // иначе он каждый кадр перезапишет текст здоровьем Damageable (1e9)
        foreach (DamageableHealthTextUI kitText in rigRoot.GetComponentsInChildren<DamageableHealthTextUI>(true))
        {
            if (healthText == null)
                healthText = kitText.text;
            kitText.enabled = false;
        }
        if (healthText == null)
            Debug.LogWarning("[PlayerHealthFeedback] Не найден HealthText (DamageableHealthTextUI) на риге — HP на руке не будет.");

        // PlayerHealth висит на камере VR Player
        Camera cam = GetComponent<Camera>() != null ? GetComponent<Camera>() : Camera.main;
        if (cam != null)
            damageFlash = DamageFlash.Create(cam);
        else
            Debug.LogWarning("[PlayerHealthFeedback] Не найдена камера — вспышки урона не будет.");

        DamageSound kitSound = rigRoot.GetComponentInChildren<DamageSound>(true);
        if (kitSound != null)
            woundSource = kitSound.audioSource;
        if (woundSource == null || woundSource.clip == null)
            Debug.LogWarning("[PlayerHealthFeedback] Не найден AudioSource звука ранения (DamageSound) — звука урона не будет.");
    }

    void OnEnable()
    {
        playerHealth.Damaged += OnDamaged;
        playerHealth.HealthChanged += OnHealthChanged;
    }

    void OnDisable()
    {
        playerHealth.Damaged -= OnDamaged;
        playerHealth.HealthChanged -= OnHealthChanged;
    }

    void Start()
    {
        OnHealthChanged(playerHealth.Health, playerHealth.MaxHealth);
    }

    void OnDamaged(int amount)
    {
        if (damageFlash != null)
            damageFlash.Flash();

        if (woundSource != null && woundSource.clip != null)
            woundSource.PlayOneShot(woundSource.clip, woundVolume);
    }

    void OnHealthChanged(int current, int max)
    {
        if (healthText != null)
            healthText.text = $"HP {current}/{max}";
    }
}
