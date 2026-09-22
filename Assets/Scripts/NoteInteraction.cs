using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NoteDisplay : MonoBehaviour
{
    [SerializeField] private GameObject canvasUI; 

    private void Start()
    {
        if (canvasUI == null)
            return;

        var interactable = GetComponent<XRGrabInteractable>();
        if (interactable == null)
            return;

        interactable.selectEntered.AddListener(_ => canvasUI.SetActive(true));
        interactable.selectExited.AddListener(_ => canvasUI.SetActive(false));
        canvasUI.SetActive(false);
    }
}