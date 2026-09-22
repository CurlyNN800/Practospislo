using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Вращает руль вокруг его СОБСТВЕННОЙ оси в зависимости от угловой скорости машины по Y.
    /// </summary>
    public class SteeringWheelFollow : MonoBehaviour
    {
        [Tooltip("Трансформ корня машины (Car 4).")]
        [SerializeField] Transform car;

        [Tooltip("Ось вращения в ЛОКАЛЬНОМ пространстве самого руля. " +
                 "Зависит от модели: попробуй (0,0,1) forward, (0,1,0) up или (1,0,0) right.")]
        [SerializeField] Vector3 spinAxisLocal = Vector3.forward;

        [Tooltip("Сколько градусов поворота руля на 1 градус/с угловой скорости машины.")]
        [SerializeField] float steerMultiplier = 8f;

        [Tooltip("Максимальный угол отклонения руля в каждую сторону.")]
        [SerializeField] float maxWheelAngle = 360f;

        [Tooltip("Скорость следования за поворотом / возврата к нейтрали.")]
        [SerializeField] float smoothing = 6f;

        Quaternion _baseLocalRotation;
        // Центр меша в локальном пространстве объекта (реальный пивот руля)
        Vector3 _meshCenterLocal;

        float _prevYaw;
        float _wheelAngle;

        void Start()
        {
            _baseLocalRotation = transform.localRotation;

            // Берём центр bounds меша как реальный центр колеса руля.
            // Именно вокруг этой точки и будем крутить.
            var mf = GetComponent<UnityEngine.MeshFilter>();
            _meshCenterLocal = mf != null && mf.sharedMesh != null
                ? mf.sharedMesh.bounds.center
                : Vector3.zero;

            _prevYaw = car != null ? car.eulerAngles.y : 0f;
        }

        void Update()
        {
            if (car == null) return;

            // 1. Считаем скорость поворота машины
            float yaw = car.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(_prevYaw, yaw);
            _prevYaw = yaw;

            float angularRate = deltaYaw / Mathf.Max(Time.deltaTime, 0.0001f);
            float target = Mathf.Clamp(angularRate * steerMultiplier, -maxWheelAngle, maxWheelAngle);

            // Плавный поворот
            _wheelAngle = Mathf.Lerp(_wheelAngle, target, smoothing * Time.deltaTime);

            // 2. ПРИМЕНЯЕМ ПОВОРОТ БЕЗОПАСНО
            // Вместо RotateAround используем локальное вращение вокруг выбранной оси
            transform.localRotation = _baseLocalRotation * Quaternion.AngleAxis(_wheelAngle, spinAxisLocal);
        }
    }
}
