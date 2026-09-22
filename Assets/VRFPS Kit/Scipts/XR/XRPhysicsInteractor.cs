using System.Collections;

using System.Collections.Generic;

using System.Threading.Tasks;

using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactables;

using UnityEngine.XR.Interaction.Toolkit.Interactors;



namespace VRFPSKit

{

    [RequireComponent(typeof(XRBaseInteractor))]

    public class XRPhysicsInteractor : MonoBehaviour

    {

        private async void CreateFixedJoint(IXRInteractable interactable)

        {

            await Task.Delay(100);



            if (ShouldSkipPhysicsJoint(interactable))

                return;



            if (!(interactable is XRGrabInteractable grabInteractable)) return;

            if (!(interactable.transform.GetComponent<Rigidbody>() is Rigidbody rb)) return;



            grabInteractable.trackPosition = false;

            grabInteractable.trackRotation = false;



            GetComponentInParent<Rigidbody>().gameObject.AddComponent<FixedJoint>().connectedBody = rb;

            rb.isKinematic = false;

        }



        private void EndFixedJoint(IXRInteractable interactable)

        {

            if (ShouldSkipPhysicsJoint(interactable))

                return;



            foreach (var fixedJoint in GetComponentInParent<Rigidbody>().GetComponents<FixedJoint>())

                if (fixedJoint.connectedBody == interactable.transform.GetComponent<Rigidbody>())

                    Destroy(fixedJoint);



            if (!(interactable is XRGrabInteractable grabInteractable)) return;

            if (grabInteractable.interactorsSelecting.Count > 0) return;



            grabInteractable.trackPosition = true;

            grabInteractable.trackRotation = true;

        }



        static bool ShouldSkipPhysicsJoint(IXRInteractable interactable)

        {

            if (interactable?.transform == null)

                return false;



            var t = interactable.transform;
            if (t.CompareTag("PassengerNote"))
                return true;

            foreach (var mb in t.GetComponentsInParent<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name == "PassengerNote")
                    return true;
            }

            return false;

        }



        private void Awake()

        {

            foreach (var interactor in GetComponents<XRBaseInteractor>())

            {

                interactor.selectEntered.AddListener(args => CreateFixedJoint(args.interactableObject));

                interactor.selectExited.AddListener(args => EndFixedJoint(args.interactableObject));

            }

        }

    }

}


