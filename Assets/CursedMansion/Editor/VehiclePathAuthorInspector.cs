#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CursedMansion.Editor
{
    [CustomEditor(typeof(VehiclePathAuthor))]
    public sealed class VehiclePathAuthorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Путь для машины задаётся отдельным окном (не этим компонентом).\n\n" +
                "Верхнее меню: CursedMansion → Path → Vehicle spline tool\n\n" +
                "После перехода на инструмент этот компонент можно удалить.",
                MessageType.Info);

            if (GUILayout.Button("Открыть Vehicle spline tool"))
                VehicleSplineToolWindow.Open();

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Удалить этот компонент"))
                Undo.DestroyObjectImmediate(target);
        }
    }
}
#endif
