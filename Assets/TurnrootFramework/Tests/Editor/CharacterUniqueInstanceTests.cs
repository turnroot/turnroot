using NUnit.Framework;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class CharacterUniqueInstanceTests
    {
        [TearDown]
        public void TearDown()
        {
            UniqueInstanceRegistry.ClearAll();
        }

        [Test]
        public void Create_ReturnsSameInstance_ForUniqueTemplate()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            // CharacterData.IsUnique is a private serialized field; set with reflection in tests
            typeof(CharacterData)
                .GetField(
                    "_isUnique",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )
                .SetValue(template, true);

            var a = CharacterInstance.Create(template);
            var b = CharacterInstance.Create(template);

            Assert.AreSame(a, b);
        }

        [Test]
        public void Create_ReturnsDifferentInstances_ForNonUniqueTemplate()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            typeof(CharacterData)
                .GetField(
                    "_isUnique",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )
                .SetValue(template, false);

            var a = CharacterInstance.Create(template);
            var b = CharacterInstance.Create(template);

            Assert.AreNotSame(a, b);
        }

        [Test]
        public void Unregister_AllowsNewInstanceCreation()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            typeof(CharacterData)
                .GetField(
                    "_isUnique",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )
                .SetValue(template, true);

            var a = CharacterInstance.Create(template);
            var removed = UniqueInstanceRegistry.TryUnregister(template, a);
            Assert.IsTrue(removed);

            var b = CharacterInstance.Create(template);
            // After unregister, new instance should be created
            Assert.AreNotSame(a, b);
        }
    }
}
