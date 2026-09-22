using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Binds <see cref="InputSystemUIInputModule"/> to a project Input Actions asset (UI map) at runtime,
/// avoiding broken serialized InputActionReference fileIDs in scenes.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class VrEventSystemUiInputBinder : MonoBehaviour
{
    [SerializeField] InputActionAsset _actions;
    [SerializeField] string _uiMapName = "UI";

    void Awake()
    {
        if (_actions == null)
            return;

        var module = GetComponent<InputSystemUIInputModule>();
        if (module == null)
            return;

        var map = _actions.FindActionMap(_uiMapName);
        if (map == null)
            return;

        static InputActionReference Ref(InputAction a) => a != null ? InputActionReference.Create(a) : null;

        var point = map.FindAction("Point");
        var navigate = map.FindAction("Navigate");
        var click = map.FindAction("Click");
        if (point == null || navigate == null || click == null)
            return;

        module.actionsAsset = _actions;
        module.point = Ref(point);
        module.move = Ref(navigate);
        module.leftClick = Ref(click);
        module.rightClick = Ref(map.FindAction("RightClick"));
        module.middleClick = Ref(map.FindAction("MiddleClick"));
        module.scrollWheel = Ref(map.FindAction("ScrollWheel"));
        module.submit = Ref(map.FindAction("Submit"));
        module.cancel = Ref(map.FindAction("Cancel"));
        module.trackedDevicePosition = Ref(map.FindAction("TrackedDevicePosition"));
        module.trackedDeviceOrientation = Ref(map.FindAction("TrackedDeviceOrientation"));
    }
}
