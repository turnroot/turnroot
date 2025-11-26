using System.Reflection;
using NUnit.Framework;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Tests.Editor
{
    public class CharacterInstanceDeepCopyTests
    {
        [Test]
        public void BoundedStatChange_DoesNotAffectTemplate()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            // Ensure no default stats from OnEnable interfere with this test
            template.BoundedStats.Clear();
            template.UnboundedStats.Clear();
            var bounded = new BoundedCharacterStat(100f, 50f, 0f, BoundedStatType.Health);
            template.BoundedStats.Add(bounded);

            var instance = CharacterInstance.Create(template);

            Assert.AreEqual(50, template.BoundedStats[0].Get());
            Assert.AreEqual(50, instance.RuntimeBoundedStats[0].Get());

            // Mutate runtime stat
            instance.RuntimeBoundedStats[0].SetCurrent(10f);

            // Template should remain unchanged
            Assert.AreEqual(50, template.BoundedStats[0].Get());
            Assert.AreEqual(10, instance.RuntimeBoundedStats[0].Get());

            Object.DestroyImmediate(template);
        }

        [Test]
        public void UnboundedStatChange_DoesNotAffectTemplate()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            // Ensure no default stats from OnEnable interfere with this test
            template.BoundedStats.Clear();
            template.UnboundedStats.Clear();
            var stat = new CharacterStat(7f, UnboundedStatType.Strength);
            template.UnboundedStats.Add(stat);

            var instance = CharacterInstance.Create(template);

            Assert.AreEqual(7, template.UnboundedStats[0].Get());
            Assert.AreEqual(7, instance.RuntimeUnboundedStats[0].Get());

            instance.RuntimeUnboundedStats[0].SetCurrent(2f);

            Assert.AreEqual(7, template.UnboundedStats[0].Get());
            Assert.AreEqual(2, instance.RuntimeUnboundedStats[0].Get());

            Object.DestroyImmediate(template);
        }

        [Test]
        public void SkillInstanceChange_DoesNotAffectTemplateList()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            // Clear any defaults that might have been added in OnEnable
            template.Skills.Clear();
            var skill = ScriptableObject.CreateInstance<Skill>();
            template.Skills.Add(skill);

            var instance = CharacterInstance.Create(template);

            Assert.AreEqual(1, template.Skills.Count);
            Assert.AreEqual(1, instance.SkillInstances.Count);
            Assert.AreSame(skill, template.Skills[0]);
            Assert.AreSame(skill, instance.SkillInstances[0].SkillTemplate);

            // Mutate runtime wrapper
            instance.SkillInstances[0].SetEquipped(true);

            // Template list/reference should be unchanged
            Assert.AreEqual(1, template.Skills.Count);
            Assert.AreSame(skill, template.Skills[0]);

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(template);
        }

        [Test]
        public void InventoryCopy_DoesNotAffectTemplateStartingInventory()
        {
            var template = ScriptableObject.CreateInstance<CharacterData>();
            // Ensure no default stats/inventory from OnEnable
            template.BoundedStats.Clear();
            template.UnboundedStats.Clear();
            template.StartingInventory.Clear();
            var item = ScriptableObject.CreateInstance<ObjectItem>();

            // Create InventorySlot and set private field _item via reflection
            var slot = new CharacterData.InventorySlot();
            var slotType = typeof(CharacterData).GetNestedType(
                "InventorySlot",
                BindingFlags.Public | BindingFlags.NonPublic
            );
            var itemField = slotType.GetField(
                "_item",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            itemField.SetValue(slot, item);

            template.StartingInventory.Add(slot);

            var instance = CharacterInstance.Create(template);

            Assert.AreEqual(1, template.StartingInventory.Count);
            Assert.AreEqual(1, instance.InventoryInstance.InventoryItems.Count);
            Assert.AreSame(item, template.StartingInventory[0].Item);
            Assert.AreSame(item, instance.InventoryInstance.InventoryItems[0].Template);

            // Remove runtime item
            var runtimeItem = instance.InventoryInstance.InventoryItems[0];
            instance.InventoryInstance.RemoveFromInventory(runtimeItem);

            // Runtime inventory changed, template starting inventory remains
            Assert.AreEqual(1, template.StartingInventory.Count);
            Assert.AreEqual(0, instance.InventoryInstance.InventoryItems.Count);
            Assert.AreSame(item, template.StartingInventory[0].Item);

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(template);
        }
    }
}
