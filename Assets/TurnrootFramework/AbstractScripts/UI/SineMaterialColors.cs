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
            // sine between startColor.r,g,b,a and endColor.r,g,b,a
            for (int i = 0; i < 4; i++)
            {
                float sineValue = (Mathf.Sin(Time.time * Speed) + 1) / 2; // Normalize sine to [0,1]
                float newValue = Mathf.Lerp(startColor[i], endColor[i], sineValue);
                Color color = TargetMaterial.color;
                color[i] = newValue;
                TargetMaterial.color = color;
            }
        }
    }
}
