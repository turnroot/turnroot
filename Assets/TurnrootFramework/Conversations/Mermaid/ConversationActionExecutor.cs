using System;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Conversations.Mermaid
{
    /// <summary>
    /// Executes immediate side-effect actions declared in Mermaid conversation graphs.
    /// </summary>
    public static class ConversationActionExecutor
    {
        public static void Execute(
            MermaidNode node,
            Conversation conversation,
            ConversationController controller
        )
        {
            if (node == null)
            {
                return;
            }

            switch (node.ActionType?.ToUpperInvariant())
            {
                case "GAINSUPPORT":
                case "LOSESUPPORT":
                    ExecuteSupportChange(node, conversation);
                    break;
                case "UNLOCKBATTLE":
                    ExecuteUnlockBattle(node);
                    break;
                default:
                    $"ConversationActionExecutor: unknown action type '{node.ActionType}' in node '{node.Id}'.".LogWarning();
                    break;
            }
        }

        private static void ExecuteSupportChange(MermaidNode node, Conversation conversation)
        {
            var action = ParseSupportChange(node, conversation);
            if (!action.HasValue)
            {
                return;
            }

            var person = conversation.People.FirstOrDefault(p =>
                string.Equals(
                    p.SpeakerName,
                    action.Value.TargetSpeaker,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if (person?.Character == null)
            {
                $"ConversationActionExecutor: no character mapped for speaker '{action.Value.TargetSpeaker}' in node '{node.Id}'.".LogWarning();
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                "ConversationActionExecutor: could not find Brain.".LogWarning();
                return;
            }

            var amount = action.Value.Magnitude switch
            {
                SupportChangeMagnitude.PlusPlus => 20f,
                SupportChangeMagnitude.Plus => 10f,
                SupportChangeMagnitude.MinusMinus => -20f,
                SupportChangeMagnitude.Minus => -10f,
                _ => action.Value.Operation == SupportChangeOperation.Gain ? 10f : -10f,
            };

            var avatar = brain.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar == null)
            {
                "ConversationActionExecutor: could not resolve avatar instance for support change.".LogWarning();
                return;
            }

            brain.charactersBrain?.IncreaseSupport(avatar, person.Character, amount);

            $"ConversationActionExecutor: {action.Value.Operation} {action.Value.Magnitude} support with {person.Character.DisplayName} (amount: {amount}).".LogInfo();
        }

        private static SupportChangeAction? ParseSupportChange(
            MermaidNode node,
            Conversation conversation
        )
        {
            var operation = node.ActionType?.ToUpperInvariant() switch
            {
                "GAINSUPPORT" => SupportChangeOperation.Gain,
                "LOSESUPPORT" => SupportChangeOperation.Lose,
                _ => (SupportChangeOperation?)null,
            };

            if (!operation.HasValue)
            {
                return null;
            }

            var strength =
                !string.IsNullOrEmpty(node.ActionStrength) ? ParseMagnitude(node.ActionStrength)
                : operation.Value == SupportChangeOperation.Gain ? SupportChangeMagnitude.Plus
                : SupportChangeMagnitude.Minus;

            var target = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(target))
            {
                $"ConversationActionExecutor: support action node '{node.Id}' has no target speaker.".LogWarning();
                return null;
            }

            return new SupportChangeAction(operation.Value, strength, target);
        }

        private static SupportChangeMagnitude ParseMagnitude(string strength)
        {
            return strength switch
            {
                "++" => SupportChangeMagnitude.PlusPlus,
                "+" => SupportChangeMagnitude.Plus,
                "--" => SupportChangeMagnitude.MinusMinus,
                "-" => SupportChangeMagnitude.Minus,
                _ => SupportChangeMagnitude.Plus,
            };
        }

        private static void ExecuteUnlockBattle(MermaidNode node)
        {
            var battleSceneId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(battleSceneId))
            {
                $"ConversationActionExecutor: unlock battle node '{node.Id}' has no battle scene id.".LogWarning();
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                "ConversationActionExecutor: could not find Brain for unlock battle.".LogWarning();
                return;
            }

            brain.sceneFlowBrain?.SetBattleUnlocked(battleSceneId, true);
            brain.PublishBattleUnlocked(battleSceneId);

            $"ConversationActionExecutor: unlocked battle '{battleSceneId}'.".LogInfo();
        }
    }
}
