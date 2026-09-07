using System;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Conversations.Mermaid
{
    /// <summary>
    /// Executes immediate side-effect actions declared in Mermaid conversation graphs.
    /// Returns an <see cref="OperationResult"/> indicating whether the action succeeded.
    /// On success, the conversation controller will display a notification and wait for it
    /// to complete before continuing.
    /// </summary>
    public static class ConversationActionExecutor
    {
        public static OperationResult Execute(
            MermaidNode node,
            Conversation conversation,
            ConversationController controller
        )
        {
            if (node == null)
            {
                return OperationResult.Failure(
                    "ConversationActionExecutor: null node passed to Execute()."
                );
            }

            try
            {
                switch (node.ActionType?.ToUpperInvariant())
                {
                    case "GAINSUPPORT":
                    case "LOSESUPPORT":
                        return ExecuteSupportChange(node, conversation);
                    case "UNLOCKBATTLE":
                        return ExecuteUnlockBattle(node);
                    case "PLAYERGAINSITEM":
                    case "GAINSITEM":
                        return ExecutePlayerGainsItem(node);
                    case "PLAYERLOSESITEM":
                    case "LOSESITEM":
                        return ExecutePlayerLosesItem(node);
                    case "CHARACTERJOINSTEAM":
                    case "JOINTEAM":
                        return ExecuteCharacterJoinsTeam(node);
                    case "CHARACTERLEAVESTEAM":
                    case "LEAVETEAM":
                        return ExecuteCharacterLeavesTeam(node);
                    default:
                        return OperationResult.Failure(
                            $"ConversationActionExecutor: unknown action type '{node.ActionType}' in node '{node.Id}'."
                        );
                }
            }
            catch (Exception exception)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: action '{node.Id}' threw an exception.",
                    exception
                );
            }
        }

        private static OperationResult ExecuteSupportChange(
            MermaidNode node,
            Conversation conversation
        )
        {
            var action = ParseSupportChange(node, conversation);
            if (!action.HasValue)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: support action node '{node.Id}' could not be parsed."
                );
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
                return OperationResult.Failure(
                    $"ConversationActionExecutor: no character mapped for speaker '{action.Value.TargetSpeaker}' in node '{node.Id}'."
                );
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                return OperationResult.Failure("ConversationActionExecutor: could not find Brain.");
            }

            if (brain.charactersBrain == null)
            {
                return OperationResult.Failure(
                    "ConversationActionExecutor: could not find CharactersBrain for support change."
                );
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
                return OperationResult.Failure(
                    "ConversationActionExecutor: could not resolve avatar instance for support change."
                );
            }

            brain.charactersBrain.IncreaseSupport(avatar, person.Character, amount);

            $"ConversationActionExecutor: {action.Value.Operation} {action.Value.Magnitude} support with {person.Character.DisplayName} (amount: {amount}).".LogInfo();
            return OperationResult.Successful();
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

        private static OperationResult ExecuteUnlockBattle(MermaidNode node)
        {
            var battleSceneId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(battleSceneId))
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: unlock battle node '{node.Id}' has no battle scene id."
                );
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                return OperationResult.Failure(
                    "ConversationActionExecutor: could not find Brain for unlock battle."
                );
            }

            if (brain.sceneFlowBrain == null)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: could not find SceneFlowBrain for battle '{battleSceneId}'."
                );
            }

            brain.sceneFlowBrain.SetBattleUnlocked(battleSceneId, true);
            brain.PublishBattleUnlocked(battleSceneId);

            $"ConversationActionExecutor: unlocked battle '{battleSceneId}'.".LogInfo();
            return OperationResult.Successful();
        }

        private static OperationResult ExecutePlayerGainsItem(MermaidNode node)
        {
            var itemId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: gain item node '{node.Id}' has no item id."
                );
            }

            var itemTemplate = Resources.Load<ObjectItem>($"Items/{itemId}");
            if (itemTemplate == null)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: could not find item '{itemId}'."
                );
            }

            var brain = GetAndCacheBrain.GetBrain();
            var avatar = brain?.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar == null || avatar.InventoryInstance == null)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: could not find avatar inventory for item '{itemId}'."
                );
            }

            var itemInstance = new ObjectItemInstance(itemTemplate);
            var result = avatar.InventoryInstance.AddToInventory(itemInstance);
            if (!result.Success)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: failed to add '{itemId}' to avatar inventory: {result.ErrorMessage}"
                );
            }

            brain.PublishItemTransferred(itemInstance, avatar.InventoryInstance);
            $"ConversationActionExecutor: added item '{itemId}' to avatar inventory.".LogInfo();
            return OperationResult.Successful();
        }

        private static OperationResult ExecutePlayerLosesItem(MermaidNode node)
        {
            var itemId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: lose item node '{node.Id}' has no item id."
                );
            }

            var brain = GetAndCacheBrain.GetBrain();
            var avatar = brain?.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar == null || avatar.InventoryInstance == null)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: could not find avatar inventory for item '{itemId}'."
                );
            }

            var itemInstance = avatar.InventoryInstance.InventoryItems.FirstOrDefault(i =>
                string.Equals(i?.Template?.name, itemId, StringComparison.OrdinalIgnoreCase)
            );

            if (itemInstance == null)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: avatar has no '{itemId}' to remove."
                );
            }

            var result = avatar.InventoryInstance.RemoveFromInventory(itemInstance);
            if (!result.Success)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: failed to remove '{itemId}' from avatar inventory: {result.ErrorMessage}"
                );
            }

            brain.PublishItemDiscarded(itemInstance);
            $"ConversationActionExecutor: removed item '{itemId}' from avatar inventory.".LogInfo();
            return OperationResult.Successful();
        }

        private static OperationResult ExecuteCharacterJoinsTeam(MermaidNode node)
        {
            var characterId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: join team node '{node.Id}' has no character id."
                );
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                return OperationResult.Failure(
                    "ConversationActionExecutor: could not find Brain for join team."
                );
            }

            var characterTemplate =
                Resources.Load<CharacterData>(characterId)
                ?? brain
                    .charactersBrain?.GetAllActiveCharacters()
                    .FirstOrDefault(c =>
                        string.Equals(
                            c?.CharacterTemplate?.name,
                            characterId,
                            StringComparison.OrdinalIgnoreCase
                        )
                        || string.Equals(
                            c?.CharacterTemplate?.DisplayName,
                            characterId,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    ?.CharacterTemplate;

            if (characterTemplate == null)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: could not find character '{characterId}'."
                );
            }

            var existingInstance = brain.gamewideContextBrain?.FindInstanceByTemplate(
                characterTemplate
            );
            var instance = existingInstance ?? CharacterInstance.Create(characterTemplate);
            var roster = brain.gamewideContextBrain?.CreateOrRecallGamewidePersistentPlayerRoster();
            var rosterInstance =
                brain.gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
            if (roster == null || rosterInstance == null)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: could not access player roster to add '{characterId}'."
                );
            }

            roster.AddCharacter(characterTemplate);
            rosterInstance.AddRuntimePlacement(characterTemplate);
            rosterInstance.AddInstance(instance);
            brain.gamewideContextBrain?.PersistCharacter(instance, updateIndex: true);

            brain.PublishHubCharacterRecruitCompleted(instance);
            $"ConversationActionExecutor: character '{characterId}' joined the team.".LogInfo();
            return OperationResult.Successful();
        }

        private static OperationResult ExecuteCharacterLeavesTeam(MermaidNode node)
        {
            var characterId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: leave team node '{node.Id}' has no character id."
                );
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                return OperationResult.Failure(
                    "ConversationActionExecutor: could not find Brain for leave team."
                );
            }

            var characterTemplate =
                Resources.Load<CharacterData>(characterId)
                ?? brain
                    .charactersBrain?.GetAllActiveCharacters()
                    .FirstOrDefault(c =>
                        string.Equals(
                            c?.CharacterTemplate?.name,
                            characterId,
                            StringComparison.OrdinalIgnoreCase
                        )
                        || string.Equals(
                            c?.CharacterTemplate?.DisplayName,
                            characterId,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    ?.CharacterTemplate;

            if (characterTemplate == null)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: could not find character '{characterId}'."
                );
            }

            var roster = brain.gamewideContextBrain?.CreateOrRecallGamewidePersistentPlayerRoster();
            var rosterInstance =
                brain.gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
            if (roster == null || rosterInstance == null)
            {
                return OperationResult.Failure(
                    $"ConversationActionExecutor: could not access player roster to remove '{characterId}'."
                );
            }

            RemoveCharacterFromRoster(roster, characterTemplate);
            RemoveCharacterFromRosterInstance(rosterInstance, characterTemplate);

            var instance = brain.gamewideContextBrain?.FindInstanceByTemplate(characterTemplate);
            if (instance != null)
            {
                brain.gamewideContextBrain?.PersistCharacter(instance, updateIndex: true);
            }

            brain.PublishHubCharacterRecruitCompleted(instance);
            $"ConversationActionExecutor: character '{characterId}' left the team.".LogInfo();
            return OperationResult.Successful();
        }

        private static void RemoveCharacterFromRoster(
            PlayerTeamRoster roster,
            CharacterData character
        )
        {
            var placements = roster.characters?.ToList();
            if (placements == null)
            {
                return;
            }

            placements.RemoveAll(p =>
                p.CharacterData != null && p.CharacterData.Matches(character)
            );
            roster.characters = placements.ToArray();
        }

        private static void RemoveCharacterFromRosterInstance(
            PlayerTeamRosterInstance rosterInstance,
            CharacterData character
        )
        {
            var field = typeof(RosterInstance<PlayerTeamRoster>).GetField(
                "_instances",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (field == null)
            {
                return;
            }

            var instances = (System.Collections.Generic.List<CharacterInstance>)
                field.GetValue(rosterInstance);
            instances?.RemoveAll(i =>
                i?.CharacterTemplate != null && i.CharacterTemplate.Matches(character)
            );
            field.SetValue(rosterInstance, instances);
        }
    }
}
