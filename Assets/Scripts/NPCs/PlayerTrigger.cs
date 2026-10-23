using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerTrigger : MonoBehaviour
{
    public event Action Entered;

    private Collider area;
    private Transform player;
    private bool hasFired;

    private void Awake()
    {
        area = GetComponent<Collider>();
        area.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CharacterController>() != null)
            Fire();
    }

    private void Update()
    {
        if (hasFired) return;

        if (player == null)
        {
            PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement == null) return;
            player = playerMovement.transform;
        }

        if (area.bounds.Contains(player.position))
            Fire();
    }

    public void ResetTrigger()
    {
        hasFired = false;
    }

    private void Fire()
    {
        if (hasFired) return;

        hasFired = true;
        Entered?.Invoke();
    }
}
