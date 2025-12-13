using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

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

        // get adjacent allies - non-allocating version that fills provided list
        public void GetAdjacentAlliesNonAlloc(
            Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext context,
            List<CharacterInstance> result
        )
        {
            result.Clear();
            if (context?.Allies == null || context.Allies.Count == 0)
            {
                return;
            }

            // Build HashSet of ally IDs for O(1) lookup instead of O(n) Exists
            using var allyIds = PooledHashSet<string>.Get();
            foreach (var ally in context.Allies)
            {
                if (ally != null)
                {
                    allyIds.HashSet.Add(ally.Id);
                }
            }

            foreach (var adjacent in GetAllAdjacent())
            {
                if (adjacent != null && allyIds.HashSet.Contains(adjacent.Id))
                {
                    result.Add(adjacent);
                }
            }
        }

        // get adjacent enemies - non-allocating version that fills provided list
        public void GetAdjacentEnemiesNonAlloc(
            Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext context,
            List<CharacterInstance> result
        )
        {
            result.Clear();
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                return;
            }

            // Build HashSet of target IDs for O(1) lookup instead of O(n) Exists
            using var targetIds = PooledHashSet<string>.Get();
            foreach (var target in context.Targets)
            {
                if (target != null)
                {
                    targetIds.HashSet.Add(target.Id);
                }
            }

            foreach (var adjacent in GetAllAdjacent())
            {
                if (adjacent != null && targetIds.HashSet.Contains(adjacent.Id))
                {
                    result.Add(adjacent);
                }
            }
        }

        // get adjacent ally count - optimized O(n) instead of O(n²)
        public int GetAdjacentAllyCount(
            Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext context
        )
        {
            if (context?.Allies == null || context.Allies.Count == 0)
            {
                return 0;
            }

            using var allyIds = PooledHashSet<string>.Get();
            foreach (var ally in context.Allies)
            {
                if (ally != null)
                {
                    allyIds.HashSet.Add(ally.Id);
                }
            }

            int count = 0;
            foreach (var adjacent in GetAllAdjacent())
            {
                if (adjacent != null && allyIds.HashSet.Contains(adjacent.Id))
                {
                    count++;
                }
            }
            return count;
        }

        // get adjacent enemy count - optimized O(n) instead of O(n²)
        public int GetAdjacentEnemyCount(
            Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext context
        )
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                return 0;
            }

            using var targetIds = PooledHashSet<string>.Get();
            foreach (var target in context.Targets)
            {
                if (target != null)
                {
                    targetIds.HashSet.Add(target.Id);
                }
            }

            int count = 0;
            foreach (var adjacent in GetAllAdjacent())
            {
                if (adjacent != null && targetIds.HashSet.Contains(adjacent.Id))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
