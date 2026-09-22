using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRDesktopController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotateSpeed = 90f;
    public Transform xrOrigin;

    void Update()
    {
        // Движение WASD
        float x = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float z = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        xrOrigin.Translate(x, 0, z);

        // Вращение Q/E или мышь
        if (Input.GetKey(KeyCode.Q))
            xrOrigin.Rotate(0, -rotateSpeed * Time.deltaTime, 0);
        if (Input.GetKey(KeyCode.E))
            xrOrigin.Rotate(0, rotateSpeed * Time.deltaTime, 0);
    }
}