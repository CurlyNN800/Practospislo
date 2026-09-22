using UnityEngine;

namespace CursedMansion
{
    /// <summary>Корневой объект на сцене Road: пролог + визуал финала.</summary>
    public class RoadRuntimePack : MonoBehaviour
    {
        void Reset()
        {
            if (GetComponent<RoadIntroSequenceController>() == null)
                gameObject.AddComponent<RoadIntroSequenceController>();
            if (GetComponent<RoadEndingVisualApplier>() == null)
                gameObject.AddComponent<RoadEndingVisualApplier>();
            if (GetComponent<AudioSource>() == null)
            {
                var a = gameObject.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.spatialBlend = 1f;
            }
        }

        void Awake()
        {
            if (GetComponent<RoadIntroSequenceController>() == null)
                gameObject.AddComponent<RoadIntroSequenceController>();
            if (GetComponent<RoadEndingVisualApplier>() == null)
                gameObject.AddComponent<RoadEndingVisualApplier>();
            if (GetComponent<AudioSource>() == null)
            {
                var a = gameObject.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.spatialBlend = 1f;
            }
        }
    }
}
