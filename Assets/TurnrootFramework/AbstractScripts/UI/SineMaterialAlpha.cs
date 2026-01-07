using UnityEngine;

public class SineMaterialAlpha : MonoBehaviour
{
    public float Speed = 1.0f;
    public Material TargetMaterial;
    private void Update()
    {
        if (TargetMaterial != null)
        {
            float alpha = (Mathf.Sin(Time.time * Speed) + 1.0f) / 2.0f; // Normalize to 0-1
            Color color = TargetMaterial.color;
            color.a = alpha;
            TargetMaterial.color = color;
        }
    }
}
