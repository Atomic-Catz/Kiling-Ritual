using UnityEngine;

public class NukePickup : MonoBehaviour
{
    [Header("Visual Settings")]
    public float rotationSpeed = 90f;
    public Color nukeColor = Color.gold;

    [Header("Pickup Settings")]
    [Tooltip("Time in seconds before the pickup disappears if not collected.")]
    public float despawnTime = 15f;
    
    private Renderer rend;

    private void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            rend.material.color = nukeColor;
        
        // Start auto-despawn timer
        Destroy(gameObject, despawnTime);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player pickup
        if (other.CompareTag("Player"))
        {
            ActivateNuke();
            Destroy(gameObject);
        }
    }

    private void ActivateNuke()
    {
        Debug.Log("NUKE ACTIVATED!");

        // Find all tagged enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemyObj in enemies)
        {
            EnemyAI enemy = enemyObj.GetComponent<EnemyAI>();
            if (enemy == null) continue;

            // Try normal death
            enemy.health = 0;

            // Call private DestroyEnemy() using UnityEvent
            enemy.SendMessage("DestroyEnemy", SendMessageOptions.DontRequireReceiver);
            
        }
    }
}