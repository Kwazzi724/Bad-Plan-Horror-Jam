using System;
using UnityEngine;

public class Stage : MonoBehaviour
{
    public event Action CleanupReady;
    public event Action<Stage, Door> DoorLocked;

    public GameObject LoadRoot => loadRoot;

    [SerializeField] private GameObject loadRoot;

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

    public int IndexOfDoor(Door door)
    {
        return doors != null ? System.Array.IndexOf(doors, door) : -1;
    }

    public Door DoorAt(int index)
    {
        return doors != null && index >= 0 && index < doors.Length ? doors[index] : null;
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