using UnityEngine;
using UnityEngine.UI;

public class ButtonHoverAll : MonoBehaviour
{
    private void Awake()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<ButtonHover>() == null)
                button.gameObject.AddComponent<ButtonHover>();
        }
    }
}
