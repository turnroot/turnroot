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

        /// <summary>
        /// Creates parameters from a character instance and movement context.
        /// </summary>
        public static PathfindingParameters FromCharacter(
            Characters.CharacterInstance character,
            MapGrid graph,
            MapGridPoint start
        )
        {
            var classData = character?.CurrentClass?.ClassData;
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
                TurnrootLogger.Log(
                    $"PathfindingParameters: Missing required parameter(s): {string.Join(", ", missingArgs)}",
                    TurnrootLogger.LogLevel.Error
                );
                return null;
            }

            var movementType = classData.Identity.MovementType;
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
                MaxRange = 0, // TODO: Get weapon range
            };
        }

        /// <summary>
        /// Creates parameters including weapon range for attack tile calculation.
        /// </summary>
        public static PathfindingParameters FromCharacterWithRange(
            Characters.CharacterInstance character,
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

        /// <summary>
        /// Creates a copy of these parameters with modified values.
        /// </summary>
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

        /// <summary>
        /// Validates that all required parameters are set.
        /// </summary>
        public bool IsValid() => Graph != null && Start != null && MovementBudget >= 0;
    }
}
