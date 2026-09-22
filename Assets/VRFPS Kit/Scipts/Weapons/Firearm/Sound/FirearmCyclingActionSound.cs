using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VRFPSKit
{
    [RequireComponent(typeof(AudioSource))]
    public class FirearmCyclingActionSound : MonoBehaviour
    {
        public Vector2 actionPositionRange = new (0, .1f);
        public AudioClip rangeActionSound;
        
        private bool _wasInRangeLastFrame;

        private FirearmCyclingAction _action;

        private void Update()
        {
            //If cocked state changed this frame, play sound
            if(IsActionInRange() && !_wasInRangeLastFrame)
                GetComponent<AudioSource>().PlayOneShot(rangeActionSound);
            
            _wasInRangeLastFrame = IsActionInRange();
        }

        private bool IsActionInRange() => 
            _action.actionPosition01 >= actionPositionRange.x &&
            _action.actionPosition01 <= actionPositionRange.y;

        private void Awake()
        {
            _action = GetComponentInParent<FirearmCyclingAction>();
            if(!_action) Debug.LogError("FirearmCyclingActionSound couldn't find FirearmCyclingAction component in parent");
        }
    }
}
