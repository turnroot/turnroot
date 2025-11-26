using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Assets.Turnroot.Gameplay.Brain.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Turnroot.Characters;
using Turnroot.Serialization;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(Brain))]
    [RequireComponent(typeof(LongTermMemory))]
    /// <summary>
    /// Manages gamewide context within the game's brain system
    /// What this actually means is- the GWCB holds all the instances, which need a central place to live,
    /// and Data -> Instance needs to happen in one local place.
    /// This Brain can convert, say, CharacterData to CharacterInstance
    /// and hold those instances for the entire game as needed.
    /// Since LongTermMemory wants strings, this Brain encodes and decodes instances <-> strings.
    /// </summary>
    public class GamewideContextBrain : MonoBehaviour
    {
        public enum TamperPolicy
        {
            NotifyOnly = 0,
            Reject = 1,
            Replace = 2,
        }

        [Header("Tamper Detection")]
        [Tooltip(
            "Policy that controls what happens when an encoded payload fails the integrity check."
        )]
        [SerializeField]
        private TamperPolicy tamperPolicy = TamperPolicy.Replace;

        // runtime accessor so tools / editor helpers can change the active policy
        public TamperPolicy Policy
        {
            get => tamperPolicy;
            set => tamperPolicy = value;
        }

        /* ----------------------------- Memory helpers ----------------------------- */
        public string DesignateInstanceType<T>()
        {
            return GamewideContextBrainHelpers.DesignateInstanceType<T>();
        }

        /// <summary>
        /// Encodes an instance into a single opaque Base64 string using Newtonsoft.Json.
        /// The wrapper contains type information and a payload; the whole wrapper is
        /// serialized to JSON and then Base64 encoded
        /// </summary>
        public string EncodeInstanceToString<T>(T instance)
        {
            return GamewideContextBrainHelpers.EncodeInstanceToString(this, instance);
        }

        /// <summary>
        /// Decodes an instance from the opaque Base64 wrapper string produced by EncodeInstanceToString.
        /// Uses Newtonsoft.Json and registered converters to attempt hydration of UnityEngine.Object references.
        /// </summary>
        public T DecodeInstanceFromString<T>(string encodedString)
        {
            return GamewideContextBrainHelpers.DecodeInstanceFromString<T>(this, encodedString);
        }
    }
}
