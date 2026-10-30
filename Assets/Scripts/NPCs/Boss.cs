using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

[RequireComponent(typeof(Rigidbody))]
public class Boss : MonoBehaviour
{
    private enum EndBehaviour { Stop, Loop, PingPong, Despawn }

    [Header("Path")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private float speed = 3f;
    [SerializeField] private EndBehaviour atEnd = EndBehaviour.Stop;
    [SerializeField] private bool faceMovement = true;
    [SerializeField] private bool keepUpright = true;
    [SerializeField] private float turnSpeed = 360f;

    [Header("Sounds")]
    [SerializeField] private EventReference stepSound;
    [SerializeField] private float stepDistance = 1.5f;
    [SerializeField] private EventReference moanLoop;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private int targetIndex;
    private int direction = 1;
    private bool moving;
    private bool hasCaught;
    private float distanceSinceStep;
    private EventInstance moanInstance;

    private void Awake()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        bool hasTrigger = false;
        foreach (Collider bossCollider in GetComponentsInChildren<Collider>())
        {
            if (bossCollider.isTrigger)
                hasTrigger = true;
        }

        if (!hasTrigger)
            Debug.LogWarning($"Boss '{name}': add a collider with Is Trigger ticked so it can catch the player.", this);
    }

    private void OnEnable()
    {
        waypoints.RemoveAll(point => point == null);
        direction = 1;
        hasCaught = false;
        distanceSinceStep = 0f;
        moving = waypoints.Count > 1;

        if (!moving)
        {
            Debug.LogWarning($"Boss '{name}': needs at least 2 waypoints.", this);
            return;
        }

        transform.position = waypoints[0].position;
        targetIndex = 1;
        FaceTowards(waypoints[targetIndex].position, true);
        StartMoan();

        if (debugLogs)
            Debug.Log($"Boss '{name}': spawned at '{waypoints[0].name}', {waypoints.Count} waypoints.", this);
    }

    private void OnDisable()
    {
        StopMoan();
    }

    private void Update()
    {
        if (moanInstance.isValid())
            moanInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));

        if (!moving || IsPaused()) return;

        Vector3 before = transform.position;
        float remaining = speed * Time.deltaTime;

        while (moving && remaining > 0f)
        {
            Vector3 target = waypoints[targetIndex].position;
            float distance = Vector3.Distance(transform.position, target);

            if (distance > remaining)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, remaining);
                break;
            }

            transform.position = target;
            remaining -= distance;
            AdvanceWaypoint();
        }

        if (moving)
            FaceTowards(waypoints[targetIndex].position, false);

        UpdateSteps(Vector3.Distance(before, transform.position));
    }

    private void AdvanceWaypoint()
    {
        int next = targetIndex + direction;

        if (next >= 0 && next < waypoints.Count)
        {
            targetIndex = next;
            return;
        }

        switch (atEnd)
        {
            case EndBehaviour.Stop:
                moving = false;
                break;
            case EndBehaviour.Loop:
                targetIndex = 0;
                break;
            case EndBehaviour.PingPong:
                direction = -direction;
                targetIndex += direction;
                break;
            case EndBehaviour.Despawn:
                moving = false;
                gameObject.SetActive(false);
                break;
        }

        if (debugLogs)
            Debug.Log($"Boss '{name}': reached the last waypoint ({atEnd}).", this);
    }

    private void FaceTowards(Vector3 point, bool instant)
    {
        if (!faceMovement) return;

        Vector3 direction3 = point - transform.position;
        if (keepUpright)
            direction3.y = 0f;

        if (direction3.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(direction3.normalized, Vector3.up);
        transform.rotation = instant ? target : Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;

        Gizmos.color = Color.magenta;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.15f);

            if (i > 0 && waypoints[i - 1] != null)
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
        }
    }

    private void UpdateSteps(float moved)
    {
        distanceSinceStep += moved;
        if (distanceSinceStep < stepDistance) return;

        distanceSinceStep = 0f;
        SoundPlayer.PlayOneShot(stepSound, transform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCaught) return;
        if (other.GetComponentInParent<PlayerMovement>() == null) return;

        Caught();
    }

    public void Caught()
    {
        hasCaught = true;

        if (debugLogs)
            Debug.Log($"Boss '{name}': caught the player.", this);

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    private void StartMoan()
    {
        StopMoan();
        if (moanLoop.IsNull) return;

        try
        {
            moanInstance = RuntimeManager.CreateInstance(moanLoop);
        }
        catch (System.Exception)
        {
            Debug.LogWarning($"Boss: couldn't create {moanLoop}.", this);
            return;
        }

        moanInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        moanInstance.start();
    }

    private void StopMoan()
    {
        if (!moanInstance.isValid()) return;

        moanInstance.stop(STOP_MODE.ALLOWFADEOUT);
        moanInstance.release();
        moanInstance.clearHandle();
    }

    private static bool IsPaused()
    {
        return PauseManager.Instance != null && PauseManager.Instance.IsPaused;
    }
}