using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRFPSKit
{
    /// <summary>
    /// URP-версия фильтра урона: вес Volume зависит от здоровья игрока (меньше HP — сильнее эффект).
    /// Если у Volume нет профиля, создаётся профиль по умолчанию (красная Vignette + ColorAdjustments).
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class DamagePostProcessFilter : MonoBehaviour
    {
        //Полное HP даёт вес 0, нулевое HP — вес 1
        public AnimationCurve profileHealthWeightCurve = AnimationCurve.Linear(0, 1, 1, 0);

        [Tooltip("Скорость изменения веса эффекта в секунду. 0 — мгновенно, как в старой версии.")]
        public float weightChangeSpeed = 2f;

        [Header("Профиль по умолчанию (если у Volume не назначен свой)")]
        public Color vignetteColor = new Color(0.6f, 0f, 0f, 1f);
        [Range(0f, 1f)] public float vignetteIntensity = 0.45f;
        [Range(0f, 1f)] public float vignetteSmoothness = 0.5f;
        [Range(-100f, 0f)] public float saturation = -60f;

        private Volume _volume;
        private Damageable _damageable;
        private VolumeProfile _runtimeProfile;

        private void Reset()
        {
            //При добавлении компонента в редакторе делаем Volume глобальным, как был PostProcessVolume у игрока
            GetComponent<Volume>().isGlobal = true;
        }

        private void Awake()
        {
            _volume = GetComponent<Volume>();
            _damageable = GetComponentInParent<Damageable>();

            if (!_damageable) Debug.LogError("DamagePostProcessFilter не нашёл Damageable в родителях, эффект урона работать не будет.");

            if (_volume.sharedProfile == null)
                CreateDefaultProfile();

            _volume.weight = 0f;
        }

        private void Update()
        {
            if (!_damageable || _damageable.startHealth <= 0f) return;

            float health01 = Mathf.Clamp01(_damageable.health / _damageable.startHealth);
            float targetWeight = Mathf.Clamp01(profileHealthWeightCurve.Evaluate(health01));

            _volume.weight = weightChangeSpeed <= 0f
                ? targetWeight
                : Mathf.MoveTowards(_volume.weight, targetWeight, weightChangeSpeed * Time.deltaTime);
        }

        private void CreateDefaultProfile()
        {
            _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _runtimeProfile.name = "DamagePostProcessFilter (runtime)";

            var vignette = _runtimeProfile.Add<Vignette>(true);
            vignette.color.Override(vignetteColor);
            vignette.intensity.Override(vignetteIntensity);
            vignette.smoothness.Override(vignetteSmoothness);

            var colorAdjustments = _runtimeProfile.Add<ColorAdjustments>(true);
            colorAdjustments.saturation.Override(saturation);

            _volume.sharedProfile = _runtimeProfile;
        }

        private void OnDestroy()
        {
            if (_runtimeProfile) Destroy(_runtimeProfile);
        }
    }
}
