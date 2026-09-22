using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VRFPSKit
{
    [RequireComponent(typeof(AudioSource))]
    public class FirearmCyclingActionEjectSound : MonoBehaviour
    {
        public AudioClip ejectSound;
        
        private CartridgeEjector _ejector;

        private void OnEject(Cartridge obj)
        {
            GetComponent<AudioSource>().PlayOneShot(ejectSound);
        }

        private void Awake()
        {
            _ejector = GetComponentInParent<CartridgeEjector>();
            if (!_ejector)
            {
                Debug.LogError("FirearmCyclingActionEjectSound couldn't find CartridgeEjector component in parent"); 
                return;
            }
            
            _ejector.EjectEvent += OnEject;
        }
    }
}
