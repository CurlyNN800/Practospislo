using UnityEngine;

namespace CursedMansion
{
    [CreateAssetMenu(menuName = "CursedMansion/Note Data", fileName = "NoteData")]
    public class NoteData : ScriptableObject
    {
        [SerializeField] string title = "Записка";
        [TextArea(4, 24)] [SerializeField] string body = "";

        public string Title => title;
        public string Body => body;
    }
}
