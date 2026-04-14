#if UNITY_EDITOR
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.GameSettings;
using Turnroot.Gameplay.Roster;
using Turnroot.Utilities.AbstractScripts;
using UnityEditor;
using UnityEngine;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Gameplay.PlayerSettings;

namespace Turnroot.EditorTools
{
    /// <summary>
    /// Editor window that validates required singleton ScriptableObjects are present and correctly configured.
    /// </summary>
    public class RequiredScriptableObjectsChecklist : EditorWindow
    {
        private Vector2 _scroll;
        private List<CheckResult> _results = new();
        private string _statusMessage = "";

        [MenuItem("Window/Turnroot/Checklists/Required Scriptable Objects")]
        public static void ShowWindow()
        {
            var w = GetWindow<RequiredScriptableObjectsChecklist>("Required ScriptableObjects");
            w.Refresh();
            w.minSize = new Vector2(600, 300);
        }

        private void OnEnable() => Refresh();

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Required ScriptableObjects Checklist", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                Refresh();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DrawLegend();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_statusMessage, EditorStyles.helpBox);
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawResults();
            EditorGUILayout.EndScrollView();
        }

        // ── Drawing ──────────────────────────────────────────────────────────

        private void DrawLegend()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLegendItem(Color.red, "Red: Will break / error on run");
            DrawLegendItem(new Color(1f, 0.6f, 0f), "Orange: Missing optional but may still run");
            DrawLegendItem(Color.yellow, "Yellow: Misconfigured / needs polish");
            DrawLegendItem(Color.green, "Green: Looks good");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLegendItem(Color col, string text)
        {
            var rect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
            EditorGUI.DrawRect(rect, col);
            GUILayout.Label(text);
        }

        private void DrawResults()
        {
            if (_results == null || _results.Count == 0)
            {
                EditorGUILayout.LabelField("No checks available.");
                return;
            }

            foreach (var r in _results)
            {
                DrawRow(r);
            }
        }

        private void DrawRow(CheckResult r)
        {
            EditorGUILayout.BeginHorizontal();

            // Colour swatch
            var swatchRect = GUILayoutUtility.GetRect(16, 20, GUILayout.Width(16));
            EditorGUI.DrawRect(swatchRect, r.Color);
            GUILayout.Space(4);

            // Label and description
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(r.Label, EditorStyles.boldLabel, GUILayout.Width(280));
            if (!string.IsNullOrEmpty(r.Note))
            {
                EditorGUILayout.LabelField(r.Note, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Ping button for found assets
            if (r.Asset != null)
            {
                if (GUILayout.Button("Ping", GUILayout.Width(48)))
                {
                    Selection.activeObject = r.Asset;
                    EditorGUIUtility.PingObject(r.Asset);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        // ── Data ─────────────────────────────────────────────────────────────

        private class CheckResult
        {
            public string Label;
            public string Note;
            public Color Color = Color.gray;
            public Object Asset;
        }

        private void Refresh()
        {
            _results.Clear();
            _statusMessage = "Checking...";

            CheckGameplayGeneralSettings();
            CheckGameplayInputSettings();
            CheckGamewideUiSettings();
            CheckGraphics2DSettings();
            CheckGameplayPlayerSettings();
            CheckGamePackageSettings();
            CheckPersistentPlayerRoster();
            CheckSupportRelationshipTable();

            int critical = 0,
                warn = 0,
                ok = 0;
            foreach (var r in _results)
            {
                if (r.Color == Color.red)
                    critical++;
                else if (r.Color == new Color(1f, 0.6f, 0f) || r.Color == Color.yellow)
                    warn++;
                else
                    ok++;
            }

            _statusMessage =
                $"{_results.Count} checks — {critical} critical, {warn} warnings, {ok} OK";
            Repaint();
        }

        // ── Individual checks ─────────────────────────────────────────────────

        private void CheckGameplayGeneralSettings()
        {
            var asset = FindSingleton<GameplayGeneralSettings>("GameplayGeneralSettings");
            if (asset == null)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "GameplayGeneralSettings",
                        Note =
                            "Asset not found in Resources. This is required for stat defaults, "
                            + "combat settings, and class validation.",
                        Color = Color.red,
                    }
                );
                return;
            }

            _results.Add(
                new CheckResult
                {
                    Label = "GameplayGeneralSettings",
                    Note = $"Found: {AssetDatabase.GetAssetPath(asset)}",
                    Color = Color.green,
                    Asset = asset,
                }
            );
        }

        private void CheckGameplayInputSettings()
        {
            var asset = FindSingleton<GameplayInputSettings>("GameplayInputSettings");
            if (asset == null)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "GameplayInputSettings",
                        Note =
                            "Asset not found in Resources. Required for UI input action bindings at runtime.",
                        Color = Color.red,
                    }
                );
                return;
            }

