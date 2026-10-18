using System;
using UnityEngine;

public class Stage : MonoBehaviour
{
    public event Action CleanupReady;

    public GameObject LoadRoot => loadRoot;
    public Door EntryDoor => entryDoor;
    [SerializeField] private GameObject loadRoot;
    [SerializeField] private Door entryDoor;// The door player walk through to get INTO this hallway, which belongs to the current hallway

    // Once the entry door locks behind the player, cleanup previous hallway
    private void OnEnable()
    {
        if (entryDoor != null)
        {
            entryDoor.Locked += SetReadyForCleanup;
        }
    }

    private void OnDisable()
    {
        if (entryDoor != null)
        {
            entryDoor.Locked -= SetReadyForCleanup;
        }
    }

    public void SetReadyForCleanup()
    {
        CleanupReady?.Invoke();
    }

    private void OnValidate()
    {
        if (loadRoot == null)
            Debug.LogError($"{gameObject.name} requires a {nameof(LoadRoot)}, since it is a stage");
    }
}