using UnityEngine;
using UnityEngine.UI;

public class TorchBatteryUI : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;

    [Header("Colours")]
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color lowColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color criticalColor = Color.red;

    [Header("Thresholds")]
    [SerializeField, Range(0f, 1f)] private float lowStart = 0.2f;
    [SerializeField, Range(0f, 1f)] private float criticalStart = 0.05f;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;

        if (fillImage == null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();
    }

    private void Update()
    {
        Torch torch = Torch.Active;
        if (torch == null) return;

        float battery = torch.BatteryPercent;
        slider.value = battery;

        if (fillImage != null)
            fillImage.color = GetColor(battery);
    }

    private Color GetColor(float battery)
    {
        if (battery >= lowStart) return fullColor;
        if (battery <= criticalStart) return criticalColor;

        float t = Mathf.InverseLerp(lowStart, criticalStart, battery);

        if (t < 0.5f)
            return Color.Lerp(fullColor, lowColor, t * 2f);

        return Color.Lerp(lowColor, criticalColor, (t - 0.5f) * 2f);
    }
}
