using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    [RequireComponent(typeof(Rigidbody))]
    public class HandPresence : MonoBehaviour
    {
        public Transform trackedController;
        [Space]
        [Header("Weight Simulation")]
        public bool simulateWeight = true;
        public float linearAcceleration = 1.2f;
        public float angularAcceleration = 1f;
        public float twoHandedWeightMultiplier = .6f;
        public AnimationCurve weightAccelerationMultiplierCurve = AnimationCurve.Linear(0, 1, 100, 0);
        [Space]
        public float handColliderReenableDelay = .25f;
        public float maxDistanceFromTrackedController = .3f;
        
        private Vector3 _parentedPositionLastFrame;
        
        private Rigidbody _rigidbody;
        private Collider[] _handColliders;
        
        
        // Update is called once per frame
        void FixedUpdate()
        {
            if (trackedController == null) return;
            
            _rigidbody.isKinematic = !simulateWeight;
            if (simulateWeight)
            {
                HandsOutOfRangeCheck();
                ApplyPhysicsTrackingForce();
            }
            else
            {
                transform.position = trackedController.position;
                transform.rotation = trackedController.rotation;
            }
        }

        private void ApplyPhysicsTrackingForce()
        {
            //Track controller position
            float accelerationMultiplier = GetWeightAccelerationMultiplier();
            _rigidbody.linearVelocity = (trackedController.position - transform.position) * Time.fixedDeltaTime * linearAcceleration * accelerationMultiplier;
            
            //Track controller rotation
            Quaternion rotationDelta = trackedController.rotation * Quaternion.Inverse(transform.rotation);
            rotationDelta.ToAngleAxis(out float angle, out Vector3 rotationAxis);
            if (angle > 180f)
                angle -= 360f;
            
            Vector3 rotationDifferenceInDegree = angle * rotationAxis.normalized;

            _rigidbody.angularVelocity = rotationDifferenceInDegree * Mathf.Deg2Rad * angularAcceleration * accelerationMultiplier;
        }

        private void HandsOutOfRangeCheck()
        {
            if (Vector3.Distance(transform.position, trackedController.position) > maxDistanceFromTrackedController)
            {
                transform.position = trackedController.position;
                Debug.Log("Physics Hand out of range, resetting position");
            }
        }

        private void Update()
        {
            //TODO make a general script for fixing interactor delay?
            Vector3 parentedPositionDelta = transform.parent.position - _parentedPositionLastFrame;
            ApplyParentMovementToInteractors(transform, parentedPositionDelta);
        }
        
        /// <summary>
        /// All movement of parent Character controller is applied to all held interactables, so they track along player movement and dont lag behind
        /// </summary>
        /// <param name="parentOfInteractors"></param>
        /// <param name="deltaPosition"></param>
        private void ApplyParentMovementToInteractors(Transform parentOfInteractors, Vector3 deltaPosition)
        {
            foreach (var interactor in parentOfInteractors.GetComponentsInChildren<XRBaseInteractor>())
            {
                foreach (var selectedInteractable in interactor.interactablesSelected)
                {
                    if (selectedInteractable.transform.GetComponentInParent<Rigidbody>() is not Rigidbody interactableRigidbody) continue;

                    int contributingInteractorAmount = CountRigidbodyInteractors(interactableRigidbody);
                    
                    //Divide by the amount of interactors contributing
                    interactableRigidbody.transform.position += deltaPosition / contributingInteractorAmount;
                    ApplyParentMovementToInteractors(selectedInteractable.transform, deltaPosition);
                }
            }
            
            _parentedPositionLastFrame = transform.parent.position;
        }
        
        private static int CountRigidbodyInteractors(Rigidbody rb)
        {
            int result = 0;
            foreach (var interactable in rb.GetComponentsInChildren<XRBaseInteractable>())
                foreach (var interactor in interactable.interactorsSelecting)
                    result++;
            
            return result;
        }

        private float GetWeightAccelerationMultiplier()
        {
            float weight = GetCurrentInteractableWeight();

            //Apply two handed weight multiplier if holding interactable two handed
            if (IsInteractableHeldTwoHanded()) 
                weight *= twoHandedWeightMultiplier;
            
            return Mathf.Clamp01(weightAccelerationMultiplierCurve.Evaluate(weight));
        }
        
        private bool IsInteractableHeldTwoHanded()
        {
            foreach (var handInteractor in GetComponentsInChildren<XRBaseInteractor>())
            {
                if (handInteractor.interactablesSelected.Count == 0) return false;
                Rigidbody interactableRigidbody = handInteractor.interactablesSelected[0].transform.GetComponentInParent<Rigidbody>();
                if (interactableRigidbody == null) return false;
                if (CountRigidbodyInteractors(interactableRigidbody) < 2) return false;
                
                return true;
            }

            return false;
        }

        private float GetCurrentInteractableWeight()
        {
            float weightSum = 0;
            foreach (var handInteractor in GetComponentsInChildren<XRBaseInteractor>())
                foreach (var handInteractable in handInteractor.interactablesSelected)
                    weightSum += CalculateNestedInteractableWeight((XRBaseInteractable)handInteractable);
            
            return weightSum;
        }

        public static float CalculateNestedInteractableWeight(XRBaseInteractable interactable)
        {
            Rigidbody interactableRigidbody = interactable.GetComponentInParent<Rigidbody>();

            float nestedInteractableWeightSum = 0;
            foreach (var nestedInteractor in interactableRigidbody.GetComponentsInChildren<XRBaseInteractor>())
                foreach (var nestedInteractable in nestedInteractor.interactablesSelected)
                    nestedInteractableWeightSum += CalculateNestedInteractableWeight((XRBaseInteractable)nestedInteractable);

            return interactableRigidbody.mass + nestedInteractableWeightSum;
        }
        
        private async void ReenableHandCollidersDelayed()
        {
            //Delay in milliseconds
            await Task.Delay((int)(handColliderReenableDelay * 1000));
            
            foreach (Collider handCollider in _handColliders)
                handCollider.enabled = true;
        }
        
        private void DisableHandColliders()
        {
            foreach (Collider handCollider in _handColliders)
                handCollider.enabled = false;
        }
        
        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _handColliders = GetComponentsInChildren<Collider>();

            foreach (var handInteractor in GetComponentsInChildren<XRBaseInteractor>())
            {
                handInteractor.selectEntered.AddListener(_ => DisableHandColliders());
                handInteractor.selectExited.AddListener(_ => ReenableHandCollidersDelayed());
            }
        }
    }
}
