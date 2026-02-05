#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI
{
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
            UpdateOpenScenes(spacing);

            Debug.Log("Menu spacing applied.");
        }

        private static void UpdateOpenScenes(float spacing)
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                if (UpdateSceneMenuSpacing(scene, spacing))
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        private static bool UpdateSceneMenuSpacing(Scene scene, float spacing)
        {
            bool sceneDirty = false;

            foreach (var go in scene.GetRootGameObjects())
            {
                if (ProcessGameObjectForMenuSpacing(go, spacing, scene.name))
                {
                    sceneDirty = true;
                }
            }

            return sceneDirty;
        }

        private static bool ProcessGameObjectForMenuSpacing(
            GameObject go,
            float spacing,
            string sceneName
        )
        {
            bool anyUpdated = false;

            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                if (TryUpdatePrefabInstanceSpacing(t.gameObject, spacing, sceneName))
                {
                    anyUpdated = true;
                }
            }

            return anyUpdated;
        }

        private static bool TryUpdatePrefabInstanceSpacing(
            GameObject obj,
            float spacing,
            string sceneName
        )
        {
            var nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
            if (nearestRoot == null)
            {
                return false;
            }

            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(nearestRoot);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (
                !assetPath.EndsWith(
                    "MenuStyleListPrefab.prefab",
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return false;
            }

            var vlg = nearestRoot.GetComponentInChildren<VerticalLayoutGroup>(true);
            if (vlg == null)
            {
                Debug.LogWarning(
                    $"MenuStyleListPrefab instance in scene '{sceneName}' has no VerticalLayoutGroup child"
                );
                return false;
            }

            Undo.RecordObject(vlg, "Apply Menu Spacing");
            vlg.spacing = spacing;
            EditorUtility.SetDirty(vlg);
            return true;
        }
    }
}
#endif
