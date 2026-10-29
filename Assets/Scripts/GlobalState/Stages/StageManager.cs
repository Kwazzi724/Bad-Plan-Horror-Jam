using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    public event Action<Stage> StageEntered;
    public event Action<Stage> StageReset;
    
    [SerializeField] private List<GameObject> stages;
    [SerializeField] private Transform homeAnchor; 

    public Stage CurrentStage { get; private set; }
    public Stage UpcomingStage { get; private set; }
    public int CurrentStageIndex { get; private set; }
    
    private int upcomingIndex = -1;
    private int loadVersion;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (homeAnchor == null)
            homeAnchor = transform;

        if (stages == null || stages.Count == 0)
        {
            Debug.LogError("the stages list is empty", this);
            return;
        }
        GameObject firstStage = Instantiate(stages[0], homeAnchor.position, homeAnchor.rotation);
        CurrentStage = firstStage.GetComponent<Stage>();
        CurrentStageIndex = 0;
        CurrentStage.DoorLocked += HandleDoorLocked;
        Debug.Log($"loaded first stage '{stages[0].name}' at the home anchor.", this);

        PreloadNextStage();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void HandleDoorLocked(Stage stage, Door door)
    {
        if (stage == CurrentStage && UpcomingStage != null)
            door.transform.SetParent(UpcomingStage.transform, true);

        NextStage();
    }

    public void NextStage()
    {
        if (UpcomingStage == null)
        {
            Debug.LogWarning("NextStage called but the upcoming stage hasn't finished loading yet", this);
            return;
        }

        Destroy(CurrentStage.gameObject);

        CurrentStage = UpcomingStage;
        CurrentStageIndex = upcomingIndex;
        UpcomingStage = null;
        Debug.Log($"now in stage {CurrentStageIndex} '{CurrentStage.name}'", this);
        StageEntered?.Invoke(CurrentStage);
        PreloadNextStage();
    }

    public Stage ResetStages()
    {
        if (CurrentStage == null) return null;

        Stage oldStage = CurrentStage;
        Vector3 position = oldStage.transform.position;
        Quaternion rotation = oldStage.transform.rotation;

        List<int> lockedDoorIndices = new List<int>();
        List<Door> inheritedDoors = new List<Door>();

        foreach (Door door in oldStage.GetComponentsInChildren<Door>(true))
        {
            if (!door.WasCrossed) continue;

            int index = oldStage.IndexOfDoor(door);

            if (index >= 0)
            {
                lockedDoorIndices.Add(index);
            }
            else
            {
                door.transform.SetParent(null, true);
                inheritedDoors.Add(door);
            }
        }

        oldStage.DoorLocked -= HandleDoorLocked;
        Destroy(oldStage.gameObject);

        if (UpcomingStage != null)
        {
            UpcomingStage.DoorLocked -= HandleDoorLocked;
            Destroy(UpcomingStage.gameObject);
            UpcomingStage = null;
        }

        loadVersion++;

        GameObject freshStage = Instantiate(stages[CurrentStageIndex], position, rotation);
        CurrentStage = freshStage.GetComponent<Stage>();
        CurrentStage.DoorLocked += HandleDoorLocked;

        foreach (int index in lockedDoorIndices)
        {
            Door door = CurrentStage.DoorAt(index);
            if (door != null)
                door.ForceLock();
        }

        foreach (Door door in inheritedDoors)
            door.transform.SetParent(CurrentStage.transform, true);

        Debug.Log($"reset stage {CurrentStageIndex} '{CurrentStage.name}'", this);
        PreloadNextStage();
        StageReset?.Invoke(CurrentStage);
        return CurrentStage;
    }

    private void PreloadNextStage()
    {
        int nextIndex = CurrentStageIndex + 1;
        if (nextIndex >= stages.Count)
        {
            nextIndex = 0;
        }

        if (CurrentStage.LoadRoot == null)
        {
            Debug.LogError($"'{CurrentStage.name}' has no LoadRoot, can't place the next stage", CurrentStage);
            return;
        }

        Transform loadRoot = CurrentStage.LoadRoot.transform;
        upcomingIndex = nextIndex;

        InstantiateParameters parameters = new InstantiateParameters
        {
            worldSpace = true
        };
        AsyncInstantiateOperation<GameObject> instantiateOperation = InstantiateAsync(stages[nextIndex], 1, loadRoot.position, loadRoot.rotation, parameters, CancellationToken.None);
        int version = loadVersion;
        instantiateOperation.completed += operation => OnUpcomingStageLoaded(operation, version);
    }

    private void OnUpcomingStageLoaded(AsyncOperation operation, int version)
    {
        AsyncInstantiateOperation<GameObject> instantiateOperation = (AsyncInstantiateOperation<GameObject>)operation;

        GameObject instance = instantiateOperation.Result[0];
        if (this == null || version != loadVersion)
        {
            Destroy(instance);
            return;
        }

        UpcomingStage = instance.GetComponent<Stage>();
        UpcomingStage.DoorLocked += HandleDoorLocked;
        Debug.Log($"preloaded stage {upcomingIndex} '{instance.name}'", this);
    }

    private void OnValidate()
    {
        if (stages == null) return;

        foreach (GameObject stageObject in stages)
        {
            if (stageObject == null)
                continue;

            if (stageObject.GetComponent<Stage>() == null)
                Debug.LogWarning($"Stage manager list has no stage on game object {stageObject.name}");
        }
    }
}