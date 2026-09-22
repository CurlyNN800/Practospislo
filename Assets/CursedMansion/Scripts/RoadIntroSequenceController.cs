using System.Collections;
using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Опциональный триггер в конце Road: рык → fade → HouseScene.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoadIntroSequenceController : MonoBehaviour
    {
        public static bool AllowAutoSceneChange = true;

        [Header("Сцена")]
        [SerializeField] string targetSceneName = GameScenes.HouseScene;
        [SerializeField] string targetSpawnId = "FromRoad";

        [Header("Эффекты")]
        [SerializeField] AudioClip roarClip;
        [SerializeField] float delayAfterRoarSeconds = 1f;
        [SerializeField] float fadeOutSeconds = 3f;
        [SerializeField] float fadeInSeconds = 2f;

        AudioSource _audio;
        bool _isTransitioning;

        void Awake()
        {
            EnsureTriggerCollider();

            _audio = GetComponent<AudioSource>();
            if (roarClip == null)
                roarClip = Resources.Load<AudioClip>("CM_Roar");
        }

        void EnsureTriggerCollider()
        {
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(12f, 6f, 12f);
                return;
            }

            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!AllowAutoSceneChange || _isTransitioning || !SceneTransfer.IsPlayerCollider(other))
                return;

            Debug.Log("[RoadIntro] Триггер: " + other.name);
            StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            _isTransitioning = true;

            if (_audio != null && roarClip != null)
                _audio.PlayOneShot(roarClip);

            if (delayAfterRoarSeconds > 0f)
                yield return new WaitForSeconds(delayAfterRoarSeconds);

            if (GameProgress.Instance != null)
                GameProgress.Instance.EnterMansionExterior();

            SceneTransitionRunner.TransitionTo(targetSceneName, targetSpawnId, fadeOutSeconds, fadeInSeconds);
        }
    }
}
