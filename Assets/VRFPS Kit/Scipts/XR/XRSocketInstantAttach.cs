using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace VRFPSKit
{
    [RequireComponent(typeof(XRSocketInteractor))]
    public class XRSocketInstantAttach : MonoBehaviour
    {
        private const float ReattachCooldownDelay = 1f;
        
        private float _lastDetachTime;
        private IXRSelectInteractable _lastInteractable;
        private XRSocketInteractor _socket;

        private void Update()
        {
            foreach (var hoveredInteractable in _socket.interactablesHovered)
                TryInstantAttachInteractable((IXRSelectInteractable)hoveredInteractable);
        }

        private void TryInstantAttachInteractable(IXRSelectInteractable interactable)
        {
            //Verify that interactor can select before transfering interactable from previous interactor to this one
            //Using interactionManager.CanSelect() doesn't work since potentially new magazine
            //already is selected by player hand. In this case we do not care if magazine already is selected
            if (!DoFiltersAllowSelect(_socket, interactable, _socket.interactionManager.selectFilters)) return;
            
            //Don't allow socket to hold more than 1
            if (_socket.interactablesSelected.Count > 0) return;
            
            //Cooldown has to be implemented twice for some reason
            if (!ReattachCooldownFilter(_socket, interactable)) return;

            //Force deselect from current interactor (e.g. hand) to allow socket to take over
            if (interactable.isSelected)
            {
                var currentInteractors = new List<IXRSelectInteractor>(interactable.interactorsSelecting);
                foreach (var interactor in currentInteractors)
                {
                    _socket.interactionManager.SelectExit(interactor, interactable);
                }
            }

            //Instantly transfer magazine from hand to socket when in range
            _socket.interactionManager.SelectEnter(_socket, interactable);
        }
        
        private bool ReattachCooldownFilter(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            //if new interactable is trying to attach again, implement selection cooldown
            if(interactable == _lastInteractable) 
                return (Time.time - _lastDetachTime > ReattachCooldownDelay); //Selection cooldown

            return true;
        }

        private void OnSelectExit(SelectExitEventArgs args)
        {
            _lastDetachTime = Time.time;
            _lastInteractable = args.interactableObject;
        }
        
        // Start is called before the first frame update
        void Awake()
        {
            _socket = GetComponent<XRSocketInteractor>();
            
            _socket.selectExited.AddListener(OnSelectExit);
            
            _socket.selectFilters.Add(new XRSelectFilterDelegate(ReattachCooldownFilter));
        }
        
        /// <summary>
        /// Essentially a clone of XRInteractionManager.ProcessFilters() since it is protected and can't be accessed from here.
        /// We still need a way to see if an interaction would be valid before transferring an interactable though.
        /// </summary>
        /// <param name="interactor"></param>
        /// <param name="interactable"></param>
        /// <param name="globalFilters"></param>
        /// <returns></returns>
        private static bool DoFiltersAllowSelect(IXRSelectInteractor interactor, IXRSelectInteractable interactable, IXRFilterList<IXRSelectFilter> globalFilters)
        {
            if (!CheckFilters(interactor, interactable, globalFilters)) return false;

            // Cast to base types to access selectFilters, as they are not on the interface
            if (interactor is XRBaseInteractor baseInteractor)
            {
                if (!CheckFilters(interactor, interactable, baseInteractor.selectFilters)) return false;
            }

            if (interactable is XRBaseInteractable baseInteractable)
            {
                if (!CheckFilters(interactor, interactable, baseInteractable.selectFilters)) return false;
            }

            return true;
        }

        private static bool CheckFilters(IXRSelectInteractor interactor, IXRSelectInteractable interactable, IXRFilterList<IXRSelectFilter> filters)
        {
            if (filters == null || filters.count == 0) return true;

            //Convert filters to a list that we can iterate
            List<IXRSelectFilter> filterList = new List<IXRSelectFilter>();
            filters.GetAll(filterList);

            foreach (var filter in filterList)
                if(!filter.Process(interactor, interactable)) return false;

            return true;
        }
    }
}
