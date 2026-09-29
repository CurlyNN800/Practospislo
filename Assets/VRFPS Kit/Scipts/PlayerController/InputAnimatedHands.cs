using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRFPSKit
{
    /// <summary>
    /// Animates player hands according to index finger and grip
    /// </summary>
    public class InputAnimatedHands : MonoBehaviour
    {
        public InputActionProperty pinchAnimationAction;
        public InputActionProperty gripAnimationAction;
        public Animator handAnimator;

        // Update is called once per frame
        void Update()
        {
            // Без аниматора анимировать нечего
            if (handAnimator == null)
                return;

            var pinchAction = pinchAnimationAction.action;
            if (pinchAction != null)
                handAnimator.SetFloat("Trigger", pinchAction.ReadValue<float>());

            var gripAction = gripAnimationAction.action;
            if (gripAction != null)
                handAnimator.SetFloat("Grip", gripAction.ReadValue<float>());
        }
    }
}
