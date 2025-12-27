#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UiTools
{
    public static void ApplyMenuButtonSpacing()
    {
        var settings = Turnroot.GameSettings.GamewideUiSettings.Instance;
        if (settings == null)
        {
            Debug.LogWarning("GamewideUiSettings not found.");
            return;
        }

        float spacing = settings.MenuButtonSpacing;

        // 1) update MenuStyleListPrefab prefabs only (faster and safer)
        var guids = AssetDatabase.FindAssets("MenuStyleListPrefab t:Prefab");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            // guard: ensure this is the expected prefab name
            if (
                !path.EndsWith(
                    "MenuStyleListPrefab.prefab",
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            var vlg = root.GetComponentInChildren<VerticalLayoutGroup>(true);
            if (vlg != null)
            {
                Undo.RecordObject(vlg, "Apply Menu Spacing");
                vlg.spacing = spacing;
                EditorUtility.SetDirty(vlg);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"Updated VerticalLayoutGroup.spacing in prefab: {path}");
            }
            else
            {
                Debug.LogWarning($"Prefab at {path} has no VerticalLayoutGroup child");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        // 2) update open scenes
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
            {
                continue;
            }

            bool sceneDirty = false;
            foreach (var go in scene.GetRootGameObjects())
            {
                // Iterate all transforms in the root to find instances of that prefab asset
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    var nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject);
                    if (nearestRoot == null)
                    {
                        continue;
                    }

                    var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        nearestRoot
                    );
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    if (
                        !assetPath.EndsWith(
                            "MenuStyleListPrefab.prefab",
                            System.StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }

                    // Found an instance of the prefab in the scene; set its VerticalLayoutGroup spacing
                    var vlg = nearestRoot.GetComponentInChildren<VerticalLayoutGroup>(true);
                    if (vlg != null)
                    {
                        Undo.RecordObject(vlg, "Apply Menu Spacing");
                        vlg.spacing = spacing;
                        EditorUtility.SetDirty(vlg);
                        sceneDirty = true;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"MenuStyleListPrefab instance in scene '{scene.name}' has no VerticalLayoutGroup child"
                        );
                    }
                }
            }
            if (sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        Debug.Log("Menu spacing applied.");
    }
}
#endif
