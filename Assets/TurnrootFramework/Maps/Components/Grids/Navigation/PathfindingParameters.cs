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
        public int MaxRange { get; set; } = 0;

        /// <summary>
        /// Creates parameters from a character instance and movement context.
        /// </summary>
        public static PathfindingParameters FromCharacter(
            Characters.CharacterInstance character,
            MapGrid graph,
            MapGridPoint start
        )
        {
            MovementType movementType;
            bool isMagic;
            try
            {
                if (character.CurrentClass?.ClassData == null)
                {
                    TurnrootLogger.Log(
                        "PathfindingParameters: Character class data is null",
                        TurnrootLogger.LogLevel.Warning
                    );
                    movementType = MovementType.Infantry;
                    TurnrootLogger.Log(
                        "PathfindingParameters: Defaulting movement type to Infantry",
                        TurnrootLogger.LogLevel.Warning
                    );
                    isMagic = false;
                }
                else
                {
                    var classData = character.CurrentClass.ClassData;
                    movementType = classData.Identity.MovementType;
                    isMagic = classData.Identity.IsMagic;
                }
                var movementStatObj = character.GetUnboundedStat(
                    Characters.Stats.UnboundedStatType.Movement
                );
                if (movementStatObj == null)
                {
                    TurnrootLogger.Log(
                        $"PathfindingParameters: Movement stat missing for character {character?.CharacterTemplate?.DisplayName ?? "<unknown>"}, using default value 5",
                        TurnrootLogger.LogLevel.Error
                    );
                    // Use a sensible default movement value instead of returning null
                    var defaultMovement = 5;
                    TurnrootLogger.Log(
                        $"PathfindingParameters: Defaulting movement to {defaultMovement} for character {character?.CharacterTemplate?.DisplayName ?? "<unknown>"}",
                        TurnrootLogger.LogLevel.Warning
                    );
                    return new PathfindingParameters
                    {
                        Graph = graph,
                        Start = start,
                        MovementBudget = defaultMovement,
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
                var movementStat = movementStatObj.CurrentInt;
                TurnrootLogger.Log(
                    $"PathfindingParameters: Movement stat for character {character.CharacterTemplate.DisplayName} is {movementStat}"
                );

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
            catch
            {
                TurnrootLogger.Log(
                    "PathfindingParameters: Failed to create from character",
                    TurnrootLogger.LogLevel.Error
                );
            }
            return null;
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
