using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Utilities.Editor
{
    public static class TurnrootPackageInstaller
    {
        private static readonly Dictionary<string, string> RequiredPackages = new()
        {
            { "com.github.siccity.xnode", "https://github.com/siccity/xNode.git" },
            {
                "com.dbrizov.naughtyattributes",
                "https://github.com/dbrizov/NaughtyAttributes.git#upm"
            },
            {
                "com.coffee.ui-effect",
                "https://github.com/mob-sakai/UIEffect.git?path=Packages/src"
            },
        };

        [MenuItem("Tools/Turnroot/Install Required Packages")]
        public static void InstallRequiredPackages()
        {
            string manifestPath = Path.GetFullPath("Packages/manifest.json");

            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[Turnroot] Could not find Packages/manifest.json");
                return;
            }

            string contents = File.ReadAllText(manifestPath);
            var added = new List<string>();

            foreach (var package in RequiredPackages)
            {
                if (contents.Contains(package.Key))
                    continue;

                // Insert before the closing brace of the dependencies block
                string insertion = $"    \"{package.Key}\": \"{package.Value}\",\n";
                int dependenciesIndex = contents.IndexOf("\"dependencies\"");
                int openBrace = contents.IndexOf('{', dependenciesIndex);
                contents = contents.Insert(openBrace + 1, "\n" + insertion);
                added.Add(package.Key);
            }

            if (added.Count == 0)
            {
                Debug.Log("[Turnroot] All required packages are already in the manifest.");
                EditorUtility.DisplayDialog(
                    "Turnroot",
                    "All required packages are already installed.",
                    "OK"
                );
                return;
            }

            File.WriteAllText(manifestPath, contents);
            Debug.Log(
                $"[Turnroot] Added {added.Count} package(s) to manifest: {string.Join(", ", added)}"
            );
            EditorUtility.DisplayDialog(
                "Turnroot",
                $"Added {added.Count} package(s) to manifest:\n\n{string.Join("\n", added)}\n\nUnity will now resolve packages.",
                "OK"
            );

            AssetDatabase.Refresh();
            UnityEditor.PackageManager.Client.Resolve();
        }
    }
}
