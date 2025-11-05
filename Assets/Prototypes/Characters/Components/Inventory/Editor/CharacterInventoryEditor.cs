using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterInventory))]
public class CharacterInventoryEditor : Editor
{
    private SerializedProperty _inventoryItems;
    private SerializedProperty _capacity;
    private SerializedProperty _equippedItemIndices;
    private SerializedProperty _isWeaponEquipped;
    private SerializedProperty _isShieldEquipped;
    private SerializedProperty _isAccessoryEquipped;

    private void OnEnable()
    {
        _inventoryItems = serializedObject.FindProperty("_inventoryItems");
        _capacity = serializedObject.FindProperty("_capacity");
        _equippedItemIndices = serializedObject.FindProperty("_equippedItemIndices");
        _isWeaponEquipped = serializedObject.FindProperty("_isWeaponEquipped");
        _isShieldEquipped = serializedObject.FindProperty("_isShieldEquipped");
        _isAccessoryEquipped = serializedObject.FindProperty("_isAccessoryEquipped");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CharacterInventory inventory = (CharacterInventory)target;

        // Capacity setting
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inventory Settings", EditorStyles.boldLabel);
        _ = EditorGUILayout.PropertyField(_capacity, new GUIContent("Capacity"));

        // Ensure capacity is at least 0
        if (_capacity.intValue < 0)
            _capacity.intValue = 0;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inventory Items", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            $"Items: {_inventoryItems.arraySize}/{_capacity.intValue}",
            _inventoryItems.arraySize > _capacity.intValue ? MessageType.Error : MessageType.Info
        );

        // Warn if over capacity
        if (_inventoryItems.arraySize > _capacity.intValue)
        {
            EditorGUILayout.HelpBox(
                "Inventory exceeds capacity! Remove items or increase capacity.",
                MessageType.Warning
            );
        }

        // Calculate current weight
        int totalWeight = 0;
        for (int i = 0; i < _inventoryItems.arraySize; i++)
        {
            var item = _inventoryItems.GetArrayElementAtIndex(i).objectReferenceValue as ObjectItem;
            if (item != null)
                totalWeight += item.Weight;
        }
        EditorGUILayout.LabelField($"Total Weight: {totalWeight}");

        EditorGUILayout.Space();

