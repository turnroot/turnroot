using Assets.Turnroot.Characters;
using NUnit.Framework;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class RosterInstanceTests
    {
        [Test]
        public void InitializeFromRoster_CreatesInstancesForEachCharacter()
        {
            var roster = ScriptableObject.CreateInstance<Roster>();
            var charA = ScriptableObject.CreateInstance<CharacterData>();
            var charB = ScriptableObject.CreateInstance<CharacterData>();
            roster.characters = new CharacterData[] { charA, charB };

            var go = new GameObject("RosterTest");
            var ri = go.AddComponent<RosterInstance>();

            ri.InitializeFromRoster(roster);

            Assert.IsNotNull(ri.Instances);
            Assert.AreEqual(2, ri.Instances.Count);
            Assert.AreSame(charA, ri.Instances[0].CharacterTemplate);
            Assert.AreSame(charB, ri.Instances[1].CharacterTemplate);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(roster);
            Object.DestroyImmediate(charA);
            Object.DestroyImmediate(charB);
        }

        [Test]
        public void InitializeFromRoster_SkipsNullEntries()
        {
            var roster = ScriptableObject.CreateInstance<Roster>();
            var charA = ScriptableObject.CreateInstance<CharacterData>();
            roster.characters = new CharacterData[] { charA, null };

            var go = new GameObject("RosterTest");
            var ri = go.AddComponent<RosterInstance>();

            ri.InitializeFromRoster(roster);

            Assert.AreEqual(1, ri.Instances.Count);
            Assert.AreSame(charA, ri.Instances[0].CharacterTemplate);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(roster);
            Object.DestroyImmediate(charA);
        }
    }
}
