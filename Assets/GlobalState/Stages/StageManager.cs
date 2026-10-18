using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    
    [SerializeField] private List<GameObject> stages;
    // only used for where the very first hallway spawns, everything after chains off the LoadRoots
    [SerializeField] private Transform homeAnchor; 
    // [SerializeField] private bool loopStages;

    // theres only ever two hallways in the scene: the one player's in and the one preloaded ahead
    public Stage CurrentStage { get; private set; }
    public Stage UpcomingStage { get; private set; }
    public int CurrentStageIndex { get; private set; }
    
    private int upcomingIndex = -1;

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
        // spawn the first hallway straight away at the home anchor so the player has somewhere to stand
        GameObject firstStage = Instantiate(stages[0], homeAnchor.position, homeAnchor.rotation);
        CurrentStage = firstStage.GetComponent<Stage>();
        CurrentStageIndex = 0;
        // listen to the first hallway's doors too, in case its exit door is the one that locks
        CurrentStage.DoorLocked += HandleDoorLocked;
        Debug.Log($"loaded first stage '{stages[0].name}' at the home anchor.", this);

        PreloadNextStage();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // any door locking in either loaded hallway means the player just crossed into the upcoming one
    private void HandleDoorLocked(Stage stage, Door door)
    {
        // if the locked door belongs to the hallway we're about to delete, hand it over to the new hallway so it doesn't vanish behind the player
        if (stage == CurrentStage && UpcomingStage != null)
            door.transform.SetParent(UpcomingStage.transform, true);

        NextStage();
    }

    // delete the old hallway, the one the player just walked into becomes current, then preload the next one ahead at its LoadRoot
    // nothing gets moved or teleported, hallways just chain forward
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
        PreloadNextStage();
    }

    private void PreloadNextStage()
    {
        int nextIndex = CurrentStageIndex + 1;
        if (nextIndex >= stages.Count)
        {
            // if (!loopStages)
            //     return;

            nextIndex = 0;
        }

        if (CurrentStage.LoadRoot == null)
        {
            Debug.LogError($"'{CurrentStage.name}' has no LoadRoot, can't place the next stage", CurrentStage);
            return;
        }

        // the next hallway spawns at the current hallway's LoadRoot (position and rotation), so turns and different lengths just line up
        Transform loadRoot = CurrentStage.LoadRoot.transform;
        upcomingIndex = nextIndex;

        InstantiateParameters parameters = new InstantiateParameters
        {
            worldSpace = true
        };
        AsyncInstantiateOperation<GameObject> instantiateOperation = InstantiateAsync(stages[nextIndex], 1, loadRoot.position, loadRoot.rotation, parameters, CancellationToken.None);
        instantiateOperation.completed += OnUpcomingStageLoaded;
    }

    private void OnUpcomingStageLoaded(AsyncOperation operation)
    {
        AsyncInstantiateOperation<GameObject> instantiateOperation = (AsyncInstantiateOperation<GameObject>)operation;
        instantiateOperation.completed -= OnUpcomingStageLoaded;

        GameObject instance = instantiateOperation.Result[0];
        if (this == null)
        {
            Destroy(instance);
            return;
        }

        UpcomingStage = instance.GetComponent<Stage>();
        // when any door in this hallway locks, cue to clean up the old hallway and preload the next
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