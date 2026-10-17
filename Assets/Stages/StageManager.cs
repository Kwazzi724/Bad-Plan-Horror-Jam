using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    
    [SerializeField] private List<GameObject> stages;
    [SerializeField] private int currentStage;

    private List<GameObject> loadedStages;
    private List<bool> stageLoadedStatuses;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject);
        
        // The assumption for now is that the first stage should always be instantiated when the level starts.
        // Up for discussion though, depending on what we decide on in practice
        //Instantiate(stages[0]);
        loadedStages = new List<GameObject> { stages[0] };
        loadedStages[0].GetComponent<Stage>().CleanupReady += CleanupPreviousStage;
        
        for(int i = 0; i < stages.Count; i++)
            stageLoadedStatuses.Add(false);
        stageLoadedStatuses[0] = true;

        LoadNextStage();
    }

    public void NextStage()
    {
        currentStage++;
        LoadNextStage();
    }

    public Stage GetStageByIndex(int index)
    {
        return loadedStages[index].GetComponent<Stage>();
    }

    private void LoadNextStage()
    {
        Stage currentStageComponent = stages[currentStage].GetComponent<Stage>();
        AsyncInstantiateOperation<GameObject> instantiateOperation = InstantiateAsync(stages[currentStage + 1], null, currentStageComponent.LoadRoot.transform.position, Quaternion.identity);
        instantiateOperation.completed += OnNextStageLoaded;
    }

    private void OnNextStageLoaded(AsyncOperation obj)
    {
        AsyncInstantiateOperation<GameObject> instantiateOperation = (AsyncInstantiateOperation<GameObject>)obj;
        instantiateOperation.completed -= OnNextStageLoaded;
        
        Assert.AreEqual(instantiateOperation.Result.Length, 1);
        Assert.AreEqual(loadedStages.Count, currentStage + 1);
        
        loadedStages.Add(instantiateOperation.Result[0]);
        loadedStages.Last().GetComponent<Stage>().CleanupReady += CleanupPreviousStage;
    }

    private void CleanupPreviousStage()
    {
        Destroy(loadedStages[currentStage - 1]);
    }

    private void OnValidate()
    {
        foreach (GameObject stageObject in stages)
        {
            if (stageObject == null)
                continue;
            
            if(stageObject.GetComponent<Stage>() == null)
                Debug.LogWarning($"Stage manager list has no stage on game object {stageObject.name}");
        }
    }
}