            _results.Add(
                new CheckResult
                {
                    Label = "GameplayInputSettings",
                    Note = $"Found: {AssetDatabase.GetAssetPath(asset)}",
                    Color = Color.green,
                    Asset = asset,
                }
            );
        }

        private void CheckGamewideUiSettings()
        {
            var asset = FindSingleton<GamewideUiSettings>("GamewideUiSettings");
            if (asset == null)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "GamewideUiSettings",
                        Note =
                            "Asset not found in Resources. Required for UI panels and map grid rendering.",
                        Color = Color.red,
                    }
                );
                return;
            }

            _results.Add(
                new CheckResult
                {
                    Label = "GamewideUiSettings",
                    Note = $"Found: {AssetDatabase.GetAssetPath(asset)}",
                    Color = Color.green,
                    Asset = asset,
                }
            );
        }

        private void CheckGraphics2DSettings()
        {
            var asset = FindSingleton<Graphics2DSettings>("Graphics2DSettings");
            if (asset == null)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "Graphics2DSettings",
                        Note =
                            "Asset not found in Resources. Required for conversation portraits and 2D graphics.",
                        Color = Color.red,
                    }
                );
                return;
            }

            _results.Add(
                new CheckResult
                {
                    Label = "Graphics2DSettings",
                    Note = $"Found: {AssetDatabase.GetAssetPath(asset)}",
                    Color = Color.green,
                    Asset = asset,
                }
            );
        }

        private void CheckGameplayPlayerSettings()
        {
            var asset = FindSingleton<GameplayPlayerSettings>("GameplayPlayerSettings");
            if (asset == null)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "GameplayPlayerSettings",
                        Note =
                            "Asset not found in Resources. Required for quality/permadeath settings at runtime.",
                        Color = new Color(1f, 0.6f, 0f),
                    }
                );
                return;
            }

            _results.Add(
                new CheckResult
                {
                    Label = "GameplayPlayerSettings",
                    Note = $"Found: {AssetDatabase.GetAssetPath(asset)}",
                    Color = Color.green,
                    Asset = asset,
                }
            );
        }

        private void CheckGamePackageSettings()
        {
            var asset = FindSingleton<GamePackage.GamePackageSettings>("GamePackageSettings");
            if (asset == null)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "GamePackageSettings",
                        Note =
                            "Asset not found in Resources. Required for map grid rendering and game package configuration.",
                        Color = new Color(1f, 0.6f, 0f),
                    }
                );
                return;
            }

            _results.Add(
                new CheckResult
                {
                    Label = "GamePackageSettings",
                    Note = $"Found: {AssetDatabase.GetAssetPath(asset)}",
                    Color = Color.green,
                    Asset = asset,
                }
            );
        }

        private void CheckPersistentPlayerRoster()
        {
            var asset = FindSingleton<PersistentPlayerRoster>("PersistentPlayerRoster");
            if (asset == null)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "PersistentPlayerRoster",
                        Note =
                            "Asset not found in Resources. Required for the hub, roster persistence, and avatar lookup.",
                        Color = Color.red,
                    }
                );
                return;
            }

            if (asset.PlayerRoster == null)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "PersistentPlayerRoster — PlayerRoster reference",
                        Note =
                            $"Found at {AssetDatabase.GetAssetPath(asset)} but PlayerRoster field is not assigned.",
                        Color = Color.red,
                        Asset = asset,
                    }
                );
                return;
            }

            // Check avatar presence
            CheckPersistentRosterAvatarEntry(asset.PlayerRoster, asset);
        }

        private void CheckPersistentRosterAvatarEntry(PlayerTeamRoster roster, Object rosterAsset)
        {
            _results.Add(
                new CheckResult
                {
                    Label = "PersistentPlayerRoster",
                    Note = $"Found, PlayerRoster assigned: {roster.name}",
                    Color = Color.green,
                    Asset = rosterAsset,
                }
            );

            // Verify at least one Avatar-type character exists in the roster
            bool hasAvatar = false;
            if (roster.characters != null)
            {
                foreach (var placement in roster.characters)
                {
                    if (
                        placement?.CharacterData != null
                        && placement.CharacterData.Which == CharacterWhich.AVATAR
                    )
                    {
                        hasAvatar = true;
                        break;
                    }
                }
            }

            if (!hasAvatar)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "PersistentPlayerRoster — Avatar entry",
                        Note =
                            $"No character with Which=Avatar found in '{roster.name}'. "
                            + "Add the avatar CharacterData to the roster's Characters list so it "
                            + "can be spawned at hub unit interaction points.",
                        Color = Color.red,
                        Asset = roster,
                    }
                );
            }
            else
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "PersistentPlayerRoster — Avatar entry",
                        Note = "Avatar character is present in the roster.",
                        Color = Color.green,
                        Asset = roster,
                    }
                );
            }
        }

        private void CheckSupportRelationshipTable()
        {
            var asset = FindSingleton<SupportRelationshipTable>("SupportRelationshipTable");
            if (asset == null)
            {
                _results.Add(
                    new CheckResult
                    {
                        Label = "SupportRelationshipTable",
                        Note =
                            "Asset not found in Resources. Required to define valid support pairings and maximum support ranks.",
                        Color = Color.red,
                    }
                );
                return;
            }

            _results.Add(
                new CheckResult
                {
                    Label = "SupportRelationshipTable",
                    Note = $"Found: {AssetDatabase.GetAssetPath(asset)}",
                    Color = Color.green,
                    Asset = asset,
                }
            );
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Finds a singleton ScriptableObject first via Resources (mirrors runtime path)
        /// then falls back to AssetDatabase search.
        /// </summary>
        private static T FindSingleton<T>(string resourceName)
            where T : ScriptableObject
        {
            var loaded = Resources.Load<T>(resourceName);
            if (loaded != null)
                return loaded;

            // Fallback: scan all assets of this type
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    return asset;
            }
            return null;
        }
    }
}
#endif
