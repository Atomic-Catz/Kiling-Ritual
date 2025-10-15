using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    // Display when Player looks at interactable
    public string promptMessage;

    // Called from Player
    public void BaseInteract()
    {
        Interact();
    }
    
    protected virtual void Interact()
    {
        // This will be overridden by subclasses
    }
}
