using UnityEngine;

public class Button : Interactable
{
    [SerializeField]private GameObject door;

    private bool doorOpen;
    // Function where we design interaction using code
    protected override void Interact()
    {
        doorOpen = !doorOpen;
        door.GetComponent<Animator>().SetBool("IsOpen", doorOpen);
    }
}
