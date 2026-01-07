using UnityEngine;

public class SineMaterialColors : MonoBehaviour
{
    public float Speed = 1.0f;
    public Material TargetMaterial;

    public Color startColor;
    public Color endColor;

    private void OnDestroy()
    {
        TargetMaterial.color = startColor;
    }

    private void Update()
    {
        if (TargetMaterial != null)
        {
            float sineValue = (Mathf.Sin(Time.time * Speed) + 1) / 2; // Normalize sine to [0,1]
            Color newColor = Color.Lerp(startColor, endColor, sineValue);
            TargetMaterial.color = newColor;
        }
    }
}
