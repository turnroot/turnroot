namespace Turnroot.Maps
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
            if (character?.CurrentClass?.ClassData == null)
            {
                return null;
            }

            var classData = character.CurrentClass.ClassData;
            var movementType = classData.Identity.MovementType;
            var movementStat = character.GetUnboundedStat(
                Characters.Stats.UnboundedStatType.Movement
            );

            return new PathfindingParameters
            {
                Graph = graph,
                Start = start,
                MovementBudget = movementStat,
                IsWalking = movementType == MovementType.Infantry,
                IsFlying = movementType == MovementType.Flying,
                IsRiding = movementType == MovementType.Riding,
                IsMagic = classData.Identity.IsMagic,
                IsArmored = movementType == MovementType.Armored,
                SameDirectionMultiplier = 0.95f,
                IncludeRange = false,
                MaxRange = 0,
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
                MaxRange = MaxRange,
            };
        }

        /// <summary>
        /// Validates that all required parameters are set.
        /// </summary>
        public bool IsValid() => Graph != null && Start != null && MovementBudget >= 0;
    }
}
