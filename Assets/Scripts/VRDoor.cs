using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRDoor : MonoBehaviour
{
    [SerializeField] float openAngle = 90f;
    [SerializeField] float animSpeed = 3f;

    float _closedAngle;
    float _targetAngle;
    bool _isOpen;
    bool _animating;

    void Start()
    {
        _closedAngle = transform.localEulerAngles.y;
        _targetAngle = _closedAngle;
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        _targetAngle = _isOpen ? _closedAngle + openAngle : _closedAngle;
        _animating = true;
    }

    void Update()
    {
        if (!_animating) return;

        float current = transform.localEulerAngles.y;
        float next = Mathf.LerpAngle(current, _targetAngle, Time.deltaTime * animSpeed);
        transform.localEulerAngles = new Vector3(0f, next, 0f);

        if (Mathf.Abs(Mathf.DeltaAngle(next, _targetAngle)) < 0.1f)
        {
            transform.localEulerAngles = new Vector3(0f, _targetAngle, 0f);
            _animating = false;
        }
    }
}
