using UnityEngine;

public class RagdollController : MonoBehaviour
{
    private Rigidbody[] rigidbodies;
    private Collider[] colliders;
    private Animator animator;

    private void Awake()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        animator = GetComponent<Animator>();

        // Turn off ragdoll at start
        SetRagdoll(false);
    }
    
    public void SetRagdoll(bool active)
    {
        foreach (var rb in rigidbodies)
            rb.isKinematic = !active;

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject)
                continue;

            col.enabled = active;
        }

        if (animator != null)
            animator.enabled = !active;
    }
}