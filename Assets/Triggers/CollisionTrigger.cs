using UnityEngine;

public class CollisionTrigger : Trigger
{
    [SerializeField] private string filterTag = "Player";
    
    private void Awake()
    {
        Collider collider =  GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError($"{gameObject.name} has a collision trigger but no collider!");
            return;
        }

        if(collider.isTrigger)
        {
            Debug.LogError($"{gameObject.name} has both a collider and a collision trigger, but the collider is not set to be a trigger! Did you forget to check the box to make it a trigger?");
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(filterTag.Length > 0 && !other.CompareTag(filterTag))
            return;
        
        ActivateTriggers();
    }
}