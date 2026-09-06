using System.Linq;
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
        private TextMeshProUGUI TerrainTypeName;

        [ShowIf(nameof(ShowTerrainTypeDescriptionOnTileHover))]
        private TextMeshProUGUI TerrainTypeDescription;
        private TextMeshProUGUI TerrainTypeDefense;
        private TextMeshProUGUI TerrainTypeAvoid;
        private TextMeshProUGUI TerrainTypeHealth;

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

            var texts = displayObj.transform.GetComponentsInChildren<TextMeshProUGUI>();

            TerrainTypeName = texts.FirstOrDefault(t => t.gameObject.name == "TerrainTypeName");
            if (ShowTerrainTypeDescriptionOnTileHover)
            {
                TerrainTypeDescription = texts.FirstOrDefault(t =>
                    t.gameObject.name == "TerrainTypeDescription"
                );
            }
            TerrainTypeDefense = texts.FirstOrDefault(t =>
                t.gameObject.name == "TerrainTypeDefense"
            );
            TerrainTypeAvoid = texts.FirstOrDefault(t => t.gameObject.name == "TerrainTypeAvoid");
            TerrainTypeHealth = texts.FirstOrDefault(t => t.gameObject.name == "TerrainTypeHealth");
            return OperationResult.Successful();
        }

        public OperationResult Display(MapGridPoint point, MovementType movementType)
        {
            if (displayObj == null)
            {
                return OperationResult.Failure("TerrainTypeOverlay not initialized");
            }

            displayObj.SetActive(true);
            var t = point.GetCachedTerrainType();
            if (t == null)
            {
                return OperationResult.Failure(
                    $"TerrainTypeOverlay: No terrain type found for point"
                );
            }
            TerrainTypeName.text = t != null ? t.Name : "Unknown";
            if (ShowTerrainTypeDescriptionOnTileHover && TerrainTypeDescription != null)
            {
                TerrainTypeDescription.text =
                    t != null ? t.Description : "No description available.";
            }
            switch (movementType)
            {
                case MovementType.Infantry:
                    TerrainTypeDefense.text = FormatStatOrDash(t?.DefenseBonusWalk);
                    TerrainTypeAvoid.text = FormatStatOrDash(t?.AvoidBonusWalk);
                    TerrainTypeHealth.text = FormatStatOrDash(t?.HealthChangePerTurnWalk);
                    SetTextColorIfNotNull(TerrainTypeDefense, t?.DefenseBonusWalk);
                    SetTextColorIfNotNull(TerrainTypeAvoid, t?.AvoidBonusWalk);
                    SetTextColorIfNotNull(TerrainTypeHealth, t?.HealthChangePerTurnWalk);
                    break;
                case MovementType.Riding:
                    TerrainTypeDefense.text = FormatStatOrDash(t?.DefenseBonusRiding);
                    TerrainTypeAvoid.text = FormatStatOrDash(t?.AvoidBonusRiding);
                    TerrainTypeHealth.text = FormatStatOrDash(t?.HealthChangePerTurnRiding);
                    SetTextColorIfNotNull(TerrainTypeDefense, t?.DefenseBonusRiding);
                    SetTextColorIfNotNull(TerrainTypeAvoid, t?.AvoidBonusRiding);
                    SetTextColorIfNotNull(TerrainTypeHealth, t?.HealthChangePerTurnRiding);
                    break;
                case MovementType.Flying:
                    TerrainTypeDefense.text = FormatStatOrDash(t?.DefenseBonusFlying);
                    TerrainTypeAvoid.text = FormatStatOrDash(t?.AvoidBonusFlying);
                    TerrainTypeHealth.text = FormatStatOrDash(t?.HealthChangePerTurnFlying);
                    SetTextColorIfNotNull(TerrainTypeDefense, t?.DefenseBonusFlying);
                    SetTextColorIfNotNull(TerrainTypeAvoid, t?.AvoidBonusFlying);
                    SetTextColorIfNotNull(TerrainTypeHealth, t?.HealthChangePerTurnFlying);
                    break;
                default:
                    if (TerrainTypeDefense != null)
                    {
                        TerrainTypeDefense.text = "-";
                    }

                    if (TerrainTypeAvoid != null)
                    {
                        TerrainTypeAvoid.text = "-";
                    }

                    if (TerrainTypeHealth != null)
                    {
                        TerrainTypeHealth.text = "-";
                    }

                    break;
            }
            return OperationResult.Successful();
        }

        private string FormatStatOrDash(int? value) =>
            value.GetValueOrDefault() == 0 ? "-" : value.Value.ToString();

        private void SetTextColorIfNotNull(TextMeshProUGUI textElement, int? value)
        {
            if (textElement == null)
            {
                return;
            }
            textElement.color =
                !ColorTerrainEffects || value == null || value == 0 ? NeutralColor
                : value > 0 ? GoodColor
                : BadColor;
        }

        public void ResetDisplay()
        {
            displayObj?.SetActive(false);
        }
    }
}
