using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.GameSettings
{
    public partial class GameplayGeneralSettings
    {
        [BoxGroup("UI"), HorizontalLine(color: EColor.Green)]
        public GoldDisplay GoldDisplayNames = new() { OneLetter = "G", FullName = "gold" };

        [BoxGroup("UI")]
        public bool ShowTerrainTypeDescriptionOnTileHover = false;

        [BoxGroup("UI")]
        public bool ColorTerrainEffects = true;

        [BoxGroup("Visuals"), HorizontalLine(color: EColor.Yellow)]
        public float UnitMovementCurveSmoothing = 4f;

        [BoxGroup("Visuals")]
        public float UnitMovementCurveRandomness = 0.25f;

        [BoxGroup("Visuals")]
        public float UnitMovementDecelerationRange = 1.5f;

        [BoxGroup("Visuals")]
        public float UnitMovementMinSpeedMultiplier = 0.4f;

        [BoxGroup("Visuals")]
        [Tooltip(
            "Lateral offset (in world units) applied to the arc midpoint when two units swap, so they pass beside each other rather than through each other."
        )]
        public float SwapArcOffset = 0.5f;

        [BoxGroup("Maps"), HorizontalLine(color: EColor.Green)]
        public bool UnexploredMaps;

        [ShowIf("UnexploredMaps"), BoxGroup("Maps"), Range(1, 3)]
        public int MaxNumberOfExplorers = 2;

        [ShowIf("UnexploredMaps"), BoxGroup("Maps")]
        public bool RidersAndFliersAreBetterExplorers = true;

        [ShowIf("UnexploredMaps"), BoxGroup("Maps")]
        public bool ExplorersFailIfInjured = false;
    }
}
