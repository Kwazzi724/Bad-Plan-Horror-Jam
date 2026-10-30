using System;
using System.Collections;
using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Pickup))]
public class Cloth : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Collider detectionZone;

    [Header("Held")]
    [SerializeField] private Vector3 heldRotation = new Vector3(90f, 0f, 0f);

    [Header("Wipe")]
    [SerializeField] private float wipeDuration = 2f;
    [SerializeField] private float wipeSpeed = 2.5f;
    [SerializeField] private float wipeAngle = 25f;
    [SerializeField] private float wipeDistance = 0.12f;
    [SerializeField] private float wipeReach = 0.1f;
    [SerializeField] private float easeDuration = 0.2f;

    [Header("Sounds")]
    [SerializeField] private EventReference wipeSound;

    private Pickup pickup;
    private Stain target;
    private Coroutine wipeRoutine;

    public Stain Target => target;
    public bool IsWiping => wipeRoutine != null;

    private Quaternion HeldRotation => Quaternion.Euler(heldRotation);

    private void Awake()
    {
        pickup = GetComponent<Pickup>();

        if (detectionZone == null)
        {
            foreach (Collider child in GetComponentsInChildren<Collider>(true))
            {
                if (child.isTrigger)
                {
                    detectionZone = child;
                    break;
                }
            }
        }

        if (detectionZone != null)
            detectionZone.isTrigger = true;
        else
            Debug.LogWarning($"Cloth '{name}' has no trigger collider to detect stains with.", this);
    }

    private void OnDisable()
    {
        SetTarget(null);
    }

    private void Update()
    {
        if (IsWiping) return;

        SetTarget(pickup.IsHeld ? FindStain() : null);
    }

    private void LateUpdate()
    {
        if (IsWiping || !pickup.IsHeld) return;

        transform.localRotation = HeldRotation;
    }

    public bool Wipe(Action onComplete)
    {
        if (IsWiping || target == null) return false;

        wipeRoutine = StartCoroutine(WipeRoutine(target, onComplete));
        return true;
    }

    private Stain FindStain()
    {
        if (detectionZone == null || !detectionZone.enabled || !detectionZone.gameObject.activeInHierarchy) return null;

        Vector3 zoneCenter = detectionZone.bounds.center;
        Stain best = null;
        float bestDistance = float.MaxValue;

        foreach (Stain stain in Stain.Active)
        {
            if (stain == null || stain.IsClean) continue;

            Vector3 center = stain.Center;
            Vector3 closest = detectionZone.ClosestPoint(center);
            float radius = stain.DetectRadius;

            if ((closest - center).sqrMagnitude > radius * radius) continue;

            float distance = (center - zoneCenter).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = stain;
            }
        }

        return best;
    }

    private void SetTarget(Stain stain)
    {
        if (stain == target) return;

        if (target != null)
            target.SetHighlighted(false);

        target = stain;

        if (target != null)
            target.SetHighlighted(true);
    }

    private IEnumerator WipeRoutine(Stain stain, Action onComplete)
    {
        SoundPlayer.PlayOneShot(wipeSound, stain.Center);

        float elapsed = 0f;

        while (elapsed < wipeDuration)
        {
            elapsed += Time.deltaTime;

            float easeIn = easeDuration > 0f ? Mathf.Clamp01(elapsed / easeDuration) : 1f;
            float easeOut = easeDuration > 0f ? Mathf.Clamp01((wipeDuration - elapsed) / easeDuration) : 1f;
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.Min(easeIn, easeOut));
            float wave = Mathf.Sin(elapsed * wipeSpeed * Mathf.PI * 2f);

            transform.localPosition = new Vector3(wave * wipeDistance, 0f, wipeReach) * weight;
            transform.localRotation = Quaternion.Euler(0f, 0f, -wave * wipeAngle * weight) * HeldRotation;
            yield return null;
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = HeldRotation;

        stain.Clean();
        target = null;
        wipeRoutine = null;
        onComplete?.Invoke();
    }
}