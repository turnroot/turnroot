using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Turnroot.Conversations.Mermaid.Editor
{
    /// <summary>
    /// Imports files with the .mmd and .mermaid extensions as Unity TextAssets so they can be
    /// assigned to the <see cref="Conversation.MermaidSource"/> field.
    /// </summary>
    [ScriptedImporter(1, new[] { "mmd", "mermaid" })]
    public class MermaidTextImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string text = File.ReadAllText(ctx.assetPath);
            var asset = new TextAsset(text)
            {
                name = Path.GetFileNameWithoutExtension(ctx.assetPath)
            };

            ctx.AddObjectToAsset("main", asset);
            ctx.SetMainObject(asset);
        }
    }
}
