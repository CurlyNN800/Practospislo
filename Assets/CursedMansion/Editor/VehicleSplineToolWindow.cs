#if UNITY_EDITOR
using System.Collections.Generic;
using CursedMansion;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CursedMansion.Editor
{
    /// <summary>
    /// Отдельный редакторский инструмент (окно + Scene View): кривая по маркерам, запекание на землю, запись в <see cref="SimpleWaypointVehicle"/>.
    /// Никаких MonoBehaviour на объектах сцены не требуется.
    /// </summary>
    public sealed class VehicleSplineToolWindow : EditorWindow
    {
        const string MenuPath = "CursedMansion/Path/Vehicle spline tool";
        const string MenuPathWindow = "Window/CursedMansion/Vehicle spline tool";
        const string BakedFolderName = "CM_BakedWaypoints";

        static VehicleSplineToolWindow _instance;

        [SerializeField] List<Vector3> _markersWorld = new();
        [SerializeField] Vector2 _scroll;

        [SerializeField] SimpleWaypointVehicle _vehicle;
        [SerializeField] Transform _bakeParent;
        [SerializeField] int _subdivisionsPerSegment = 10;
        [SerializeField] float _groundOffset = 0.08f;
        [SerializeField] float _raycastFromHeight = 2048f;
        [SerializeField] LayerMask _groundLayers = ~0;

        [MenuItem(MenuPath)]
        [MenuItem(MenuPathWindow)]
        public static void Open()
        {
            var w = GetWindow<VehicleSplineToolWindow>("Vehicle spline");
            w.minSize = new Vector2(320f, 280f);
            w.Show();
        }

        void OnEnable()
        {
            _instance = this;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (_instance == this)
                _instance = null;
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Маркеры в мировых координатах. Scene View: тяните кубики; Ctrl+ЛКМ по коллайдеру — точка в конец.\n" +
                "Запечь — Catmull–Rom, высота по лучу, дочерние wp_* и массив Waypoints у машины.",
                MessageType.Info);

            _vehicle = (SimpleWaypointVehicle)EditorGUILayout.ObjectField(
                "Simple Waypoint Vehicle", _vehicle, typeof(SimpleWaypointVehicle), true);
            _bakeParent = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Родитель для запечённых точек", "Куда вешается папка CM_BakedWaypoints. Пусто = создаётся CM_VehiclePath_Root в сцене."),
                _bakeParent,
                typeof(Transform),
                true);

            _subdivisionsPerSegment = Mathf.Max(2, EditorGUILayout.IntField("Подразбиений на сегмент", _subdivisionsPerSegment));
            _groundOffset = EditorGUILayout.FloatField("Отступ от земли", _groundOffset);
            _raycastFromHeight = EditorGUILayout.FloatField("Луч: старт Y (мир)", _raycastFromHeight);
            _groundLayers = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(
                EditorGUILayout.MaskField(
                    "Слои земли",
                    InternalEditorUtility.LayerMaskToConcatenatedLayersMask(_groundLayers),
                    InternalEditorUtility.layers));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Очистить маркеры"))
                {
                    Undo.RecordObject(this, "Clear spline markers");
                    _markersWorld.Clear();
                }

                if (GUILayout.Button("+ маркер (камера вперёд 5м)"))
                {
                    Undo.RecordObject(this, "Add spline marker");
                    var sc = SceneView.lastActiveSceneView;
                    var o = sc != null ? sc.camera.transform.position + sc.camera.transform.forward * 5f : Vector3.forward * 5f;
                    _markersWorld.Add(o);
                }
            }

            using (new EditorGUI.DisabledScope(_markersWorld.Count < 2 || _vehicle == null))
            {
                if (GUILayout.Button("Запечь кривую → Waypoints", GUILayout.Height(28)))
                    Bake();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"Маркеров: {_markersWorld.Count}", EditorStyles.miniBoldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(120f));
            for (var i = 0; i < _markersWorld.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"#{i}", GUILayout.Width(24f));
                    _markersWorld[i] = EditorGUILayout.Vector3Field(GUIContent.none, _markersWorld[i]);
                    if (GUILayout.Button("×", GUILayout.Width(22f)))
                    {
                        Undo.RecordObject(this, "Remove marker");
                        _markersWorld.RemoveAt(i);
                        break;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (_instance != this)
                return;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            if (_markersWorld.Count >= 2)
            {
                Handles.color = new Color(1f, 0.75f, 0.1f, 1f);
                for (var i = 0; i < _markersWorld.Count - 1; i++)
                    Handles.DrawAAPolyLine(3f, _markersWorld[i], _markersWorld[i + 1]);
            }

            Handles.color = Color.cyan;
            for (var i = 0; i < _markersWorld.Count; i++)
            {
                var wp = _markersWorld[i];
                var sz = HandleUtility.GetHandleSize(wp) * 0.12f;
                Handles.CubeHandleCap(0, wp, Quaternion.identity, sz, EventType.Repaint);

                EditorGUI.BeginChangeCheck();
                var nw = Handles.PositionHandle(wp, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Move spline marker");
                    _markersWorld[i] = nw;
                    Repaint();
                }
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && e.control)
            {
                var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out var hit, 1e5f, _groundLayers, QueryTriggerInteraction.Ignore))
                {
                    Undo.RecordObject(this, "Ctrl+click add spline marker");
                    _markersWorld.Add(hit.point + hit.normal * _groundOffset);
                    e.Use();
                    Repaint();
                }
            }

            if (_markersWorld.Count > 0)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(10f, 10f, 260f, 22f));
                GUILayout.Label("Vehicle spline tool (окно открыто)", EditorStyles.boldLabel);
                GUILayout.EndArea();
                Handles.EndGUI();
            }

        }

        void Bake()
        {
            if (_vehicle == null)
            {
                EditorUtility.DisplayDialog("Vehicle spline", "Назначь Simple Waypoint Vehicle.", "OK");
                return;
            }

            if (_markersWorld.Count < 2)
            {
                EditorUtility.DisplayDialog("Vehicle spline", "Нужно минимум 2 маркера.", "OK");
                return;
            }

            var controls = new List<Vector3>(_markersWorld);
            var subdiv = _subdivisionsPerSegment;
            var curve = new List<Vector3>();
            for (var i = 0; i < controls.Count - 1; i++)
            {
                var p0 = controls[Mathf.Max(0, i - 1)];
                var p1 = controls[i];
                var p2 = controls[i + 1];
                var p3 = controls[Mathf.Min(controls.Count - 1, i + 2)];
                for (var s = 0; s < subdiv; s++)
                {
                    var t = s / (float)subdiv;
                    curve.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }

            curve.Add(controls[^1]);

            var snapped = new List<(Vector3 pos, Vector3 normal)>(curve.Count);
            foreach (var p in curve)
            {
                if (!TrySnapToGround(p, out var pos, out var normal))
                {
                    EditorUtility.DisplayDialog(
                        "Vehicle spline",
                        $"Луч не попал в землю около ({p.x:F1}, {p.z:F1}).",
                        "OK");
                    return;
                }

                snapped.Add((pos + normal * _groundOffset, normal));
            }

            var parentTr = _bakeParent;
            if (parentTr == null)
            {
                var rootGo = new GameObject("CM_VehiclePath_Root");
                Undo.RegisterCreatedObjectUndo(rootGo, "Vehicle path root");
                parentTr = rootGo.transform;
                if (_vehicle != null && _vehicle.transform.parent != null)
                    Undo.SetTransformParent(parentTr, _vehicle.transform.parent, "Parent path root");
            }

            var existing = parentTr.Find(BakedFolderName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            var folder = new GameObject(BakedFolderName);
            Undo.RegisterCreatedObjectUndo(folder, "Bake vehicle spline");
            folder.transform.SetParent(parentTr, false);
            folder.transform.localPosition = Vector3.zero;
            folder.transform.localRotation = Quaternion.identity;
            folder.transform.localScale = Vector3.one;

            var bakedTransforms = new List<Transform>(snapped.Count);
            for (var i = 0; i < snapped.Count; i++)
            {
                var go = new GameObject($"wp_{i:000}");
                Undo.RegisterCreatedObjectUndo(go, "Bake waypoint");
                go.transform.SetParent(folder.transform, worldPositionStays: true);
                go.transform.position = snapped[i].pos;

                var up = snapped[i].normal;
                Vector3 tangent;
                if (i == 0)
                    tangent = snapped[1].pos - snapped[0].pos;
                else if (i == snapped.Count - 1)
                    tangent = snapped[i].pos - snapped[i - 1].pos;
                else
                    tangent = snapped[i + 1].pos - snapped[i - 1].pos;

                var forward = Vector3.ProjectOnPlane(tangent, up);
                if (forward.sqrMagnitude < 1e-6f)
                    forward = Vector3.ProjectOnPlane(Vector3.forward, up);
                go.transform.rotation = Quaternion.LookRotation(forward.normalized, up.normalized);
                bakedTransforms.Add(go.transform);
            }

            var so = new SerializedObject(_vehicle);
            var wp = so.FindProperty("waypoints");
            wp.ClearArray();
            for (var i = 0; i < bakedTransforms.Count; i++)
            {
                wp.InsertArrayElementAtIndex(i);
                wp.GetArrayElementAtIndex(i).objectReferenceValue = bakedTransforms[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_vehicle);

            if (parentTr.gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(parentTr.gameObject.scene);

            Selection.activeGameObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        bool TrySnapToGround(Vector3 worldHint, out Vector3 position, out Vector3 normal)
        {
            var origin = new Vector3(worldHint.x, _raycastFromHeight, worldHint.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, _raycastFromHeight + 8192f, _groundLayers, QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
                normal = hit.normal;
                return true;
            }

            var terrain = Terrain.activeTerrain;
            if (terrain != null && terrain.terrainData != null)
            {
                var t = terrain.transform;
                var local = t.InverseTransformPoint(worldHint);
                var h = terrain.terrainData.GetInterpolatedHeight(
                    local.x / terrain.terrainData.size.x,
                    local.z / terrain.terrainData.size.z);
                var y = h + t.position.y;
                position = new Vector3(worldHint.x, y, worldHint.z);
                normal = Vector3.up;
                return true;
            }

            position = default;
            normal = Vector3.up;
            return false;
        }
    }
}
#endif
