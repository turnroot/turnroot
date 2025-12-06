using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using UnityEngine;

public class CharacterClass : MonoBehaviour
{
    public CharacterData characterData;
    public CharacterClassData characterClassData;

    /// <summary>
    /// This does a couple things- it gets the skin colors
    /// and accent colors from a characterdata, creates a material
    /// from the shader, applies the colors to the material,
    /// and applies the material to the mesh renderer.
    /// </summary>
}
