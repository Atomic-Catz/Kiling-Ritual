using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class AutoDamageTest : MonoBehaviour
{
    [SerializeField] private CharacterHealth health;
    [SerializeField] private float damageAmount = 25f;
    [SerializeField] private float damageInterval = 2f;

    private float timer;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<CharacterHealth>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= damageInterval)
        {
            timer = 0f;
            health.TakeDamage(damageAmount);
        }
    }
}