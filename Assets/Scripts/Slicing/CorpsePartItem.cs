using UnityEngine;

public enum CorpsePartType
{
    Humanoid_Arm,
    Humanoid_Leg,
    Humanoid_Head,
    Humanoid_Torso,
    Other
}

public class CorpsePartItem : MonoBehaviour
{
    [Header("Identification")]
    public string partId;            // e.g. "orc_left_arm"
    public string displayName;       // e.g. "Orc Left Arm"
    public string creatureTypeId;    // e.g. "Orc"
    public CorpsePartType partType = CorpsePartType.Other;

    [Header("Economy")]
    public float baseSellValue = 10f;     // raw value before quality etc.

    [Header("Meta (for later systems)")]
    public float weightKg = 5f;
    public bool isFlesh = true;          // can influence gore, rot, etc.
}
