using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Дверь, которую можно толкнуть: игрок входит в триггер-зону →
/// дверь плавно открывается в сторону от игрока.
/// Закрывается автоматически через autoCloseDelay секунд.
/// Pivot объекта должен быть у края двери (где петли).
/// </summary>
public class PushDoor : MonoBehaviour
{
    [Header("Открытие")]
    [Tooltip("Угол открытия в градусах")]
    public float openAngle = 85f;
    [Tooltip("Скорость анимации")]
    public float speed = 4f;
    [Tooltip("Открывать в обе стороны (определять сторону игрока)")]
    public bool bothWays = true;

    [Header("Авто-закрытие")]
    [Tooltip("Задержка закрытия после ухода игрока (сек). 0 = не закрывать.")]
    public float autoCloseDelay = 3f;

    [Header("Триггер зона")]
    [Tooltip("Толщина зоны обнаружения по глубине двери (в мировых единицах)")]
    public float triggerDepth = 1.2f;

    [Header("Взаимодействие руками (XR)")]
    [Tooltip("Если включено, дверь реагирует на коллайдеры рук / интеракторов XR (можно 'толкать' рукой).")]
    public bool allowHands = true;

    // ── состояние ──────────────────────────────────────────────
    private float _closedAngle;
    private float _targetAngle;
    private float _closeTimer = -1f;
    private int   _inside = 0;         // сколько триггеров от игрока внутри
    private bool  _opened;

    // ── инициализация ──────────────────────────────────────────
    void Awake()
    {
        _closedAngle = transform.localEulerAngles.y;
        _targetAngle = _closedAngle;
    }

    // ── обновление ─────────────────────────────────────────────
    void Update()
    {
        // Плавно поворачиваем
        float cur  = transform.localEulerAngles.y;
        float next = Mathf.LerpAngle(cur, _targetAngle, Time.deltaTime * speed);
        transform.localEulerAngles = new Vector3(0f, next, 0f);

        // Обратный отсчёт до закрытия
        if (_inside == 0 && _closeTimer > 0f)
        {
            _closeTimer -= Time.deltaTime;
            if (_closeTimer <= 0f)
            {
                _targetAngle = _closedAngle;
                _opened      = false;
            }
        }
    }

    // ── триггеры ───────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerOrHand(other)) return;
        _inside++;
        _closeTimer = -1f;

        if (!_opened)
            Open(other.transform.position);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayerOrHand(other)) return;
        _inside = Mathf.Max(0, _inside - 1);

        if (_inside == 0 && autoCloseDelay > 0f)
            _closeTimer = autoCloseDelay;
    }

    // ── логика открытия ────────────────────────────────────────
    void Open(Vector3 playerWorldPos)
    {
        float direction = 1f;

        if (bothWays)
        {
            // Вектор от двери к игроку по XZ
            Vector3 toPlayer = playerWorldPos - transform.position;
            toPlayer.y = 0f;
            // transform.right = вектор вдоль плоскости двери;
            // transform.forward = нормаль к двери.
            // Знак dot(forward, toPlayer) говорит с какой стороны игрок.
            float dot = Vector3.Dot(transform.forward, toPlayer.normalized);
            direction = dot >= 0f ? -1f : 1f;
        }

        _targetAngle = _closedAngle + openAngle * direction;
        _opened      = true;
    }

    // ── публичный метод для ручного открытия ──────────────────
    public void ForceOpen(float dir = 1f)
    {
        _targetAngle = _closedAngle + openAngle * Mathf.Sign(dir);
        _opened      = true;
    }

    public void ForceClose()
    {
        _targetAngle = _closedAngle;
        _opened      = false;
        _inside      = 0;
    }

    // ── вспомогательные ───────────────────────────────────────
    static bool IsPlayer(Collider c)
        => c.gameObject.layer == 6 || c.CompareTag("Player");

    bool IsPlayerOrHand(Collider c)
    {
        if (IsPlayer(c)) return true;
        if (!allowHands) return false;

        // XR hands/controllers often aren't on Player layer/tag.
        if (c.GetComponentInParent<XRBaseInteractor>() != null) return true;

        // Fallback: some setups put hand colliders on Interactable layer.
        // (Project uses layer 11 "Interactable" for pickups.)
        if (c.gameObject.layer == 11) return true;

        return false;
    }
}
