using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Turnroot.GameSettings;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Utilities.Editor
{
    /// <summary>
    /// Tools -> Turnroot -> Test Generic Enemy Skew
    /// Small editor window for running the seedcalc logic from the original console tool and
    /// experimenting with GenericEnemySkewAdjustmentRange and player-level lists.
    /// </summary>
    public class TestGenericEnemySkewWindow : EditorWindow
    {
        private List<int> playerLevels = new List<int> { 10, 11, 12, 10, 14, 15, 12 };
        private float skewMin = -0.15f;
        private float skewMax = 0.2f;
        private string outputText = string.Empty;
        private Vector2 outputScroll;

        private int enemyCount = 10;
        private readonly int[] seeds = new[]
        {
            131662290,
            123456789,
            588400120,
            737628147,
            300104262,
        };
        private readonly (string label, double value)[] multipliers =
        {
            ("m0", 0.9),
            ("m1", 1.0),
            ("m3", 1.3),
            ("m2", 1.6),
        };

        private readonly (string name, double h)[] difficulties =
        {
            ("Easy", 3.0),
            ("Normal", 4.0),
            ("Hard", 5.0),
            ("Extreme", 6.0),
        };

        [MenuItem("Tools/Turnroot/Test Generic Enemy Skew")]
        public static void ShowWindow()
        {
            GetWindow<TestGenericEnemySkewWindow>("Test Generic Enemy Skew");
        }

        private void OnEnable()
        {
            // Try to initialize from GameplayGeneralSettings if available
            try
            {
                var g = GameplayGeneralSettings.Instance;
                if (g != null)
                {
                    skewMin = g.GenericEnemySkewAdjustmentRange.x;
                    skewMax = g.GenericEnemySkewAdjustmentRange.y;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Generic Enemy Skew Adjustment Range",
                EditorStyles.boldLabel
            );
            EditorGUILayout.BeginHorizontal();
            skewMin = EditorGUILayout.FloatField("Min", skewMin);
            skewMax = EditorGUILayout.FloatField("Max", skewMax);
            if (GUILayout.Button("Load from GameplayGeneralSettings", GUILayout.Width(220)))
            {
                if (GameplayGeneralSettings.Instance != null)
                {
                    skewMin = GameplayGeneralSettings.Instance.GenericEnemySkewAdjustmentRange.x;
                    skewMax = GameplayGeneralSettings.Instance.GenericEnemySkewAdjustmentRange.y;
                }
                else
                {
                    Debug.LogWarning("GameplayGeneralSettings.Instance not found in editor.");
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "This range affects how the deterministic skew is sampled per-enemy. Change here and press Test.",
                MessageType.Info
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player team (editable)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add level", GUILayout.Width(100)))
            {
                playerLevels.Add(12);
            }
            if (playerLevels.Count > 1 && GUILayout.Button("Remove last", GUILayout.Width(100)))
            {
                playerLevels.RemoveAt(playerLevels.Count - 1);
            }
            if (GUILayout.Button("Reset default", GUILayout.Width(120)))
            {
                playerLevels = new List<int> { 10, 11, 12, 10, 14, 15, 12 };
            }
            EditorGUILayout.EndHorizontal();

            // Draw the list of player levels
            for (int i = 0; i < playerLevels.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                playerLevels[i] = EditorGUILayout.IntField($"[{i}]", playerLevels[i]);
                if (GUILayout.Button("↑", GUILayout.Width(24)) && i > 0)
                {
                    var t = playerLevels[i - 1];
                    playerLevels[i - 1] = playerLevels[i];
                    playerLevels[i] = t;
                }
                if (GUILayout.Button("↓", GUILayout.Width(24)) && i < playerLevels.Count - 1)
                {
                    var t = playerLevels[i + 1];
                    playerLevels[i + 1] = playerLevels[i];
                    playerLevels[i] = t;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test", GUILayout.Height(30)))
            {
                outputText = RunTest();
                outputScroll = Vector2.zero;
            }
            if (GUILayout.Button("Copy output", GUILayout.Height(30), GUILayout.Width(110)))
            {
                EditorGUIUtility.systemCopyBuffer = outputText ?? "";
            }
            if (GUILayout.Button("Save range to GameplayGeneralSettings", GUILayout.Height(30)))
            {
                if (GameplayGeneralSettings.Instance != null)
                {
                    GameplayGeneralSettings.Instance.GenericEnemySkewAdjustmentRange = new Vector2(
                        skewMin,
                        skewMax
                    );
                    EditorUtility.SetDirty(GameplayGeneralSettings.Instance);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    Debug.LogWarning("GameplayGeneralSettings.Instance not found in editor.");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            outputScroll = EditorGUILayout.BeginScrollView(outputScroll);
            EditorGUILayout.TextArea(
                outputText ?? string.Empty,
                GUILayout.ExpandHeight(true),
                GUILayout.Height(420)
            );
            EditorGUILayout.EndScrollView();
        }

        private string RunTest()
        {
            var sb = new StringBuilder();

            int[] playerArr = playerLevels.ToArray();
            double playerMean = Mean(playerArr);

            sb.AppendLine("PLAYER TEAM");
            AppendStats(sb, "Player team", playerArr, playerMean);
            sb.AppendLine();

            foreach (var (label, value) in multipliers)
            {
                foreach (var (name, h) in difficulties)
                {
                    foreach (var seed in seeds)
                    {
                        var adjusted = ComputeAdjustedLevels(
                            playerArr,
                            value,
                            h,
                            seed,
                            enemyCount,
                            skewMin,
                            skewMax
                        );
                        sb.AppendLine($"Multiplier={value}, difficulty={name}, seed={seed}");
                        AppendStats(
                            sb,
                            $"Enemies (mult={label}, h={name}, seed={seed})",
                            adjusted,
                            playerMean
                        );
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        // --- Copied/adapted helpers from original console tool ---
        private static int StableStringHash(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return 0;
            }
            unchecked
            {
                int hash = 23;
                foreach (var c in s)
                {
                    hash = hash * 31 + c;
                }

                return hash;
            }
        }

        private static double DeterministicDouble(
            double min,
            double max,
            int battleSeed,
            string salt
        )
        {
            unchecked
            {
                int seed =
                    (battleSeed == 0 ? (int)0x9E3779B9 : battleSeed) ^ StableStringHash(salt ?? "");
                var rnd = new System.Random(seed);
                return min + rnd.NextDouble() * (max - min);
            }
        }

        private static List<int> ComputeAdjustedLevels(
            int[] playerLevels,
            double multiplier,
            double h,
            int battleSeed,
            int enemyCount,
            double skewMin,
            double skewMax
        )
        {
            int averagePlayerLevel =
                playerLevels.Length > 0 ? (int)Math.Round(Math.Round(playerLevels.Average())) : 1;

            double highest_d = (averagePlayerLevel * 10) + Math.Ceiling(h * (multiplier * 10));
            int highest = (int)Math.Round(highest_d / 10.0);

            double lowest_d =
                (playerLevels.Min() * 10) - Math.Ceiling((7.0 - h) * (multiplier * 10));
            int lowest = (int)Math.Round(lowest_d / 10.0);
            if (lowest < 1)
            {
                lowest = 1;
            }

            var result = new List<int>();
            for (int i = 0; i < enemyCount; i++)
            {
                string salt = "Enemy" + i;
                // <-- updated: use skewMin/skewMax from GameplayGeneralSettings (or UI override)
                double skewRand = DeterministicDouble(skewMin, skewMax, battleSeed, salt);
                double localSkew = multiplier - 1.0 + skewRand;
                double chosen;
                if (localSkew <= 0)
                {
                    chosen = DeterministicDouble(lowest, averagePlayerLevel, battleSeed, salt);
                }
                else
                {
                    chosen = DeterministicDouble(averagePlayerLevel, highest, battleSeed, salt);
                }
                int adjusted = (int)chosen;
                result.Add(adjusted);
            }
            return result;
        }

        private static double Mean(IEnumerable<int> vals)
        {
            var arr = vals.ToArray();
            return arr.Length == 0 ? 0.0 : arr.Average();
        }

        private static double Median(IList<int> sorted)
        {
            if (sorted == null || sorted.Count == 0)
            {
                return 0.0;
            }

            int n = sorted.Count;
            if (n % 2 == 1)
            {
                return sorted[n / 2];
            }

            return (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        }

        private static (double q1, double mean, double q3) QuartileStats(IEnumerable<int> values)
        {
            var list = values.OrderBy(x => x).ToList();
            double mean = Mean(list);
            int n = list.Count;
            if (n == 0)
            {
                return (0.0, mean, 0.0);
            }

            List<int> lower,
                upper;
            if (n % 2 == 0)
            {
                lower = list.Take(n / 2).ToList();
                upper = list.Skip(n / 2).ToList();
            }
            else
            {
                lower = list.Take(n / 2).ToList();
                upper = list.Skip(n / 2 + 1).ToList();
            }

            double q1 = Median(lower);
            double q3 = Median(upper);
            return (q1, mean, q3);
        }

        private static void AppendStats(
            StringBuilder sb,
            string title,
            IEnumerable<int> values,
            double playerMean
        )
        {
            var list = values.ToList();
            var (q1, mean, q3) = QuartileStats(list);
            double diff = mean - playerMean;
            sb.AppendLine($"{title}: {string.Join(", ", list)}");
            sb.AppendLine(
                $"  mean = {mean:0.##}, difference vs player mean = {diff:+0.##;-0.##;0.00}"
            );
            sb.AppendLine($"  quartiles (25% / mean / 75%) = {q1:0.##} / {mean:0.##} / {q3:0.##}");
        }
    }
}
