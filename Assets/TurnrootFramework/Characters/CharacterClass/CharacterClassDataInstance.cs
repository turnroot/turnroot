using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Serialization;
using UnityEngine;

public class CharacterClassDataInstance : IPostDeserialize
{
    public CharacterData characterData;
    public CharacterClassData classData;
    public MeshRenderer meshRenderer;
    private Material materialInstance;

    public bool Initialize()
    {
        if (
            characterData == null
            || meshRenderer == null
            || classData == null
            || classData.ShaderGraph == null
        )
        {
            return false;
        }

        materialInstance = new Material(classData.ShaderGraph);
        meshRenderer.material = materialInstance;

        materialInstance.SetColor("_Skin_Color", characterData.SkinColor);
        materialInstance.SetColor("_Accent_Color_1", characterData.AccentColor1);
        materialInstance.SetColor("_Accent_Color_2", characterData.AccentColor2);
        materialInstance.SetColor("_Accent_Color_3", characterData.AccentColor3);
        materialInstance.SetTexture("_Base", classData.Base);
        materialInstance.SetTexture("_MSE", classData.MSE);
        materialInstance.SetTexture("_Tint_Mask", classData.TintMask);

        return true;
    }

    public void OnAfterDeserialize() { }
}
