using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RadioScript : MonoBehaviour
{
    [Header("UI ш Чтѓъ")]
    [SerializeField] private GameObject canvasUI; 
    [SerializeField] private AudioSource radioAudio; 

    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();


        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        if (canvasUI != null) canvasUI.SetActive(false);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (canvasUI != null) canvasUI.SetActive(true);

        if (radioAudio != null)
        {
            radioAudio.Play();
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {

        if (canvasUI != null) canvasUI.SetActive(false);


        if (radioAudio != null)
        {
            radioAudio.Stop();
        }
    }
}