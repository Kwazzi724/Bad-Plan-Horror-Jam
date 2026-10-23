using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClockMinigame : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private float distanceFromCamera = 0.6f;
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private Canvas canvas;

    [Header("Dials")]
    [SerializeField] private Transform hourDial;
    [SerializeField] private Transform minuteDial;
    [SerializeField] private Vector3 dialAxis = Vector3.right;
    [SerializeField] private float dialTurnDuration = 0.15f;

    [Header("Text")]
    [SerializeField] private TMP_Text hourText;
    [SerializeField] private TMP_Text minuteText;

    [Header("Buttons")]
    [SerializeField] private Button hourUpButton;
    [SerializeField] private Button hourDownButton;
    [SerializeField] private Button minuteUpButton;
    [SerializeField] private Button minuteDownButton;
    [SerializeField] private Button applyButton;

    private Transform targetCamera;
    private Action<int, int> onApplied;
    private int hour;
    private int minute;
    private Quaternion hourDialRest;
    private Quaternion minuteDialRest;
    private Tween hourTween;
    private Tween minuteTween;
    private bool initialised;

    public bool IsOpen => targetCamera != null;

    private void Initialise()
    {
        if (initialised) return;
        initialised = true;

        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);

        if (hourDial != null)
            hourDialRest = hourDial.localRotation;

        if (minuteDial != null)
            minuteDialRest = minuteDial.localRotation;

        AddListener(hourUpButton, () => ChangeHour(1));
        AddListener(hourDownButton, () => ChangeHour(-1));
        AddListener(minuteUpButton, () => ChangeMinute(1));
        AddListener(minuteDownButton, () => ChangeMinute(-1));
        AddListener(applyButton, Apply);
    }

    public void Open(Camera viewer, int startHour, int startMinute, Action<int, int> applied)
    {
        gameObject.SetActive(true);
        Initialise();

        targetCamera = viewer.transform;
        onApplied = applied;

        if (canvas != null)
            canvas.worldCamera = viewer;

        hour = Wrap(startHour, 24);
        minute = Wrap(startMinute, 60);
        UpdateText();
        TurnDial(hourDial, hourDialRest, hour * 360f / 24f, ref hourTween, true);
        TurnDial(minuteDial, minuteDialRest, minute * 360f / 60f, ref minuteTween, true);

        FollowCamera();
    }

    public void Close()
    {
        hourTween?.Kill();
        minuteTween?.Kill();
        targetCamera = null;
        onApplied = null;
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

    private void ChangeHour(int delta)
    {
        hour = Wrap(hour + delta, 24);
        UpdateText();
        TurnDial(hourDial, hourDialRest, hour * 360f / 24f, ref hourTween, false);
    }

    private void ChangeMinute(int delta)
    {
        minute = Wrap(minute + delta, 60);
        UpdateText();
        TurnDial(minuteDial, minuteDialRest, minute * 360f / 60f, ref minuteTween, false);
    }

    private void Apply()
    {
        if (!IsOpen) return;

        Action<int, int> applied = onApplied;
        int appliedHour = hour;
        int appliedMinute = minute;

        Close();
        applied?.Invoke(appliedHour, appliedMinute);
    }

    private void UpdateText()
    {
        if (hourText != null)
            hourText.text = hour.ToString("D2");

        if (minuteText != null)
            minuteText.text = minute.ToString("D2");
    }

    private void TurnDial(Transform dial, Quaternion rest, float angle, ref Tween tween, bool instant)
    {
        if (dial == null) return;

        tween?.Kill();
        Quaternion target = rest * Quaternion.AngleAxis(angle, dialAxis);

        if (instant || dialTurnDuration <= 0f)
        {
            dial.localRotation = target;
            tween = null;
            return;
        }

        tween = dial.DOLocalRotateQuaternion(target, dialTurnDuration).SetEase(Ease.OutBack).SetLink(dial.gameObject);
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static int Wrap(int value, int range)
    {
        return ((value % range) + range) % range;
    }
}
