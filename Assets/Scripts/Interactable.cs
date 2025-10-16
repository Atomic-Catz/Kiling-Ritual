using UnityEngine;
using UnityEditor;

public abstract class Interactable : MonoBehaviour
{
    public bool useEvents;
    // Display when Player looks at interactable
    public string promptMessage;

    // Called from Player
    public void BaseInteract()
    {
        
        if(useEvents)
            GetComponent<InteractionEvent>().onInteract.Invoke();
        Interact();
    }
    
    protected virtual void Interact()
    {
        // This will be overridden by subclasses
    }
}
