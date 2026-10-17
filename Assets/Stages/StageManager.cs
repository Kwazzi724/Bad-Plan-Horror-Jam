using System;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    
    [SerializeField] private List<GameObject> stages;
    [SerializeField] private int currentStage;

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
        LoadNextStage();
    }

    public void NextStage()
    {
        currentStage++;
        LoadNextStage();
    }

    private void LoadNextStage()
    {
        Stage currentStageComponent = stages[currentStage].GetComponent<Stage>();
        InstantiateAsync(stages[currentStage + 1], null, currentStageComponent.LoadRoot.transform.position, Quaternion.identity);
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
