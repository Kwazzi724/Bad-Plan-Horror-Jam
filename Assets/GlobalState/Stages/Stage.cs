using System;
using UnityEngine;

public class Stage : MonoBehaviour
{
    public event Action CleanupReady;
    
    public GameObject LoadRoot => loadRoot;
    
    // The game object that the next scene should be instantiated at
    [SerializeField] private GameObject loadRoot;

    public void SetReadyForCleanup()
    {
        CleanupReady?.Invoke();
    }
    
    private void OnValidate()
    {
        if(loadRoot == null)
            Debug.LogError($"{gameObject.name} requires a {nameof(LoadRoot)}, since it is a stage");
    }
}
