using NUnit.Framework;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class CharacterInstanceTests
    {
        [Test]
        public void CharacterInstance_InitializesStatsAndSkillsFromTemplate()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();

            // Add template stats
            template.BoundedStats.Add(
                new BoundedCharacterStat(100f, 75f, 0f, BoundedStatType.Health)
            );
            template.UnboundedStats.Add(new CharacterStat(12f, UnboundedStatType.Strength));

            // Add a simple skill template
            var skill = ScriptableObject.CreateInstance<Skill>();
            skill.SkillName = "TestSkill";
            template.Skills.Add(skill);

            var instance = CharacterInstance.Create(template);

            // Counts should match
            Assert.AreEqual(template.BoundedStats.Count, instance.RuntimeBoundedStats.Count);
            Assert.AreEqual(template.UnboundedStats.Count, instance.RuntimeUnboundedStats.Count);
            Assert.AreEqual(template.Skills.Count, instance.SkillInstances.Count);

            // Ensure stats were deep-copied (different instances)
            Assert.AreNotSame(template.BoundedStats[0], instance.RuntimeBoundedStats[0]);
            Assert.AreEqual(
                template.BoundedStats[0].StatType,
                instance.RuntimeBoundedStats[0].StatType
            );
            Assert.AreEqual(
                template.BoundedStats[0].Current,
                instance.RuntimeBoundedStats[0].Current
            );

            Assert.AreNotSame(template.UnboundedStats[0], instance.RuntimeUnboundedStats[0]);
            Assert.AreEqual(
                template.UnboundedStats[0].StatType,
                instance.RuntimeUnboundedStats[0].StatType
            );
        }
    }
}
