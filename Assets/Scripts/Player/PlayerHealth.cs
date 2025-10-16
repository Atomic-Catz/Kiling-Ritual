using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth instance;

    public int maxHealth, currentHealth;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        currentHealth = maxHealth;
        PlayerUI.Instance.healthPercentage.text = "+" + currentHealth;
    }
    
    public void DamagePlayer(int damage)
    {
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }   
        
        PlayerUI.Instance.healthPercentage.text = "+" + currentHealth;
        
    }
}
