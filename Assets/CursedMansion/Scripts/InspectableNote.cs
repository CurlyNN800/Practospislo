using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Записка: при входе игрока в триггер показывает текст (NoteData или fallback).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InspectableNote : MonoBehaviour
    {
        [SerializeField] NoteData data;
        [SerializeField] string fallbackTitle = "Записка";
        [TextArea(4, 16)] [SerializeField] string fallbackBody = "Текст записки…";
        [SerializeField] bool showOnce = true;

        bool _shown;

        void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
                return;

            if (showOnce && _shown) return;
            _shown = true;

            string t = data != null ? data.Title : fallbackTitle;
            string b = data != null ? data.Body : fallbackBody;
            NoteOverlay.ShowNote(t, b);
        }
    }
}
