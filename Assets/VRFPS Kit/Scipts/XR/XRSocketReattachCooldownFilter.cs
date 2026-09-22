using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    public class XRSocketReattachCooldownFilter : IXRSelectFilter
    {
        private const float ReattachCooldownDelay = .5f;
        private static readonly Dictionary<XRSocketInteractor, float> LastSocketExitTime = new();

        public bool canProcess => true;

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (interactor is not XRSocketInteractor socket) return true;
            //Return if already attached (we only want cooldown to be applied when interaction is started)
            if (interactor.IsSelecting(interactable)) return true;
            
            //Listen to select exit event, and store exit time in LastSocketExitTime
            socket.selectExited.AddListener(StoreSocketExitTime);
            
            LastSocketExitTime.TryGetValue(socket, out float exitTime);
            return (Time.time - exitTime > ReattachCooldownDelay);
        }

        private void StoreSocketExitTime(SelectExitEventArgs args)
        {
            XRSocketInteractor socket = (XRSocketInteractor)args.interactorObject;
            
            LastSocketExitTime[socket] = Time.time; 
            
            //Only need to call this once
            socket.selectExited.RemoveListener(StoreSocketExitTime);
        }
    }
}
