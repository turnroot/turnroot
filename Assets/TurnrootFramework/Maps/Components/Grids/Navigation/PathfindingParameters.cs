using Turnroot.Characters;
using Turnroot.GameSettings;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// Parameter object for pathfinding queries to reduce long parameter lists.
    /// Encapsulates movement characteristics and constraints for A* pathfinding.
    /// </summary>
    public class PathfindingParameters
    {
        public MapGrid Graph { get; set; }
        public MapGridPoint Start { get; set; }
        public int MovementBudget { get; set; }
        public bool IsWalking { get; set; } = true;
        public bool IsFlying { get; set; } = false;
        public bool IsRiding { get; set; } = false;
        public bool IsMagic { get; set; } = false;
        public bool IsArmored { get; set; } = false;
        public float SameDirectionMultiplier { get; set; } = 0.95f;
        public bool IncludeRange { get; set; } = false;
        public bool IncludeHealRange { get; set; } = false;
        public int MaxRange { get; set; } = 1;

        public static PathfindingParameters FromCharacter(
            CharacterInstance character,
            MapGrid graph,
            MapGridPoint start
        )
        {
            var classData = character?.CurrentClass?.ClassData;
            // If classData is missing on the character (e.g., deserialized instance lost class),
            // attempt to assign the template starting class or the global default and persist it.
            if (classData == null && character != null)
            {
                var classToApply =
                    character.CharacterTemplate?.GetPreferredStartingClass()
                    ?? GameplayGeneralSettings.Instance?.GetDefaultStartingClass();
                if (classToApply != null)
                {
                    var res = character.ChangeClass(classToApply, applyClassChangeBonuses: false);
                    if (res.Success)
                    {
                        // Persist the updated character so future recalls include the class
                        var brain = UnityEngine.Object.FindFirstObjectByType<Brain.Brain>();
                        brain?.gamewideContextBrain?.PersistCharacter(
                            character,
                            updateIndex: false
                        );
                    }
                    else
                    {
                        $"PathfindingParameters: Failed to assign default class to {character.Id}: {res.ErrorMessage}".LogWarning();
                    }

                    classData = character?.CurrentClass?.ClassData;
                }
            }

            var movementStatObj = character?.GetUnboundedStat(
                Characters.Stats.UnboundedStatType.Movement
            );

            if (
                !ValidationHelper.ValidateNotNull(
                    "PathfindingParameters.FromCharacter",
                    out var missingArgs,
                    (character, nameof(character)),
                    (graph, nameof(graph)),
                    (start, nameof(start)),
                    (classData, nameof(classData)),
                    (movementStatObj, nameof(movementStatObj))
                )
            )
            {
                $"PathfindingParameters: Missing required parameter(s): {string.Join(", ", missingArgs)}".LogError();
                return null;
            }

            var movementType = character.GetEffectiveMovementType();
            var isMagic = classData.Identity.IsMagic;

            var movementStat = movementStatObj.CurrentInt;
            return new PathfindingParameters
            {
                Graph = graph,
                Start = start,
                MovementBudget = movementStat,
                IsWalking = movementType == MovementType.Infantry,
                IsFlying = movementType == MovementType.Flying,
                IsRiding = movementType == MovementType.Riding,
                IsMagic = isMagic,
                IsArmored = movementType == MovementType.Armored,
                SameDirectionMultiplier = 0.95f,
                IncludeRange = false,
                IncludeHealRange = false,
                MaxRange = 0,
            };
        }

        public static PathfindingParameters FromCharacterWithRange(
            CharacterInstance character,
            MapGrid graph,
            MapGridPoint start
        )
        {
            var parameters = FromCharacter(character, graph, start);
            if (parameters == null)
            {
                return null;
            }

            parameters.IncludeRange = true;
            parameters.MaxRange = character.GetMaxRange();
            return parameters;
        }

        public PathfindingParameters Clone()
        {
            return new PathfindingParameters
            {
                Graph = Graph,
                Start = Start,
                MovementBudget = MovementBudget,
                IsWalking = IsWalking,
                IsFlying = IsFlying,
                IsRiding = IsRiding,
                IsMagic = IsMagic,
                IsArmored = IsArmored,
                SameDirectionMultiplier = SameDirectionMultiplier,
                IncludeRange = IncludeRange,
                IncludeHealRange = IncludeHealRange,
                MaxRange = MaxRange,
            };
        }

        public bool IsValid() => Graph != null && Start != null && MovementBudget >= 0;
    }
}
