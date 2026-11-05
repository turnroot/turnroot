#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.GameSettings;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Editor
{
    [CustomEditor(typeof(ObjectItem))]
    public class ObjectItemEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, bool> _foldouts = new();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("_name");
            var id = serializedObject.FindProperty("_id");
            if (id != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(id);
                }
            }
            DrawProperty("_flavorText");

            EditorGUILayout.Space();
            DrawProperty("Subtype");
            string subtype = FindProperty("Subtype")?.FindPropertyRelative("_value")?.stringValue;
            bool isWeapon = subtype == ObjectSubtype.Weapon;
            bool isMagic = subtype == ObjectSubtype.Magic;
            bool isEquipable = subtype == ObjectSubtype.Equipable;
            bool isGift = subtype == ObjectSubtype.Gift;
            bool isLostItem = subtype == ObjectSubtype.LostItem;
            bool isConsumableWithEffect = subtype == ObjectSubtype.ConsumableWithEffect;
            bool isWeaponOrMagic = isWeapon || isMagic;

            if (isWeaponOrMagic)
            {
                DrawProperty("WeaponType");
                DrawProperty("IsUnequippable");
            }
            if (isMagic)
            {
                DrawProperty("TeamSupportMagic");
            }
            if (isEquipable)
            {
                DrawProperty("EquipableType");
            }

            EditorGUILayout.Space();
            DrawSection("Lost Items", "Lost Items", () =>
            {
                DrawProperty("BelongsTo");
            }, isLostItem);

            DrawSection("Pricing", "Pricing", () =>
            {
                DrawProperties("BasePrice", "Sellable", "Buyable", "SellPriceDeductedPerUse");
            });

            DrawSection("Repair", "Repair", () =>
            {
                DrawProperties("Repairable", "Forgeable");
                if (serializedObject.FindProperty("Repairable")?.boolValue == true)
                {
                    DrawProperties("RepairPricePerUse", "RepairNeedsItems");
                    if (serializedObject.FindProperty("RepairNeedsItems")?.boolValue == true)
                    {
                        DrawProperties("RepairItem", "OneRepairItemCoversFullRepair");
                        if (
                            serializedObject.FindProperty("OneRepairItemCoversFullRepair")?.boolValue
                            != true
                        )
                        {
                            DrawProperty("RepairItemAmountPerUse");
                        }
                    }
                }
                if (serializedObject.FindProperty("Forgeable")?.boolValue == true)
                {
                    DrawProperty("ForgeOptions");
                }
            }, isWeaponOrMagic);

            DrawSection("Gift", "Gift", () =>
            {
                DrawProperties("GiftRank", "UnitsLove", "UnitsHate");
            }, isGift);

            bool isStaff = isEquipable
                && serializedObject.FindProperty("EquipableType")?.enumValueIndex
                    == (int)EquipableObjectType.Staff;
            DrawSection("Range", "Range", () =>
            {
                DrawProperties("LowerRange", "UpperRange", "_rangeAdjustedByStat");
                if (serializedObject.FindProperty("_rangeAdjustedByStat")?.boolValue == true)
                {
                    DrawProperties("_rangeAdjustedByStatName", "_rangeAdjustedByStatAmount");
                }
            }, isWeaponOrMagic || isStaff);

            DrawSection("Durability", "Durability", () =>
            {
                if (isWeaponOrMagic)
                {
                    DrawProperties("Durability", "MaxUses", "_replenishUsesAfterBattle");
                    if (serializedObject.FindProperty("_replenishUsesAfterBattle")?.boolValue == true)
                    {
                        DrawProperty("_replenishUsesAfterBattleAmount");
                    }
                }
                else
                {
                    DrawProperty("MaxUses");
                }
            }, isWeaponOrMagic || isConsumableWithEffect);

            bool isTeamSupportMagic =
                isMagic && serializedObject.FindProperty("TeamSupportMagic")?.boolValue == true;
            DrawSection("Support", "Support", () =>
            {
                DrawProperty("SupportHealing");
                if (serializedObject.FindProperty("SupportHealing")?.boolValue == true)
                {
                    DrawProperty("HealingAmountPercent");
                }
                DrawProperty("SupportSkill");
            }, isTeamSupportMagic);

            DrawSection("Combat", "Combat", () =>
            {
                DrawProperties(
                    "SpeciesEffectiveAgainst",
                    "WeaponTypesEffectiveAgainst",
                    "EffectiveAgainstFlying",
                    "EffectiveAgainstArmored",
                    "EffectiveAgainstRiding",
                    "_weaponEffect",
                    "StatBonuses",
                    "Might",
                    "Hit",
                    "Critical",
                    "Weight"
                );
            }, isWeaponOrMagic && !isTeamSupportMagic);

            DrawSection("Aptitude", "Aptitude", () =>
            {
                DrawProperty("MinWeaponTypeAptitude");
            }, isWeaponOrMagic && !isTeamSupportMagic);

            DrawSection("Consumable Effects", "Consumable Effects", () =>
            {
                DrawProperties(
                    "StatEffectIsPermanent",
                    "RemovesDebuff",
                    "AddsBuff",
                    "StatModifiersAdded"
                );
                if (serializedObject.FindProperty("RemovesDebuff")?.boolValue == true)
                {
                    DrawProperty("DebuffTypeRemoved");
                }
                if (serializedObject.FindProperty("AddsBuff")?.boolValue == true)
                {
                    DrawProperty("BuffTypeAdded");
                }
            }, isConsumableWithEffect);

            DrawSection("Visuals", "Visuals", () =>
            {
                DrawProperties("Prefab", "InventoryIcon");
            });

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSection(string key, string title, Action draw, bool visible = true)
        {
            if (!visible)
            {
                return;
            }

            bool expanded = _foldouts.TryGetValue(key, out var value) && value;
            expanded = EditorGUILayout.Foldout(expanded, title, true);
            _foldouts[key] = expanded;
            if (expanded)
            {
                EditorGUI.indentLevel++;
                draw();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawProperties(params string[] names)
        {
            foreach (string name in names)
            {
                DrawProperty(name);
            }
        }

        private void DrawProperty(string name)
        {
            var property = FindProperty(name);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }

        private SerializedProperty FindProperty(string name) =>
            serializedObject.FindProperty(name)
            ?? serializedObject.FindProperty($"<{name}>k__BackingField");
    }
}
#endif
