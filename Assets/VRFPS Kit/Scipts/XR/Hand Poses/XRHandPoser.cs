using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit.HandPoses
{
    public class XRHandPoser : BaseHand
    {
        public XRDirectInteractor[] interactors;
        
        private XRHandPoseContainer _poseContainer;
        
        private void FixedUpdate()
        {
            if(!_poseContainer) return;
            
            ApplyPose(_poseContainer.pose);
        }
        
        private async void OnSelectEntered(SelectEnterEventArgs args)
        {
            //Wait one frame for potential interactor transfer to complete before getting interactor index
            await Task.Delay(50);
            
            if (GetComponent<Animator>() is Animator animator) animator.enabled = false;
            
            //Get index of current interactor in interactable selecting list
            int interactorIndex = args.interactableObject.interactorsSelecting.FindIndex(selectingInteractor => selectingInteractor == args.interactorObject);
            
            //Find a hand pose with a matching interactor index
            foreach (var pose in args.interactableObject.transform.GetComponents<XRHandPoseContainer>())
                if(pose.interactorIndex == interactorIndex)
                    _poseContainer = pose;
            
            if(!_poseContainer) return;
            
            attachTransform = _poseContainer.GetAttachPoint();
            ApplyPose(_poseContainer.pose);
        }
        
        private void OnSelectExited(SelectExitEventArgs args)
        {
            attachTransform = ((XRDirectInteractor)args.interactorObject).attachTransform;
            
            _poseContainer = null;
            ApplyPose(defaultPose);

            if (GetComponent<Animator>() is Animator animator) animator.enabled = true;
        }
        
        // Update is called once per frame
        protected override void Awake()
        {
            base.Awake();

            attachTransform = interactors[0].attachTransform;
            
            ApplyPose(defaultPose);
            
            foreach (var interactor in interactors)
            {
                interactor.selectEntered.AddListener(OnSelectEntered);
                interactor.selectExited.AddListener(OnSelectExited);
            }
        }
    }
}
