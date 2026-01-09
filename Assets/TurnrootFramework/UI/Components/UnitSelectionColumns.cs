using Coffee.UIEffects;
using TMPro;
using Turnroot.Gameplay.Brain;
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

        public int MaxSelectedUnits = 6; // TODO: Get this from BattlePreparationObject

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

#if UNITY_EDITOR
            Debug.Log(
                $"UnitSelectionColumns: Initializing with {unitCount} units, {keys.Count} selection keys in LTM"
            );
#endif
            var keysSet = new System.Collections.Generic.HashSet<string>(keys);

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

                ConfigureUnitCell(
                    unitCell,
                    unit,
                    u,
                    keysSet,
                    LongTermMemory,
                    ref currentlySelectedCount
                );
            }
            UpdateUnitCountText(currentlySelectedCount);
        }

        private void ConfigureUnitCell(
            GameObject unitCell,
            Characters.Roster.UnitPlacement unit,
            string prefix,
            System.Collections.Generic.HashSet<string> keySet,
            LongTermMemory ltm,
            ref int currentlySelectedCount
        )
        {
            var uf = new UtilityFunctions();
            var nameT = uf.FindChildByTag(unitCell, "UnitCellUnitName");
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

                    selectionIndicator.SetActive(isSelected);
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

        private void UpdateUnitCountText(int selectedCount)
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
                textLbl.text = $"{selectedCount} / {MaxSelectedUnits} Units";
            }
        }
    }
}
