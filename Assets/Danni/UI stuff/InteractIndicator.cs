using DG.Tweening;
using UnityEngine;

public class InteractIndicator : MonoBehaviour
{
    [Header("Pop")]
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    [Header("Bob")]
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float bobDuration = 0.6f;

    [Header("Billboard")]
    [SerializeField] private bool yawOnly;

    private Vector3 baseScale;
    private Vector3 baseLocalPosition;
    private Tween scaleTween;
    private Tween bobTween;
    private Camera targetCamera;
    private bool isShown;

    private void Awake()
    {
        baseScale = transform.localScale;
        baseLocalPosition = transform.localPosition;
        transform.localScale = Vector3.zero;
    }

    private void OnDisable()
    {
        KillTweens();
        isShown = false;
        transform.localScale = Vector3.zero;
        transform.localPosition = baseLocalPosition;
    }

    public void Show()
    {
        if (isShown) return;
        isShown = true;

        KillTweens();
        transform.localPosition = baseLocalPosition;

        scaleTween = transform.DOScale(baseScale, popDuration).SetEase(Ease.OutBack).SetLink(gameObject);
        bobTween = transform.DOLocalMoveY(baseLocalPosition.y + bobHeight, bobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    public void Hide()
    {
        if (!isShown) return;
        isShown = false;

        KillTweens();

        scaleTween = transform.DOScale(Vector3.zero, hideDuration)
            .SetEase(Ease.InBack)
            .SetLink(gameObject)
            .OnComplete(() => transform.localPosition = baseLocalPosition);
    }

    private void LateUpdate()
    {
        if (transform.localScale == Vector3.zero) return;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        Vector3 awayFromCamera = transform.position - targetCamera.transform.position;

        if (yawOnly)
            awayFromCamera.y = 0f;

        if (awayFromCamera.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(awayFromCamera);
    }

    private void KillTweens()
    {
        scaleTween?.Kill();
        bobTween?.Kill();
        scaleTween = null;
        bobTween = null;
    }
}
