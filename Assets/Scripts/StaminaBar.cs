using UnityEngine;
using UnityEngine.UI;

// Attach to a UI Slider GameObject. Assign the player object in Inspector
// or it will be found automatically from the scene.
public class StaminaBar : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private Slider slider;
    [SerializeField] private Image fill;

    [Header("Colors")]
    [SerializeField] private Color fullColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.1f);
    [SerializeField] private float lowThreshold = 0.3f;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>();

        if (slider == null)
            slider = GetComponent<Slider>();

        if (fill == null && slider != null && slider.fillRect != null)
            fill = slider.fillRect.GetComponent<Image>();
    }

    private void Update()
    {
        if (player == null || slider == null)
            return;

        float fraction = player.MaxStamina > 0f
            ? player.CurrentStamina / player.MaxStamina
            : 0f;

        slider.value = fraction;

        if (fill != null)
            fill.color = Color.Lerp(lowColor, fullColor, fraction / lowThreshold);
    }
}
