using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities.SceneFlows;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    public partial class DynamicSceneFlow : MonoBehaviour
    {
        #region Conversation Management Reroutes

        public void StartConversation(string conversationId) =>
            conversationController?.PlayConversationById(conversationId);

        public void AdvanceConversation() => conversationController?.NextLayer();

        public void ChooseBranch(string targetNodeId) =>
            conversationController?.ChooseBranchTarget(targetNodeId);

        public void StartConversationFromNode(string conversationId, string nodeId) =>
            conversationController?.StartConversationById(conversationId, nodeId);

        #endregion
    }
}