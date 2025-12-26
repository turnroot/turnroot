using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations
{
    public enum Direction
    {
        Center,
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public class Adjacency
    {
        public CharacterInstance Center { get; set; }
        public CharacterInstance TopLeft { get; set; }
        public CharacterInstance TopCenter { get; set; }
        public CharacterInstance TopRight { get; set; }
        public CharacterInstance CenterLeft { get; set; }
        public CharacterInstance CenterRight { get; set; }
        public CharacterInstance BottomLeft { get; set; }
        public CharacterInstance BottomCenter { get; set; }
        public CharacterInstance BottomRight { get; set; }

        public Adjacency(CharacterInstance center)
        {
            Center = center;
        }

        // Get unit at specific direction
        public CharacterInstance GetUnit(Direction direction)
        {
            return direction switch
            {
                Direction.Center => Center,
                Direction.TopLeft => TopLeft,
                Direction.TopCenter => TopCenter,
                Direction.TopRight => TopRight,
                Direction.CenterLeft => CenterLeft,
                Direction.CenterRight => CenterRight,
                Direction.BottomLeft => BottomLeft,
                Direction.BottomCenter => BottomCenter,
                Direction.BottomRight => BottomRight,
                _ => null,
            };
        }

        // Set unit at specific direction
        public void SetUnit(Direction direction, CharacterInstance unit)
        {
            switch (direction)
            {
                case Direction.Center:
                    Center = unit;
                    break;
                case Direction.TopLeft:
                    TopLeft = unit;
                    break;
                case Direction.TopCenter:
                    TopCenter = unit;
                    break;
                case Direction.TopRight:
                    TopRight = unit;
                    break;
                case Direction.CenterLeft:
                    CenterLeft = unit;
                    break;
                case Direction.CenterRight:
                    CenterRight = unit;
                    break;
                case Direction.BottomLeft:
                    BottomLeft = unit;
                    break;
                case Direction.BottomCenter:
                    BottomCenter = unit;
                    break;
                case Direction.BottomRight:
                    BottomRight = unit;
                    break;
            }
        }

        // Get all non-null adjacent units (excluding center)
        public IEnumerable<CharacterInstance> GetAllAdjacent()
        {
            if (TopLeft != null)
            {
                yield return TopLeft;
            }

            if (TopCenter != null)
            {
                yield return TopCenter;
            }

            if (TopRight != null)
            {
                yield return TopRight;
            }

            if (CenterLeft != null)
            {
                yield return CenterLeft;
            }

            if (CenterRight != null)
            {
                yield return CenterRight;
            }

            if (BottomLeft != null)
            {
                yield return BottomLeft;
            }

            if (BottomCenter != null)
            {
                yield return BottomCenter;
            }

            if (BottomRight != null)
            {
                yield return BottomRight;
            }
        }

        // Non-allocating variant that fills the provided list with adjacent units (excluding center)
        public void GetAllAdjacentNonAlloc(List<CharacterInstance> result)
        {
            result.Clear();
            if (TopLeft != null)
                result.Add(TopLeft);
            if (TopCenter != null)
                result.Add(TopCenter);
            if (TopRight != null)
                result.Add(TopRight);
            if (CenterLeft != null)
                result.Add(CenterLeft);
            if (CenterRight != null)
                result.Add(CenterRight);
            if (BottomLeft != null)
                result.Add(BottomLeft);
            if (BottomCenter != null)
                result.Add(BottomCenter);
            if (BottomRight != null)
                result.Add(BottomRight);
        }

        // get adjacent allies - non-allocating version that fills provided list
        public void GetAdjacentAlliesNonAlloc(BattleContext context, List<CharacterInstance> result)
        {
            result.Clear();
            if (context?.Participants?.Allies == null || context.Participants.Allies.Count == 0)
            {
                return;
            }

            // Build HashSet of ally IDs for O(1) lookup instead of O(n) Exists
            using var allyIds = PooledHashSet<string>.Get();
            foreach (var ally in context.Participants.Allies)
            {
                if (ally != null)
                {
                    allyIds.HashSet.Add(ally.Id);
                }
            }

            // Use non-alloc collection to avoid enumerator allocations
            var allAdjAllies = ListPool<CharacterInstance>.Get();
            GetAllAdjacentNonAlloc(allAdjAllies);
            foreach (var adjacent in allAdjAllies)
            {
                if (adjacent != null && allyIds.HashSet.Contains(adjacent.Id))
                {
                    result.Add(adjacent);
                }
            }
            ListPool<CharacterInstance>.Return(allAdjAllies);
        }

        // get adjacent enemies - non-allocating version that fills provided list
        public void GetAdjacentEnemiesNonAlloc(
            BattleContext context,
            List<CharacterInstance> result
        )
        {
            result.Clear();
            if (context?.Participants?.Targets == null || context.Participants.Targets.Count == 0)
            {
                return;
            }

            // Build HashSet of target IDs for O(1) lookup instead of O(n) Exists
            using var targetIds = PooledHashSet<string>.Get();
            foreach (var target in context.Participants.Targets)
            {
                if (target != null)
                {
                    targetIds.HashSet.Add(target.Id);
                }
            }

            // Use non-alloc collection to avoid enumerator allocations
            var allAdjTargets = ListPool<CharacterInstance>.Get();
            GetAllAdjacentNonAlloc(allAdjTargets);
            foreach (var adjacent in allAdjTargets)
            {
                if (adjacent != null && targetIds.HashSet.Contains(adjacent.Id))
                {
                    result.Add(adjacent);
                }
            }
            ListPool<CharacterInstance>.Return(allAdjTargets);
        }

        // get adjacent ally count - optimized O(n) instead of O(n²)
        public int GetAdjacentAllyCount(BattleContext context)
        {
            if (context?.Participants?.Allies == null || context.Participants.Allies.Count == 0)
            {
                return 0;
            }

            using var allyIds = PooledHashSet<string>.Get();
            foreach (var ally in context.Participants.Allies)
            {
                if (ally != null)
                {
                    allyIds.HashSet.Add(ally.Id);
                }
            }

            int count = 0;
            // Use non-alloc collection to avoid enumerator allocations
            var allAdjCount = ListPool<CharacterInstance>.Get();
            GetAllAdjacentNonAlloc(allAdjCount);
            foreach (var adjacent in allAdjCount)
            {
                if (adjacent != null && allyIds.HashSet.Contains(adjacent.Id))
                {
                    count++;
                }
            }
            ListPool<CharacterInstance>.Return(allAdjCount);
            return count;
        }

        // get adjacent enemy count - optimized O(n) instead of O(n²)
        public int GetAdjacentEnemyCount(BattleContext context)
        {
            if (context?.Participants?.Targets == null || context.Participants.Targets.Count == 0)
            {
                return 0;
            }

            using var targetIds = PooledHashSet<string>.Get();
            foreach (var target in context.Participants.Targets)
            {
                if (target != null)
                {
                    targetIds.HashSet.Add(target.Id);
                }
            }

            int count = 0;
            // Use non-alloc collection to avoid enumerator allocations
            var allAdjTargetCount = ListPool<CharacterInstance>.Get();
            GetAllAdjacentNonAlloc(allAdjTargetCount);
            foreach (var adjacent in allAdjTargetCount)
            {
                if (adjacent != null && targetIds.HashSet.Contains(adjacent.Id))
                {
                    count++;
                }
            }
            ListPool<CharacterInstance>.Return(allAdjTargetCount);
            return count;
        }
    }
}
