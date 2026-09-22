using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VRFPSKit
{
    [RequireComponent(typeof(AudioSource))]
    public class FirearmCyclingActionFeedSound : MonoBehaviour
    {
        public AudioClip feedSound;
        
        private FirearmCyclingAction _action;

        private void OnFeed()
        {
            GetComponent<AudioSource>().PlayOneShot(feedSound);
        }

        private void Awake()
        {
            _action = GetComponentInParent<FirearmCyclingAction>();
            if (!_action)
            {
                Debug.LogError("FirearmCyclingActionFeedSound couldn't find FirearmCyclingAction component in parent"); 
                return;
            }
            
            _action.FeedRoundEvent += OnFeed;
        }
    }
}
