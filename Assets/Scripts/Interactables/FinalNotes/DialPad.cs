using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialPad : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private float distanceFromCamera = 0.6f;
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private Canvas canvas;

    [Header("Buttons")]
    [SerializeField] private Button[] digitButtons = new Button[10];
    [SerializeField] private Button clearButton;
    [SerializeField] private Button callButton;

    [Header("Display")]
    [SerializeField] private TMP_Text display;
    [SerializeField] private int maxDigits = 10;

    private Transform targetCamera;
    private Action<int> onDigit;
    private Action<string> onCall;
    private string number = "";
    private bool initialised;

    public bool IsOpen => targetCamera != null;

    private void Initialise()
    {
        if (initialised) return;
        initialised = true;

        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);

        for (int i = 0; i < digitButtons.Length; i++)
        {
            int digit = i;
            AddListener(digitButtons[i], () => PressDigit(digit));
        }

        AddListener(clearButton, Clear);
        AddListener(callButton, Call);
    }

    public void Open(Camera viewer, Action<int> digitPressed, Action<string> called)
    {
        gameObject.SetActive(true);
        Initialise();

        targetCamera = viewer.transform;
        onDigit = digitPressed;
        onCall = called;

        if (canvas != null)
            canvas.worldCamera = viewer;

        number = "";
        UpdateDisplay();
        FollowCamera();
    }

    public void Close()
    {
        targetCamera = null;
        onDigit = null;
        onCall = null;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        FollowCamera();
    }

    private void FollowCamera()
    {
        if (targetCamera == null) return;

        transform.position = targetCamera.position + targetCamera.forward * distanceFromCamera;
        transform.rotation = targetCamera.rotation * Quaternion.Euler(rotationOffset);
    }

    private void PressDigit(int digit)
    {
        if (!IsOpen || number.Length >= maxDigits) return;

        number += digit.ToString();
        UpdateDisplay();
        onDigit?.Invoke(digit);
    }

    private void Clear()
    {
        if (!IsOpen) return;

        number = "";
        UpdateDisplay();
    }

    private void Call()
    {
        if (!IsOpen) return;

        Action<string> called = onCall;
        string dialled = number;

        Close();
        called?.Invoke(dialled);
    }

    private void UpdateDisplay()
    {
        if (display != null)
            display.text = number;
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }
}
