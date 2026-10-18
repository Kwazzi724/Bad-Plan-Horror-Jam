using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    
    [SerializeField] private List<GameObject> stages;
    [SerializeField] private Transform homeAnchor; 
    // [SerializeField] private bool loopStages;

    // theres only ever two hallways in the scene: the one player's in and the one preloaded ahead
    public Stage CurrentStage { get; private set; }
    public Stage UpcomingStage { get; private set; }
    public int CurrentStageIndex { get; private set; }
    
    private Transform traveller;
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
        // spawn the first hallway straight away at slot A so the player has somewhere to stand
        GameObject firstStage = Instantiate(stages[0], homeAnchor.position, homeAnchor.rotation);
        CurrentStage = firstStage.GetComponent<Stage>();
        CurrentStageIndex = 0;
        Debug.Log("loaded first stage '{stages[0].name}' at the home anchor.", this);

        PreloadNextStage();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    public void SetTraveller(Transform newTraveller)
    {
        traveller = newTraveller;
    }

    // ping-pong load the current and next stages: delete the old hallway, pull the new one back to slot A, then preload the next one ahead
    public void NextStage()
    {
        if (UpcomingStage == null)
            return;

        UpcomingStage.CleanupReady -= NextStage;

        Destroy(CurrentStage.gameObject);
        SnapToHome(UpcomingStage);

        CurrentStage = UpcomingStage;
        CurrentStageIndex = upcomingIndex;
        UpcomingStage = null;
        Debug.Log("now in stage {CurrentStageIndex} '{CurrentStage.name}', snapped to the default anchor", this);
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
            return;

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
        // when entry door locks, cue to swap hallway
        UpcomingStage.CleanupReady += NextStage;
    }

    // moves the hallway back to slot A and offset player
    private void SnapToHome(Stage stage)
    {
        Transform stageTransform = stage.transform;
        Vector3 oldStagePosition = stageTransform.position;
        Quaternion rotationDelta = homeAnchor.rotation * Quaternion.Inverse(stageTransform.rotation);

        stageTransform.SetPositionAndRotation(homeAnchor.position, homeAnchor.rotation);

        if (traveller != null)
        {
            Vector3 offset = traveller.position - oldStagePosition;
            Vector3 newPosition = homeAnchor.position + rotationDelta * offset;
            Quaternion newRotation = rotationDelta * traveller.rotation;

            // switch off charactercontroller while teleport
            CharacterController controller = traveller.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;

            if (controller != null)
            {
                controller.enabled = false;
            }

            traveller.SetPositionAndRotation(newPosition, newRotation);

            if (controller != null)
            {
                controller.enabled = controllerWasEnabled;
            }
        }
        Physics.SyncTransforms();
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