using System;
using Turnroot.Characters;
using Turnroot.Conversations;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Assets.Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(Brain))]
    /// <summary>
    /// Manages conversations and conversation progressions within the game's brain system.
    /// </summary>
    public class ConversationalBrain : MonoBehaviour
    {
        [SerializeField]
        private Brain _brain;

        public void Awake()
        {
            Debug.Log("ConversationalBrain Awake called.");
            if (_brain == null)
                _brain = GetComponent<Brain>();

            if (_brain == null)
            {
                Debug.LogError("ConversationalBrain requires a Brain");
                Debug.Break();
            }
        }
    }
}
