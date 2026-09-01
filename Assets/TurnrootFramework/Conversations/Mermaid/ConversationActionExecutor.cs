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
                case "PLAYERGAINSITEM":
                case "GAINSITEM":
                    ExecutePlayerGainsItem(node);
                    break;
                case "PLAYERLOSESITEM":
                case "LOSESITEM":
                    ExecutePlayerLosesItem(node);
                    break;
                case "CHARACTERJOINSTEAM":
                case "JOINTEAM":
                    ExecuteCharacterJoinsTeam(node);
                    break;
                case "CHARACTERLEAVESTEAM":
                case "LEAVETEAM":
                    ExecuteCharacterLeavesTeam(node);
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

        private static void ExecutePlayerGainsItem(MermaidNode node)
        {
            var itemId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                $"ConversationActionExecutor: gain item node '{node.Id}' has no item id.".LogWarning();
                return;
            }

            var itemTemplate = Resources.Load<ObjectItem>($"Items/{itemId}");
            if (itemTemplate == null)
            {
                $"ConversationActionExecutor: could not find item '{itemId}'.".LogWarning();
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            var avatar = brain?.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar == null || avatar.InventoryInstance == null)
            {
                $"ConversationActionExecutor: could not find avatar inventory for item '{itemId}'.".LogWarning();
                return;
            }

            var itemInstance = new ObjectItemInstance(itemTemplate);
            var result = avatar.InventoryInstance.AddToInventory(itemInstance);
            if (!result.Success)
            {
                $"ConversationActionExecutor: failed to add '{itemId}' to avatar inventory: {result.ErrorMessage}".LogWarning();
                return;
            }

            brain.PublishItemTransferred(itemInstance, avatar.InventoryInstance);
            $"ConversationActionExecutor: added item '{itemId}' to avatar inventory.".LogInfo();
        }

        private static void ExecutePlayerLosesItem(MermaidNode node)
        {
            var itemId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                $"ConversationActionExecutor: lose item node '{node.Id}' has no item id.".LogWarning();
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            var avatar = brain?.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar == null || avatar.InventoryInstance == null)
            {
                $"ConversationActionExecutor: could not find avatar inventory for item '{itemId}'.".LogWarning();
                return;
            }

            var itemInstance = avatar.InventoryInstance.InventoryItems.FirstOrDefault(i =>
                string.Equals(i?.Template?.name, itemId, StringComparison.OrdinalIgnoreCase)
            );

            if (itemInstance == null)
            {
                $"ConversationActionExecutor: avatar has no '{itemId}' to remove.".LogWarning();
                return;
            }

            var result = avatar.InventoryInstance.RemoveFromInventory(itemInstance);
            if (!result.Success)
            {
                $"ConversationActionExecutor: failed to remove '{itemId}' from avatar inventory: {result.ErrorMessage}".LogWarning();
                return;
            }

            brain.PublishItemDiscarded(itemInstance);
            $"ConversationActionExecutor: removed item '{itemId}' from avatar inventory.".LogInfo();
        }

        private static void ExecuteCharacterJoinsTeam(MermaidNode node)
        {
            var characterId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                $"ConversationActionExecutor: join team node '{node.Id}' has no character id.".LogWarning();
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                $"ConversationActionExecutor: could not find Brain for join team.".LogWarning();
                return;
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
                $"ConversationActionExecutor: could not find character '{characterId}'.".LogWarning();
                return;
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
                $"ConversationActionExecutor: could not access player roster to add '{characterId}'.".LogWarning();
                return;
            }

            roster.AddCharacter(characterTemplate);
            rosterInstance.AddRuntimePlacement(characterTemplate);
            rosterInstance.AddInstance(instance);
            brain.gamewideContextBrain?.PersistCharacter(instance, updateIndex: true);

            brain.PublishHubCharacterRecruitCompleted(instance);
            $"ConversationActionExecutor: character '{characterId}' joined the team.".LogInfo();
        }

        private static void ExecuteCharacterLeavesTeam(MermaidNode node)
        {
            var characterId = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(characterId))
            {
                $"ConversationActionExecutor: leave team node '{node.Id}' has no character id.".LogWarning();
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                $"ConversationActionExecutor: could not find Brain for leave team.".LogWarning();
                return;
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
                $"ConversationActionExecutor: could not find character '{characterId}'.".LogWarning();
                return;
            }

            var roster = brain.gamewideContextBrain?.CreateOrRecallGamewidePersistentPlayerRoster();
            var rosterInstance =
                brain.gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
            if (roster == null || rosterInstance == null)
            {
                $"ConversationActionExecutor: could not access player roster to remove '{characterId}'.".LogWarning();
                return;
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
