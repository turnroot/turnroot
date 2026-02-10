using NaughtyAttributes;
using TMPro;
using Turnroot.Gameplay.Maps;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Components.Battle
{
    public class TerrainTypeOverlay : MonoBehaviour
    {
        public GameObject terrainTypeDisplayPrefab;
        private GameObject displayObj;
        public TextMeshProUGUI TerrainTypeName;

        [ShowIf(nameof(ShowTerrainTypeDescriptionOnTileHover))]
        public TextMeshProUGUI TerrainTypeDescription;
        public TextMeshProUGUI TerrainTypeDefense;
        public TextMeshProUGUI TerrainTypeAvoid;
        public TextMeshProUGUI TerrainTypeHealth;

        private bool ShowTerrainTypeDescriptionOnTileHover = false;
        private bool ColorTerrainEffects = true;

        [ShowIf(nameof(ColorTerrainEffects))]
        public Color GoodColor = Color.green;

        [ShowIf(nameof(ColorTerrainEffects))]
        public Color BadColor = Color.red;

        public Color NeutralColor = Color.black;

        public OperationResult Initialize()
        {
            try
            {
                displayObj = Instantiate(terrainTypeDisplayPrefab, transform);
                ResetDisplay();
                ShowTerrainTypeDescriptionOnTileHover = GameplayGeneralSettings
                    .Instance
                    .ShowTerrainTypeDescriptionOnTileHover;
                ColorTerrainEffects = GameplayGeneralSettings.Instance.ColorTerrainEffects;
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure(
                    $"Failed to initialize TerrainTypeOverlay: {ex.Message}"
                );
            }
            return OperationResult.Successful();
        }

        public OperationResult Display(MapGridPoint point, MovementType movementType)
        {
            displayObj.SetActive(true);
            var t = point.GetCachedTerrainType();
            if (t == null)
            {
                return OperationResult.Failure(
                    $"TerrainTypeOverlay: No terrain type found for point"
                );
            }
            TerrainTypeName.text = t != null ? t.Name : "Unknown";
            TerrainTypeDescription.text = t != null ? t.Description : "No description available.";
            switch (movementType)
            {
                case MovementType.Infantry:
                    TerrainTypeDefense.text = FormatStatOrDash(t?.DefenseBonusWalk);
                    TerrainTypeAvoid.text = FormatStatOrDash(t?.AvoidBonusWalk);
                    TerrainTypeHealth.text = FormatStatOrDash(t?.HealthChangePerTurnWalk);
                    SetTextColor(TerrainTypeDefense, t?.DefenseBonusWalk);
                    SetTextColor(TerrainTypeAvoid, t?.AvoidBonusWalk);
                    SetTextColor(TerrainTypeHealth, t?.HealthChangePerTurnWalk);
                    break;
                case MovementType.Riding:
                    TerrainTypeDefense.text = FormatStatOrDash(t?.DefenseBonusRiding);
                    TerrainTypeAvoid.text = FormatStatOrDash(t?.AvoidBonusRiding);
                    TerrainTypeHealth.text = FormatStatOrDash(t?.HealthChangePerTurnRiding);
                    SetTextColor(TerrainTypeDefense, t?.DefenseBonusRiding);
                    SetTextColor(TerrainTypeAvoid, t?.AvoidBonusRiding);
                    SetTextColor(TerrainTypeHealth, t?.HealthChangePerTurnRiding);
                    break;
                case MovementType.Flying:
                    TerrainTypeDefense.text = FormatStatOrDash(t?.DefenseBonusFlying);
                    TerrainTypeAvoid.text = FormatStatOrDash(t?.AvoidBonusFlying);
                    TerrainTypeHealth.text = FormatStatOrDash(t?.HealthChangePerTurnFlying);
                    SetTextColor(TerrainTypeDefense, t?.DefenseBonusFlying);
                    SetTextColor(TerrainTypeAvoid, t?.AvoidBonusFlying);
                    SetTextColor(TerrainTypeHealth, t?.HealthChangePerTurnFlying);
                    break;
                default:
                    TerrainTypeDefense.text = "-";
                    TerrainTypeAvoid.text = "-";
                    TerrainTypeHealth.text = "-";
                    break;
            }
            if (!ShowTerrainTypeDescriptionOnTileHover)
            {
                TerrainTypeDescription.gameObject.SetActive(false);
            }
            return OperationResult.Successful();
        }

        private string FormatStatOrDash(int? value) =>
            value.GetValueOrDefault() == 0 ? "-" : value.Value.ToString();

        private void SetTextColor(TextMeshProUGUI textElement, int? value) =>
            textElement.color =
                !ColorTerrainEffects || value == null || value == 0 ? NeutralColor
                : value > 0 ? GoodColor
                : BadColor;

        public void ResetDisplay() => displayObj.SetActive(false);
    }
}
