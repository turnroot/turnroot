using TMPro;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class PopulateMapPrefabEnviromentConditions : MonoBehaviour
{
    private Brain _brain;

    // Resolve EnvironmentalConditions for a Brain (prefer PreparationObject; fall back to scene)
    private static Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment.EnvironmentalConditions ResolveEnvironmentalConditions(
        Brain b
    )
    {
        // Prefer an explicitly-assigned PreparationObject on the BattleBrain
        var prep = b?.battleBrain?.PreparationObject;
        return prep.GetComponentInChildren<Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment.EnvironmentalConditions>(
            true
        );
    }

    private static class RowHelpers
    {
        public static Image FindImage(Transform t, string childName) =>
            t.Find(childName)?.GetComponent<Image>();

        public static TextMeshProUGUI FindLabel(Transform t, string childName) =>
            t.Find(childName)?.GetComponent<TextMeshProUGUI>();

        public static (Sprite sprite, string label) FirstMatch(
            params (bool cond, Sprite sprite, string label)[] opts
        )
        {
            foreach (var o in opts)
            {
                if (o.cond)
                {
                    return (o.sprite, o.label);
                }
            }
            return (null, null);
        }

        public static void ApplyRowOrDisable(
            Transform row,
            string imageChild,
            string labelChild,
            (bool cond, Sprite sprite, string label)[] opts,
            Sprite defaultSprite = null,
            string defaultLabel = null
        )
        {
            if (row == null)
            {
                return;
            }

            var (sprite, label) = FirstMatch(opts);
            var img = FindImage(row, imageChild);
            var lbl = FindLabel(row, labelChild);

            if (
                sprite == null
                && string.IsNullOrEmpty(label)
                && defaultSprite == null
                && string.IsNullOrEmpty(defaultLabel)
            )
            {
                row.gameObject.SetActive(false);
                return;
            }

            if (sprite == null)
            {
                sprite = defaultSprite;
            }

            label ??= defaultLabel;

            // Ensure the row is active when we have something to show
            if (sprite != null || label != null)
            {
                if (!row.gameObject.activeSelf)
                {
                    row.gameObject.SetActive(true);
                }
            }

            if (img != null && sprite != null)
            {
                img.sprite = sprite;
            }

            if (lbl != null && label != null)
            {
                lbl.text = label;
            }
        }
    }

    public OperationResult Initialize(Brain brain)
    {
        _brain = brain;
        var gamewideUiSettings = _brain.uiBrain.uiSettings;
        Debug.Log("PopulateMapPrefabEnviromentConditions: Initializing with brain " + brain.name);
        // Find the child GameObject tagged 'BattleMapEnvironment' and use it directly
        GameObject envRoot = null;
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.CompareTag("BattleMapEnvironment"))
            {
                envRoot = t.gameObject;
                break;
            }
        }
        if (envRoot == null)
        {
            return OperationResult.Failure("Child with tag 'BattleMapEnvironment' not found");
        }

        var timeRow = envRoot.transform.Find("TimeRow");
        var temperatureRow = envRoot.transform.Find("TemperatureRow");
        var weatherRow = envRoot.transform.Find("WeatherRow");
        var specialRow = envRoot.transform.Find("SpecialRow");

        // Resolve EnvironmentalConditions for this brain (BattleGameObject -> PreparationObject -> scene fallback)
        var env = ResolveEnvironmentalConditions(_brain);
        if (env == null)
        {
            return OperationResult.Failure("EnvironmentalConditions not found");
        }

        RowHelpers.ApplyRowOrDisable(
            timeRow,
            "Time",
            "TimeLabel",
            new[]
            {
                (env.IsDawn, gamewideUiSettings.timeDawnImage, "Dawn"),
                (env.IsSunset, gamewideUiSettings.timeSunsetImage, "Sunset"),
                (env.IsNight, gamewideUiSettings.timeNightImage, "Night"),
                (true, gamewideUiSettings.timeDayImage, "Day"),
            }
        );
        RowHelpers.ApplyRowOrDisable(
            temperatureRow,
            "Temperature",
            "TemperatureLabel",
            new[]
            {
                (env.IsVeryCold, gamewideUiSettings.temperatureVeryColdImage, "Very Cold!"),
                (env.IsVeryHot, gamewideUiSettings.temperatureVeryHotImage, "Very Hot!"),
            }
        );
        RowHelpers.ApplyRowOrDisable(
            weatherRow,
            "Weather",
            "WeatherLabel",
            new[]
            {
                (env.IsRaining, gamewideUiSettings.isRainingImage, "Raining"),
                (env.IsSnowing, gamewideUiSettings.isSnowingImage, "Snowing"),
                (env.IsFoggy, gamewideUiSettings.isFoggyImage, "Foggy"),
                (env.IsStormy, gamewideUiSettings.isStormyImage, "Stormy"),
                (env.IsWindy, gamewideUiSettings.isWindyImage, "Windy"),
            },
            gamewideUiSettings.clearImage,
            "Sunny"
        );
        RowHelpers.ApplyRowOrDisable(
            specialRow,
            "Special",
            "SpecialLabel",
            new[]
            {
                (env.IsUnderwater, gamewideUiSettings.isUnderwaterImage, "Underwater"),
                (env.IsSwampy, gamewideUiSettings.isSwampyImage, "Swampy"),
                (env.IsUnderground, gamewideUiSettings.isUndergroundImage, "Underground"),
                (env.IsDesert, gamewideUiSettings.isDesertImage, "Desert"),
                (env.IsRocky, gamewideUiSettings.isRockyImage, "Rocky"),
                (env.IsVolcanic, gamewideUiSettings.isVolcanicImage, "Volcanic"),
            }
        );
        return OperationResult.SuccessResult();
    }
}
