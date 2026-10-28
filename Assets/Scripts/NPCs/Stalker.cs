using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class Stalker : MonoBehaviour, IEnemy
{
    private struct SpeedSample
    {
        public float time;
        public float speed;
    }

    private enum StalkerState { Waiting, Following, Retreating }

    [Header("Parts")]
    [SerializeField] private GameObject visual;
    [SerializeField] private Transform lookTarget;

    [Header("Following")]
    [SerializeField] private float followDistance = 4f;
    [SerializeField] private float reactionDelay = 0.4f;
    [SerializeField] private float maxSpeed = 6f;

    [Header("Footsteps")]
    [SerializeField] private EventReference footstepEvent;
    [SerializeField] private float stepDistance = 1.8f;
    [SerializeField] private SoundLabel stepPace = new SoundLabel("WalkRun", "Walk");
    [SerializeField] private SoundLabel stepFloor = new SoundLabel("Floor", "Wood");

    [Header("Being Seen")]
    [SerializeField] private int emptyLooksBeforeReveal = 3;
    [SerializeField] private float lookAngle = 30f;
    [SerializeField] private float minUnseenTime = 2f;
    [SerializeField] private float offScreenMargin = 0.15f;
    [SerializeField] private LayerMask sightBlockers = ~0;

    [Header("Retreat")]
    [SerializeField] private float retreatSpeed = 2f;
    [SerializeField] private float retreatDistance = 4f;
    [SerializeField] private EventReference retreatSound;

    private readonly List<Vector3> trail = new List<Vector3>();
    private readonly List<SpeedSample> speedHistory = new List<SpeedSample>();

    private StalkerState state = StalkerState.Waiting;
    private Transform player;
    private Camera viewer;
    private Renderer[] renderers;
    private Vector3 lastPlayerPosition;
    private float distanceSinceStep;
    private int emptyLooks;
    private bool wasLookedAt;
    private float unseenTime;
    private bool revealPending;
    private bool isRevealed;

    private bool IsPaused => PauseManager.Instance != null && PauseManager.Instance.IsPaused;
    private Vector3 LookPoint => lookTarget != null ? lookTarget.position : transform.position;

    private void Awake()
    {
        if (visual == null)
            visual = gameObject;

        renderers = visual.GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
    }

    public void Begin(Transform playerTransform)
    {
        player = playerTransform;
        lastPlayerPosition = player.position;

        trail.Clear();
        trail.Add(player.position);
        speedHistory.Clear();

        emptyLooks = 0;
        revealPending = false;
        isRevealed = false;
        wasLookedAt = false;
        unseenTime = 0f;
        distanceSinceStep = stepDistance * 0.5f;

        SetVisible(false);
        state = StalkerState.Following;
    }

    private void Update()
    {
        if (state != StalkerState.Following || player == null || IsPaused) return;

        if (viewer == null)
        {
            viewer = player.GetComponentInChildren<Camera>();

            if (viewer == null)
                viewer = Camera.main;
        }

        RecordPlayer();
        Follow();
        FacePlayer();
        CheckSeen();
    }

    private void RecordPlayer()
    {
        Vector3 delta = player.position - lastPlayerPosition;
        delta.y = 0f;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        lastPlayerPosition = player.position;

        speedHistory.Add(new SpeedSample { time = Time.time, speed = speed });
        speedHistory.RemoveAll(sample => sample.time < Time.time - reactionDelay - 1f);

        if (Vector3.Distance(trail[trail.Count - 1], player.position) > 0.25f)
            trail.Add(player.position);
    }

    private float DelayedSpeed()
    {
        float delayedTime = Time.time - reactionDelay;
        float speed = 0f;

        foreach (SpeedSample sample in speedHistory)
        {
            if (sample.time > delayedTime) break;
            speed = sample.speed;
        }

        return speed;
    }

    private float PathLengthToPlayer()
    {
        float length = 0f;
        Vector3 previous = transform.position;

        foreach (Vector3 point in trail)
        {
            length += Vector3.Distance(previous, point);
            previous = point;
        }

        return length + Vector3.Distance(previous, player.position);
    }

    private void Follow()
    {
        float budget = Mathf.Min(DelayedSpeed(), maxSpeed) * Time.deltaTime;
        float slack = PathLengthToPlayer() - followDistance;
        budget = Mathf.Min(budget, Mathf.Max(0f, slack));

        Vector3 start = transform.position;

        while (budget > 0f && trail.Count > 0)
        {
            Vector3 next = trail[0];
            float distance = Vector3.Distance(transform.position, next);

            if (distance <= budget)
            {
                transform.position = next;
                budget -= distance;

                if (trail.Count > 1)
                    trail.RemoveAt(0);
                else
                    break;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, next, budget);
                budget = 0f;
            }
        }

        StepSounds(start, transform.position);
    }

    private void StepSounds(Vector3 from, Vector3 to)
    {
        Vector3 moved = to - from;
        moved.y = 0f;

        if (moved.sqrMagnitude < 0.0000001f)
        {
            distanceSinceStep = stepDistance * 0.5f;
            return;
        }

        distanceSinceStep += moved.magnitude;

        if (distanceSinceStep >= stepDistance)
        {
            distanceSinceStep = 0f;
            SoundPlayer.PlayOneShot(footstepEvent, transform.position, stepPace, stepFloor);
        }
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void CheckSeen()
    {
        if (viewer == null) return;

        bool lookedAt = IsLookedAt();

        if (lookedAt && !wasLookedAt)
        {
            if (isRevealed)
            {
                StartCoroutine(Retreat());
                return;
            }

            if (unseenTime >= minUnseenTime)
            {
                emptyLooks++;

                if (emptyLooks >= emptyLooksBeforeReveal)
                    revealPending = true;
            }
        }

        unseenTime = lookedAt ? 0f : unseenTime + Time.deltaTime;
        wasLookedAt = lookedAt;

        if (revealPending && !IsOnScreen())
        {
            revealPending = false;
            isRevealed = true;
            SetVisible(true);
        }
    }

    private bool IsLookedAt()
    {
        Vector3 toTarget = LookPoint - viewer.transform.position;

        if (Vector3.Angle(viewer.transform.forward, toTarget) > lookAngle)
            return false;

        return HasLineOfSight();
    }

    private bool IsOnScreen()
    {
        Vector3 viewport = viewer.WorldToViewportPoint(LookPoint);

        if (viewport.z <= 0f) return false;

        bool insideFrame = viewport.x > -offScreenMargin && viewport.x < 1f + offScreenMargin
                           && viewport.y > -offScreenMargin && viewport.y < 1f + offScreenMargin;

        return insideFrame && HasLineOfSight();
    }

    private bool HasLineOfSight()
    {
        Vector3 from = viewer.transform.position;
        Vector3 to = LookPoint;

        if (Physics.Linecast(from, to, out RaycastHit hit, sightBlockers, QueryTriggerInteraction.Ignore))
            return hit.transform.IsChildOf(transform);

        return true;
    }

    private IEnumerator Retreat()
    {
        state = StalkerState.Retreating;
        SoundPlayer.PlayOneShot(retreatSound, transform.position);

        Vector3 away = transform.position - player.position;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward;

        float travelled = 0f;
        distanceSinceStep = stepDistance * 0.5f;

        while (travelled < retreatDistance)
        {
            if (!IsPaused)
            {
                Vector3 start = transform.position;
                float step = Mathf.Min(retreatSpeed * Time.deltaTime, retreatDistance - travelled);

                transform.position += away * step;
                travelled += step;

                if (player != null)
                    FacePlayer();

                StepSounds(start, transform.position);
                SetAlpha(1f - travelled / retreatDistance);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer meshRenderer in renderers)
            meshRenderer.enabled = visible;
    }

    private void SetAlpha(float alpha)
    {
        foreach (Renderer meshRenderer in renderers)
        {
            foreach (Material material in meshRenderer.materials)
            {
                string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                if (colorProperty == null) continue;

                Color color = material.GetColor(colorProperty);
                color.a = alpha;
                material.SetColor(colorProperty, color);
            }
        }
    }
}