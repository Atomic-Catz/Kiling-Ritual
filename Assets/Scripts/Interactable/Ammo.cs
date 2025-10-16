using UnityEngine;

public class Ammo : Interactable
{
    protected override void Interact()
    {
        Destroy(gameObject);
    }
}
