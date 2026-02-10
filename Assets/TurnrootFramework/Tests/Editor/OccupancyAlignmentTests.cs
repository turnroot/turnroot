#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class OccupancyAlignmentTests
    {
        [Test]
        public void DebugVerifyOccupancyAlignment_DoesNotThrowOnEmptyContext()
        {
            var go = new GameObject("test-battle-context");
            var ctx =
                go.AddComponent<Turnroot.Gameplay.Combat.FundamentalComponents.Context.BattleContext>();

            // Should not throw when no participants and a default MapGrid
            Assert.DoesNotThrow(() => ctx.DebugVerifyOccupancyAlignment());

            Object.DestroyImmediate(go);
        }
    }
}
#endif
