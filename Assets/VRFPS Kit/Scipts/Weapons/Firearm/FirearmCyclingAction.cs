using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRFPSKit
{
    /// <summary>
    /// Represents a firearm action that operates by cycling (As opposed to say breaking open)
    /// </summary>
    [RequireComponent(typeof(Firearm))]
    public class FirearmCyclingAction : MonoBehaviour
    {
        [Range(0, 1)] public float inBattery01 = .05f;
        [Range(0, 1)] public float roundEjectAction01 = .6f;
        [Range(0, 1)] public float roundFeedAction01 = .85f;
        [Space]
        [Tooltip("Does the action lock back when the magazine is empty?")]
        public bool lockOnEmptyMag = true;
        [Tooltip("Turn this off for bolt action")] 
        public bool automaticCycling = true;
        public int roundsPerMinute;
        
        [Space]
        public float actionPosition01;
        public bool isLockedBack;
        

        public event Action FeedRoundEvent;
        public event Action LoadRoundEvent;

        private bool _readyToEject;
        private bool _roundFed;

        private Firearm _firearm;
        private XRGrabInteractable _interactable;
        private FirearmCyclingActionInteractable _actionInteractable;

        private void Update()
        {
            UpdateAction();
        }

        public void UpdateAction()
        {
            if (isLockedBack)
                actionPosition01 = 1;

            _firearm.isActionOpen = (actionPosition01 > inBattery01);
            
            //Try Recock hammer
            if(actionPosition01 > .3f)
                _firearm.isHammerCocked = true;
            
            HandleChamber();
            TryEmptyLockAction();
            ApplySpringMovement();
        }
        
        public void HandleChamber()
        {
            //return if weapon is not held
            if (!_interactable.isSelected)
                return;
            
            //Cartridge Ejection
            if (actionPosition01 < roundEjectAction01) _readyToEject = true;
            if (actionPosition01 > roundEjectAction01 && _readyToEject)
            {
                _readyToEject = false;
                _firearm.TryEjectChamber();
            }

            //Feed Cartridge (Action has been moved back far enough to feed a cartridge)
            if (actionPosition01 > roundFeedAction01 && actionPosition01 > roundEjectAction01 && //Action moved past feed & eject point
                _firearm.magazine && !_firearm.magazine.GetTopCartridge().IsNull() && //There is another round in the magazine
                _firearm.chamberCartridge.IsNull() && //Chamber is empty
                !_roundFed) //Round has not been fed yet
            {
                _roundFed = true;
                FeedRoundEvent?.Invoke();
            }
            
            //Chamber cartridge (Begin moving round in to chamber)
            if (actionPosition01 < roundFeedAction01 && _roundFed)
            {
                LoadRoundEvent?.Invoke();
                _firearm.TryLoadChamber();
                _roundFed = false;
            }
        }
        
        private void TryEmptyLockAction()
        {
            if (!lockOnEmptyMag)
                return;
            //Don't lock back twice
            if (isLockedBack)
                return;
            //return if weapon is not held
            if (!_interactable.isSelected)
                return;
            //Wait until action is all the way back
            if (actionPosition01 < 0.9f)
                return;
            //Don't lock back if magazine is missing
            if (_firearm.magazine == null)
                return;
            //Lock back when magazine is empty
            if (!_firearm.magazine.IsEmpty())
                return;
            
            isLockedBack = true;

            //Detach hand from potential action interactable
            if (_actionInteractable)
                _actionInteractable.ForceDetachSelectors();
        }

        public void Shoot(Cartridge cartridge)
        {
            if (!automaticCycling)
                return;
            
            actionPosition01 = 1;
            
            if (_actionInteractable)
                _actionInteractable.ForceDetachSelectors();
            
            //Make sure all action updates are performed right after shot
            //TODO might not be needed anymore
            UpdateAction();
        }

        public void TryUnlockAction()
        {
            if (!isLockedBack)
                return;
            
            isLockedBack = false;
            
            //Move action forward far enough that action won't lock again
            actionPosition01 = 0.7f;
        }

        private void ApplySpringMovement()
        {
            if (isLockedBack)
                return;
            //If has action interactable, and it is held, don't apply spring movement
            if (_actionInteractable && _actionInteractable.actionInteractable.isSelected)
                return;

            //Fire rate is determined by how quick action goes back in to battery again after firing
            float roundsPerSecond = roundsPerMinute / 60f;
            actionPosition01 = Mathf.Clamp01(actionPosition01 - (roundsPerSecond * Time.deltaTime));
        }
        
        private void Awake()
        {
            _firearm = GetComponent<Firearm>();
            _interactable = GetComponent<XRGrabInteractable>();
            _actionInteractable = GetComponentInChildren<FirearmCyclingActionInteractable>();

            _firearm.ShootEvent += Shoot;
        }
    }
}