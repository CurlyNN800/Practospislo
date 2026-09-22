using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// Attach this to the audio source of the suppressed sound, and it will automatically change the
    /// firearm shoot sound when suppressorSocket is filled
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FirearmSuppressorSound : MonoBehaviour
    {
        public XRSocketInteractor suppressorSocket;

        private AudioSource _suppressedShotSource;
        private AudioSource _unsuppressedShotSource;
        
        private XRSocketInteractor _socket;
        private FirearmAnimator _firearmAnimator;

        private void AttachSuppressor(SelectEnterEventArgs arg0) => _firearmAnimator.shootSound = _suppressedShotSource;
        private void DetachSuppressor(SelectExitEventArgs arg0) => _firearmAnimator.shootSound = _unsuppressedShotSource;

        private void Awake()
        {
            suppressorSocket.selectEntered.AddListener(AttachSuppressor);
            suppressorSocket.selectExited.AddListener(DetachSuppressor);
            
            _firearmAnimator = GetComponentInParent<FirearmAnimator>();
            if(!_firearmAnimator) Debug.LogError("FirearmSuppressorSound couldn't find FirearmAnimator component in parent");
            
            _unsuppressedShotSource = _firearmAnimator.shootSound;
            _suppressedShotSource = GetComponent<AudioSource>();
        }
    }
}
