/*
The conversational brain need to manage conversations and conversation progressions.
Conversations events should propogate upwards through the brain.
So, a Conversation Layer Event -> Brain Event, which is subscribed to by the other things that need it.
Or, a Conversation Start Event -> Brain Event, etc.
This maintains the bowtie structure of the brain- many things come in to the central brain, which then fans back out to the various systems.
The conversational brain also needs to keep track of things like support conversations and support progressions during conversations.
If support points reach a support conversation point during battle, the conversational brain should be alerted through subscription-
then, the brain will invoke an event, and listeners such as the UI (adding a Support option to character action menu) will respond
through their subscription to the brain. This way, the conversational brain remains the central hub for conversation-related events.
The character action menu doesn't need to know about support points or conversations- it just listens to the brain for relevant events.
*/

using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages conversations and conversation progressions within the game's brain system.
    /// </summary>
    public class ConversationalBrain : MonoBehaviour
    {
        private LongTermMemory _longTermMemory;

        public void Awake()
        {
            Debug.Log("ConversationalBrain Awake called.");
            _longTermMemory =
                gameObject.GetComponent<LongTermMemory>()
                ?? gameObject.AddComponent<LongTermMemory>();
        }
    }
}
