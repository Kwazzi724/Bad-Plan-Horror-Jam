using System;
using UnityEngine;

public class Stage : MonoBehaviour
{
    public event Action CleanupReady;
    // fires with this stage and the door that just locked, so the manager knows what happened and where
    public event Action<Stage, Door> DoorLocked;

    public GameObject LoadRoot => loadRoot;

    // Where the next hallway gets spawned, so stick it at this hallway's exit
    [SerializeField] private GameObject loadRoot;

    // grabs every door inside this hallway automatically, so it doesn't matter if a door is set up as an entry or an exit
    private Door[] doors;

    private void Awake()
    {
        doors = GetComponentsInChildren<Door>(true);

        foreach (Door door in doors)
        {
            door.Locked += () => HandleDoorLocked(door);
        }

        Debug.Log($"{name}: found {doors.Length} door(s).", this);
    }

    private void HandleDoorLocked(Door door)
    {
        Debug.Log($"{name}: door '{door.name}' locked.", this);
        DoorLocked?.Invoke(this, door);
        SetReadyForCleanup();
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