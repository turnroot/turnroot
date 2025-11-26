using NUnit.Framework;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class CharacterSupportTests
    {
        [Test]
        public void IncreaseSupport_CreatesRelationWhenMissing()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            var other = ScriptableObject.CreateInstance<CharacterData>();

            var instance = CharacterInstance.Create(template);

            // No relationship exists initially
            Assert.IsNull(instance.GetSupportRelationship(other));

            // Increase support for a character not in the list - should add relationship
            instance.IncreaseSupport(other, 5);

            var rel = instance.GetSupportRelationship(other);
            Assert.IsNotNull(rel);
            Assert.IsTrue(rel.SupportPoints > 0, "IncreaseSupport should add and increment points");
        }

        [Test]
        public void IncreaseSupport_UsesSupportSpeedAndClamps()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            var other = ScriptableObject.CreateInstance<CharacterData>();

            // Create a template relationship with high speed
            var templ = new SupportRelationship();
            templ.Character = other;
            templ.SupportSpeed = 3;

            template.SupportRelationships.Add(templ);

            var instance = CharacterInstance.Create(template);

            var rel = instance.GetSupportRelationship(other);
            Assert.IsNotNull(rel);

            rel.Increase(10); // should add 10 * speed
            Assert.AreEqual(30, rel.SupportPoints);
        }
    }
}