        // Add item button
        GUI.enabled = _inventoryItems.arraySize < _capacity.intValue;
        if (GUILayout.Button("Add Item Slot"))
        {
            _inventoryItems.arraySize++;
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Display inventory items
        for (int i = 0; i < _inventoryItems.arraySize; i++)
        {
            _ = EditorGUILayout.BeginVertical("box");

            _ = EditorGUILayout.BeginHorizontal();

            // Item field
            var itemProp = _inventoryItems.GetArrayElementAtIndex(i);
            var item = itemProp.objectReferenceValue as ObjectItem;

            _ = EditorGUILayout.PropertyField(itemProp, new GUIContent($"Slot {i}"));

            // Remove button
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                _inventoryItems.DeleteArrayElementAtIndex(i);
                // Also clear equipped state if this was equipped
                for (int slot = 0; slot < 3; slot++)
                {
                    if (_equippedItemIndices.GetArrayElementAtIndex(slot).intValue == i)
                    {
                        _equippedItemIndices.GetArrayElementAtIndex(slot).intValue = -1;
                        UpdateEquippedFlag(slot, false);
                    }
                    else if (_equippedItemIndices.GetArrayElementAtIndex(slot).intValue > i)
                    {
                        // Adjust indices
                        _equippedItemIndices.GetArrayElementAtIndex(slot).intValue--;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            // Show item details if assigned
            if (item != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Type", item.ItemType.ToString());
                EditorGUILayout.LabelField("Weight", item.Weight.ToString());

                // Check if item is equipped
                bool isEquipped = false;
                int equippedSlot = -1;
                for (int slot = 0; slot < 3; slot++)
                {
                    if (_equippedItemIndices.GetArrayElementAtIndex(slot).intValue == i)
                    {
                        isEquipped = true;
                        equippedSlot = slot;
                        break;
                    }
                }

                // Equipment controls
                if (CanBeEquipped(item.ItemType))
                {
                    _ = EditorGUILayout.BeginHorizontal();

                    bool newEquipped = EditorGUILayout.Toggle("Equipped", isEquipped);
                    if (newEquipped != isEquipped)
                    {
                        if (newEquipped)
                        {
                            // Equip the item
                            EquipItemAtIndex(i, item.ItemType);
                        }
                        else
                        {
                            // Unequip the item
                            if (equippedSlot >= 0)
                            {
                                _equippedItemIndices.GetArrayElementAtIndex(equippedSlot).intValue =
                                    -1;
                                UpdateEquippedFlag(equippedSlot, false);
                            }
                        }
                    }

                    if (isEquipped)
                    {
                        EditorGUILayout.LabelField(
                            $"({GetSlotName(equippedSlot)})",
                            GUILayout.Width(80)
                        );
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("Cannot be equipped");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // Equipment summary
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Equipment Status", EditorStyles.boldLabel);
        _ = EditorGUILayout.BeginVertical("box");

        DrawEquipmentSlot(0, "Weapon", ObjectItemType.Weapon);
        DrawEquipmentSlot(1, "Shield", ObjectItemType.Shield);
        DrawEquipmentSlot(2, "Accessory", ObjectItemType.Accessory);

        EditorGUILayout.EndVertical();

        _ = serializedObject.ApplyModifiedProperties();
    }

    private void DrawEquipmentSlot(int slotIndex, string slotName, ObjectItemType expectedType)
    {
        int equippedIndex = _equippedItemIndices.GetArrayElementAtIndex(slotIndex).intValue;

        _ = EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(slotName, GUILayout.Width(80));

        if (equippedIndex >= 0 && equippedIndex < _inventoryItems.arraySize)
        {
            var item =
                _inventoryItems.GetArrayElementAtIndex(equippedIndex).objectReferenceValue
                as ObjectItem;
            if (item != null)
            {
                // Validate type matches
                if (item.ItemType != expectedType)
                {
                    EditorGUILayout.HelpBox(
                        $"ERROR: {item.itemName} is type {item.ItemType}, should be {expectedType}",
                        MessageType.Error
                    );
                    if (GUILayout.Button("Fix", GUILayout.Width(50)))
                    {
                        _equippedItemIndices.GetArrayElementAtIndex(slotIndex).intValue = -1;
                        UpdateEquippedFlag(slotIndex, false);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"{item.itemName} (Slot {equippedIndex})",
                        EditorStyles.boldLabel
                    );
                }
            }
            else
            {
                EditorGUILayout.LabelField("(Empty - Invalid Index)", EditorStyles.helpBox);
            }
        }
        else
        {
            EditorGUILayout.LabelField("(Empty)", EditorStyles.helpBox);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void EquipItemAtIndex(int inventoryIndex, ObjectItemType itemType)
    {
        int slotIndex = itemType switch
        {
            ObjectItemType.Weapon => 0,
            ObjectItemType.Shield => 1,
            ObjectItemType.Accessory => 2,
            _ => -1,
        };

        if (slotIndex == -1)
            return;

        // Unequip any item in this slot
        _equippedItemIndices.GetArrayElementAtIndex(slotIndex).intValue = inventoryIndex;
        UpdateEquippedFlag(slotIndex, true);
    }

    private void UpdateEquippedFlag(int slotIndex, bool equipped)
    {
        switch (slotIndex)
        {
            case 0:
                _isWeaponEquipped.boolValue = equipped;
                break;
            case 1:
                _isShieldEquipped.boolValue = equipped;
                break;
            case 2:
                _isAccessoryEquipped.boolValue = equipped;
                break;
        }
    }

    private bool CanBeEquipped(ObjectItemType itemType)
    {
        return itemType == ObjectItemType.Weapon
            || itemType == ObjectItemType.Shield
            || itemType == ObjectItemType.Accessory;
    }

    private string GetSlotName(int slotIndex)
    {
        return slotIndex switch
        {
            0 => "Weapon",
            1 => "Shield",
            2 => "Accessory",
            _ => "Unknown",
        };
    }
}
