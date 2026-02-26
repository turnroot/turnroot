#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using Turnroot.Utilities;
using XNode;

namespace Turnroot.Skills.Nodes.Editor
{
    /// <summary>
    /// Small repair helper: ensure every Node instance in a SkillGraph is persisted
    /// as a sub-asset of the graph asset (fixes fileID==0 / missing-connections cases).
    /// Usage: select a SkillGraph asset in Project window -> Tools / Skill Graphs / Repair Selected Graph
    /// </summary>
    public static class SkillGraphRepair
    {
        [MenuItem("Tools/Turnroot/Skill Graphs/Repair Selected Graph", priority = 2000)]
        public static void RepairSelectedGraph()
        {
            var graph = Selection.activeObject as NodeGraph;
            if (graph == null)
            {
                "SkillGraphRepair: select a SkillGraph asset in the Project window first.".LogWarning();
                return;
            }

            var graphPath = AssetDatabase.GetAssetPath(graph);
            if (string.IsNullOrEmpty(graphPath))
            {
                "SkillGraphRepair: selected object is not an on-disk asset.".LogWarning();
                return;
            }

            int added = 0;
            foreach (var node in graph.nodes?.ToList() ?? Enumerable.Empty<Node>())
            {
                if (node == null)
                {
                    continue;
                }

                var nodePath = AssetDatabase.GetAssetPath(node);
                if (string.IsNullOrEmpty(nodePath) || nodePath != graphPath)
                {
                    AssetDatabase.AddObjectToAsset(node, graphPath);
                    EditorUtility.SetDirty(node);
                    added++;
                }
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(graphPath, ImportAssetOptions.ForceUpdate);
                $"SkillGraphRepair: attached {added} node(s) to '{graphPath}' and saved.".LogInfo();
            }
            else
            {
                "SkillGraphRepair: no orphan nodes found. If nodes still show 'Type mismatch' check the Console for missing scripts or restore the asset from VCS.".LogInfo();
            }
        }

        [MenuItem("Tools/Turnroot/Skill Graphs/Repair Selected Graph", true)]
        private static bool RepairSelectedGraph_Validate()
        {
            return Selection.activeObject is NodeGraph;
        }
    }

    /// <summary>
    /// Automatically repair SkillGraph assets on import by attaching node subassets
    /// that are referenced by the in-memory graph but not persisted to the .asset file.
    /// This prevents 'Type mismatch' / fileID: 0 cases after import/VCS merges.
    /// </summary>
    internal class SkillGraphImportPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            foreach (var path in importedAssets)
            {
                if (!path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var graph = AssetDatabase.LoadAssetAtPath<SkillGraph>(path);
                if (graph == null)
                {
                    continue;
                }

                RepairGraphIfNeeded(graph, path);
            }
        }

        private static void RepairGraphIfNeeded(SkillGraph graph, string graphPath)
        {
            int added = 0;
            foreach (var node in graph.nodes ?? Enumerable.Empty<Node>())
            {
                if (node == null)
                {
                    continue;
                }

                var nodePath = AssetDatabase.GetAssetPath(node);
                if (
                    string.IsNullOrEmpty(nodePath)
                    || !string.Equals(
                        nodePath,
                        graphPath,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    AssetDatabase.AddObjectToAsset(node, graphPath);
                    EditorUtility.SetDirty(node);
                    added++;
                }
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(graphPath, ImportAssetOptions.ForceUpdate);
                $"SkillGraphImportPostprocessor: attached {added} node(s) to '{graphPath}' on import.".LogInfo();
            }
        }
    }
}
#endif
