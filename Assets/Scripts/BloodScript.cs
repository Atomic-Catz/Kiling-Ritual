using UnityEngine;

public class BloodScript : MonoBehaviour
{
    [Tooltip("Time in seconds before this object is destroyed.")]
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
