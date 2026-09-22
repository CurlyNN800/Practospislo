using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class DoorHandle : MonoBehaviour
{
    [SerializeField] public VRDoor door;

    void Awake()
    {
        var grab = GetComponent<XRGrabInteractable>();
        grab.trackPosition = false;
        grab.trackRotation = false;
        grab.throwOnDetach = false;
        grab.selectEntered.AddListener(_ => door?.Toggle());
    }
}