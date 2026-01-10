using System.Collections.Generic;
using Coffee.UIEffects;
using TMPro;
using Turnroot.Gameplay.Brain;
using Turnroot.UI.Components.GridMenu;
using Turnroot.UI.Components.Menu;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public class UnitSelectionColumns : MonoBehaviour
    {
        private Brain _brain;
        private int TotalColumns => Columns.Length;

        public GameObject UnitCellPrefab;

        public GameObject UnitCountText;
        public GameObject[] Columns;

        public int MaxSelectedUnits;

        public int SelectedCount;

        public void Initialize(Brain brain)
        {
            _brain = brain;
            var playerTeamRoster =
                _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            var LongTermMemory = _brain.ltm;
            var units = playerTeamRoster.characters;
            int unitCount = units.Length;
            var u = LtmKeys.UnitSelectedForBattlePrefix;
            var keys = LongTermMemory.RecallKeysByPrefix(u);
            MaxSelectedUnits = _brain.battleBrain.PreparationObject.MaxPlayerTeamUnits;

#if UNITY_EDITOR
            Debug.Log(
                $"UnitSelectionColumns: Initializing with {unitCount} units, {keys.Count} selection keys in LTM"
            );
#endif
            var keysSet = new HashSet<string>(keys);

            // Count currently selected units so we can fill up to MaxSelectedUnits when necessary
            int currentlySelectedCount = 0;
            foreach (var k in keys)
            {
                if (LongTermMemory.RecallBool(k))
                {
                    currentlySelectedCount++;
                }
            }

            for (int i = 0; i < unitCount; i++)
            {
                var unit = units[i];
                var whichColumn = i % TotalColumns;
                var unitCell = Instantiate(UnitCellPrefab, Columns[whichColumn].transform);

                // Ensure the instantiated unit cell has a GridMenuItem so it appears in the MenuBase menuItems
                if (!unitCell.TryGetComponent<UnitCellGridMenuItem>(out var gridMenuItem))
                {
                    gridMenuItem = unitCell.AddComponent<UnitCellGridMenuItem>();
                    gridMenuItem.IsSelectedForBattle = false;
                    gridMenuItem.CanBeSelectedForBattle = true;
                }

                // Column and Row are used by GridMenu navigation; Row is integer division (floor)
                gridMenuItem.Column = whichColumn;
                gridMenuItem.Row = i / TotalColumns;
                gridMenuItem.SetItemNamePublic($"UnitCell_{unit.CharacterData.FullName}");

                ConfigureUnitCell(
                    unitCell,
                    unit,
                    u,
                    keysSet,
                    LongTermMemory,
                    ref currentlySelectedCount
                );
            }

            // After instantiating unit cells, refresh parent menu so newly added GridMenuItems are registered
            var parentMenu = GetComponentInParent<MenuBase>(true);
            parentMenu?.RefreshMenuItems();

            // Initialize the displayed selected count from computed selections
            SelectedCount = currentlySelectedCount;

            RecomputeSelectedCount();

#if UNITY_EDITOR
            Debug.Log($"UnitSelectionColumns: Initialized SelectedCount = {SelectedCount}");
#endif
            UpdateUnitCountText();
        }

        public void RecomputeSelectedCount()
        {
            int count = 0;
            foreach (var col in Columns)
            {
                if (col == null)
                {
                    continue;
                }

                var cells = col.GetComponentsInChildren<UnitCellGridMenuItem>(true);
                foreach (var c in cells)
                {
                    if (c.IsSelectedForBattle)
                    {
                        count++;
                    }
                }
            }
            SelectedCount = Mathf.Clamp(count, 0, MaxSelectedUnits);
            UpdateUnitCountText();
        }

        private void ConfigureUnitCell(
            GameObject unitCell,
            Characters.Roster.UnitPlacement unit,
            string prefix,
            HashSet<string> keySet,
            LongTermMemory ltm,
            ref int currentlySelectedCount
        )
        {
            var uf = new UtilityFunctions();
            var nameT = uf.FindChildByTag(unitCell, "UnitCellUnitName");
            var gridMenuItem = unitCell.GetComponent<UnitCellGridMenuItem>();
            if (nameT != null && nameT.TryGetComponent<TextMeshProUGUI>(out var nameLbl))
            {
                nameLbl.text = unit.CharacterData.DisplayName;
            }

            var portraitT = uf.FindChildByTag(unitCell, "UnitCellUnitPortrait");
            if (portraitT != null && portraitT.TryGetComponent<UnityEngine.UI.Image>(out var img))
            {
                var portrait = unit.CharacterData.DefaultPortrait?.RuntimeSprite;
                if (portrait != null)
                {
                    img.sprite = portrait;
                }
            }

            var classT = uf.FindChildByTag(unitCell, "UnitCellUnitClass");
            if (classT != null && classT.TryGetComponent<TextMeshProUGUI>(out var classLbl))
            {
                classLbl.text = "n/a"; // TODO: Get current class name from roster instance?
            }

            var selectedT = uf.FindChildByTag(unitCell, "UnitCellSelected");
            if (selectedT != null)
            {
                var selectionIndicator = selectedT.gameObject;
                if (selectionIndicator != null)
                {
                    var key = prefix + unit.CharacterData.FullName;
                    bool isSelected = false;

                    if (keySet.Contains(key))
                    {
                        isSelected = ltm.RecallBool(key);
                    }
                    else
                    {
                        // Select up to MaxSelectedUnits if not present in LTM
                        if (currentlySelectedCount < MaxSelectedUnits)
                        {
                            isSelected = true;
                            ltm.RememberBool(key, true);
                            currentlySelectedCount++;
                        }
                        else
                        {
                            isSelected = false;
                            ltm.RememberBool(key, false);
                        }
                    }

                    // If the unit is required for this battle, enable them but don't save it to LTM
                    var requiredUnits = _brain.battleBrain.PreparationObject.RequiredPlayerUnits;
                    if (requiredUnits.Contains(unit.CharacterData))
                    {
                        // If not already selected via LTM or auto-fill, count them now
                        if (!isSelected)
                        {
                            isSelected = true;
                            currentlySelectedCount++;
                        }

                        // Turn on the required indicator
                        var requiredT = uf.FindChildByTag(unitCell, "UnitCellRequiredIndicator");
                        if (requiredT != null)
                        {
                            requiredT.gameObject.SetActive(true);
                            gridMenuItem.CanBeSelectedForBattle = false;
                        }
                        else
                        {
                            gridMenuItem.CanBeSelectedForBattle = true;
                        }
                    }

                    selectionIndicator.SetActive(isSelected);
                    gridMenuItem.IsSelectedForBattle = isSelected;
                    if (isSelected)
                    {
#if COFFEE_UIEFFECTS
                        if (selectionIndicator.TryGetComponent<UIEffect>(out var uiEffect))
                        {
                            uiEffect.transitionRate = Random.Range(0, 1f);
                        }
#endif
                    }
                }
            }
        }

        public void UpdateUnitCountText()
        {
            if (UnitCountText == null)
            {
                return;
            }

            if (
                UnitCountText != null
                && UnitCountText.TryGetComponent<TextMeshProUGUI>(out var textLbl)
            )
            {
                textLbl.text = $"{SelectedCount} / {MaxSelectedUnits} Units";
            }
        }
    }
}
