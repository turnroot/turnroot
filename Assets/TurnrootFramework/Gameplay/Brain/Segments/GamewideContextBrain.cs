using System;
using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(LongTermMemory))]
    [RequireComponent(typeof(Brain))]
    public class GamewideContextBrain : BrainComponent
    {
        public Brain CentralBrain => _brain;

        public enum TamperPolicy
        {
            NotifyOnly,
            Reject,
            Replace,
        }

        [SerializeField]
        private TamperPolicy _tamperPolicy = TamperPolicy.Replace;
        public TamperPolicy Policy => _tamperPolicy;

        protected override void Awake()
        {
            _brain = GetComponent<Brain>();

            // Now subscribe to brain events
            Debug.Log(
                $"{GetType().Name} Awake - subscribing to brain events with priority {GetSubscriptionPriority()}."
            );
            SubscribeToBrainEvents();
        }

        // TODO: Store instances outside of battle

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents()
        {
            //  don't currently need to subscribe to anything
        }
    }
}
