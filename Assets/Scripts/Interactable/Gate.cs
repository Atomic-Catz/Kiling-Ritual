using UnityEngine;

public class Gate : Interactable
{
    private bool gateOpen;
    protected override void Interact()
    {
        gateOpen = !gateOpen;
        this.gameObject.GetComponent<Animator>().SetBool("IsOpen", gateOpen);
    }
}
