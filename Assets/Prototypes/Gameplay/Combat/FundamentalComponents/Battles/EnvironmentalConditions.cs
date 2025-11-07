using UnityEngine;

[CreateAssetMenu(
    fileName = "EnvironmentalConditions",
    menuName = "Gameplay/Combat/Environmental Conditions"
)]
public class EnvironmentalConditions : ScriptableObject
{
    public bool IsNight;
    public bool IsRaining;
    public bool IsFoggy;
    public bool IsDesert;
    public bool IsSnowing;
    public bool IsIndoors;
}
