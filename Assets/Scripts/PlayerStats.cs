using UnityEngine;

/// Central data model for HUD. Place on the Player GameObject alongside PlayerMovement.
public class PlayerStats : MonoBehaviour
{
    [Header("Vitals")]
    public float maxHp      = 2680f;
    public float currentHp  = 2350f;
    public float maxMana    = 1500f;
    public float currentMana = 870f;

    [Header("Corruption")]
    [Range(0f, 100f)] public float corruption = 42f;

    [Header("Progression")]
    public int   leyer = 18;
    public float pd    = 127f;

    [Header("Combat Stats")]
    public float damage  = 187f;
    public float defence = 45f;

    [Header("Depth")]
    public float depthMeters   = 87f;
    [Range(0f, 100f)]
    public float depthPressure = 42f;
    public string depthTier  = "II";
    public string depthZone  = "KATAKUMBY";

    [Header("Mutators")]
    public string[] mutators = { "MGŁA KRWI", "ECHO", "TOKSYNA" };

    // ---- Forwarded from PlayerMovement ----
    public float CurrentStamina => movement != null ? movement.CurrentStamina : 0f;
    public float MaxStamina     => movement != null ? movement.MaxStamina     : 100f;
    public float Speed          => movement != null ? movement.runSpeed       : 5f;
    public int   ComboStep      => movement != null ? movement.ComboStep      : 0;
    public int   ComboMaxSteps  => movement != null ? movement.ComboMaxSteps  : 3;
    public float ComboWindowEnd => movement != null ? movement.ComboWindowEnd : 0f;

    private PlayerMovement movement;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>()
                ?? FindFirstObjectByType<PlayerMovement>();
    }
}
