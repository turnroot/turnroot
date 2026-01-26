using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Turnroot.Utilities
{
    /// <summary>
    /// Helper to load game settings assets placed under Resources/GameSettings/*.
    /// Tries a direct `Resources.Load` using the type name, then `Resources.LoadAll` on the
    /// subfolder, and finally an Editor `AssetDatabase` search as a fallback.
    /// </summary>
    public static class GameSettingsLoader
    {
        public static T LoadFirst<T>(string subfolder = "GameSettings")
            where T : ScriptableObject
        {
            // Prefer an exact-named asset matching the type name under the subfolder
            string typeName = typeof(T).Name;
            try
            {
                T found = Resources.Load<T>($"{subfolder}/{typeName}");
                if (found != null)
                {
                    return found;
                }
            }
            catch (UnityException)
            {
                // Resources.Load may throw during Unity's deserialization. Fall through to editor lookup.
            }

            // Fallback: load any asset of this type under the subfolder. Guard with try/catch to
            // avoid recursive-load exceptions (Resources.LoadAll can trigger dereferencing PPtrs).
            try
            {
                var foundAll = Resources.LoadAll<T>(subfolder);
                if (foundAll != null && foundAll.Length > 0)
                {
                    // Prefer an asset whose filename matches the type name
                    foreach (var candidate in foundAll)
                    {
                        if (candidate != null && candidate.name == typeName)
                        {
                            return candidate;
                        }
                    }
                    return foundAll[0];
                }
            }
            catch (UnityException)
            {
                // Ignore and fall back to AssetDatabase in editor mode below.
            }

#if UNITY_EDITOR
            // Editor-only fallback: search AssetDatabase for an asset under Resources/<subfolder>/
            try
            {
                string filter = $"t:{typeName}";
                var guids = AssetDatabase.FindAssets(filter);
                if (guids != null && guids.Length > 0)
                {
                    // Prefer assets explicitly under a Resources/<subfolder> directory
                    foreach (var g in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(g);
                        if (path.Contains($"/Resources/{subfolder}/"))
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                            if (asset != null)
                            {
                                return asset;
                            }
                        }
                    }

                    // Otherwise return the first found of this type in the project
                    var fallbackPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<T>(fallbackPath);
                }
            }
            catch { }
#endif

            return null;
        }
    }
}
