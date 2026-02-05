using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public static partial class DamageCalculator
    {
        #region Helper Methods
        private static bool ValidateBasicInputs(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        )
        {
            if (attacker == null || target == null || weaponItem?.Template == null)
            {
                TurnrootLogger.Log(
                    "CalculatePotentialDamage: null attacker, target, or weapon",
                    TurnrootLogger.LogLevel.Warning
                );
                return false;
            }
            return true;
        }

        private static GameplayGeneralSettings LoadSettings() =>
            GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");

        private static void GetHitFormulaMultipliers(
            GameplayGeneralSettings settings,
            out float skillMult,
            out float dexMult,
            out float luckMult
        )
        {
            if (settings != null)
            {
                settings.GetHitFormulaMultipliers(out skillMult, out dexMult, out luckMult);
            }
            else
            {
                skillMult = 2f;
                dexMult = 1f;
                luckMult = 0.5f;
            }
        }

        private static void GetCritFormulaMultipliers(
            GameplayGeneralSettings settings,
            out float skillMult,
            out float luckMult
        )
        {
            if (settings != null)
            {
                settings.GetCritFormulaMultipliers(out skillMult, out luckMult);
            }
            else
            {
                skillMult = 0.5f;
                luckMult = 0f;
            }
        }

        private static void GetAvoidFormulaMultipliers(
            GameplayGeneralSettings settings,
            out float speedMult,
            out float luckMult
        )
        {
            if (settings != null)
            {
                settings.GetAvoidFormulaMultipliers(out speedMult, out luckMult);
            }
            else
            {
                speedMult = 2f;
                luckMult = 1f;
            }
        }
        #endregion
    }
}
