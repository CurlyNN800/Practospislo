using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRFPSKit;

namespace CursedMansion
{
    /// <summary>
    /// URP post-processing VFX for player damage (vignette flash + low HP vignette).
    /// Creates its own Global Volume at runtime so it works in XR/URP.
    /// </summary>
    public class PlayerDamageVfx : MonoBehaviour
    {
        const string VolumeGoName = "CM_PlayerDamageVfxVolume";

        [Header("Flash (on hit)")]
        [SerializeField] float flashIntensity = 0.35f;
        [SerializeField] float flashInTime = 0.05f;
        [SerializeField] float flashOutTime = 0.35f;

        [Header("Low HP vignette (constant)")]
        [SerializeField] float lowHpStart01 = 0.55f; // below this HP%, start showing constant vignette
        [SerializeField] float minConstantIntensity = 0.05f;
        [SerializeField] float maxConstantIntensity = 0.35f;
        [SerializeField] float constantAlphaExponent = 1.6f;

        [Header("Post-processing look")]
        [SerializeField] Color vignetteColor = new(0.75f, 0f, 0f, 1f);
        [SerializeField] float vignetteSmoothness = 0.55f;

        Damageable _playerDamageable;
        Volume _volume;
        VolumeProfile _profile;
        Vignette _vignette;

        float _flashT;
        float _flashDuration;
        float _lastHealth01 = 1f;

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Damageable.GlobalDamageEvent += OnGlobalDamage;
            Damageable.GlobalDeathEvent += OnGlobalDeath;
            TryFindPlayerDamageable();
            EnsureVolume();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Damageable.GlobalDamageEvent -= OnGlobalDamage;
            Damageable.GlobalDeathEvent -= OnGlobalDeath;
        }

        void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            TryFindPlayerDamageable();
            EnsureVolume();
        }

        void Update()
        {
            if (_vignette == null) return;

            var health01 = GetHealth01();
            _lastHealth01 = health01;

            var constant = ComputeConstantAlpha(health01);
            var flash = ComputeFlashAlpha();
            var intensity = Mathf.Clamp01(Mathf.Max(constant, flash));

            _vignette.active = intensity > 0.0001f;
            _vignette.intensity.Override(intensity);
            _vignette.color.Override(vignetteColor);
            _vignette.smoothness.Override(vignetteSmoothness);
            _vignette.rounded.Override(true);
        }

        void OnGlobalDamage(Damageable d, float damage)
        {
            if (!IsPlayerDamageable(d)) return;
            StartFlash();
        }

        void OnGlobalDeath(Damageable d)
        {
            if (!IsPlayerDamageable(d)) return;
            // On death we keep the vignette as-is; game over UI will appear.
            // But stop flashing so it doesn't animate during pause.
            _flashDuration = 0f;
        }

        void StartFlash()
        {
            _flashT = 0f;
            _flashDuration = Mathf.Max(0.01f, flashInTime + flashOutTime);
        }

        float ComputeFlashAlpha()
        {
            if (_flashDuration <= 0f) return 0f;

            _flashT += Time.unscaledDeltaTime;
            if (_flashT >= _flashDuration)
            {
                _flashDuration = 0f;
                return 0f;
            }

            var t = _flashT;
            if (t <= flashInTime)
            {
                var k = Mathf.Clamp01(t / Mathf.Max(0.0001f, flashInTime));
                return Mathf.Lerp(0f, flashIntensity, k);
            }

            var outT = t - flashInTime;
            var kOut = Mathf.Clamp01(outT / Mathf.Max(0.0001f, flashOutTime));
            return Mathf.Lerp(flashIntensity, 0f, kOut);
        }

        float ComputeConstantAlpha(float health01)
        {
            if (health01 >= lowHpStart01) return 0f;

            var t = Mathf.InverseLerp(lowHpStart01, 0f, health01); // 0..1
            t = Mathf.Pow(t, constantAlphaExponent);
            return Mathf.Lerp(minConstantIntensity, maxConstantIntensity, t);
        }

        float GetHealth01()
        {
            if (_playerDamageable == null) return _lastHealth01;
            var start = Mathf.Max(1f, _playerDamageable.startHealth);
            return Mathf.Clamp01(_playerDamageable.health / start);
        }

        bool IsPlayerDamageable(Damageable d)
        {
            if (d == null) return false;
            if (d.CompareTag("Player")) return true;
            var root = d.transform.root;
            return root != null && root.CompareTag("Player");
        }

        void TryFindPlayerDamageable()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                _playerDamageable = null;
                return;
            }

            if (playerObj.GetComponentInChildren<Damageable>(true) is Damageable d)
                _playerDamageable = d;
        }

        void EnsureVolume()
        {
            if (_volume != null && _profile != null && _vignette != null) return;

            var existing = GameObject.Find(VolumeGoName);
            if (existing == null)
            {
                existing = new GameObject(VolumeGoName);
                DontDestroyOnLoad(existing);
            }

            _volume = existing.GetComponent<Volume>();
            if (_volume == null) _volume = existing.AddComponent<Volume>();

            _volume.isGlobal = true;
            _volume.priority = 999f;
            _volume.weight = 1f;

            if (_profile == null)
            {
                _profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _profile.hideFlags = HideFlags.HideAndDontSave;
            }

            _volume.profile = _profile;

            if (!_profile.TryGet(out _vignette))
                _vignette = _profile.Add<Vignette>(true);

            // Ensure parameters are overridable.
            _vignette.color.overrideState = true;
            _vignette.intensity.overrideState = true;
            _vignette.smoothness.overrideState = true;
            _vignette.rounded.overrideState = true;
        }
    }
}

