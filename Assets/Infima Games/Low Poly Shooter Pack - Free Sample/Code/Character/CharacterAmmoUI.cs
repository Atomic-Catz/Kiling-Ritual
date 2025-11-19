using UnityEngine;
using TMPro;
using InfimaGames.LowPolyShooterPack;

public class CharacterAmmoUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI textCurrent;  // Text Ammunition Current
    [SerializeField] private TextMeshProUGUI textTotal;    // Text Ammunition Total

    private CharacterBehaviour character;
    private WeaponBehaviour weapon;

    private void Awake()
    {
        // Auto-find text fields if not assigned
        if (textCurrent == null || textTotal == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                textCurrent = texts[0];
                textTotal   = texts[1];
            }
        }

        if (textCurrent == null || textTotal == null)
            Debug.LogError("Ammo UI: Missing TextMeshProUGUI reference!");

        // Get the player
        character = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
    }

    private void Update()
    {
        if (character == null)
            return;

        // Get equipped weapon
        weapon = character.GetInventory().GetEquipped();

        if (weapon == null)
        {
            textCurrent.text = "0";
            textTotal.text   = "0";
            return;
        }

        // Current magazine ammo
        int currentAmmo = weapon.GetAmmunitionCurrent();

        // Reserve ammo (only exists on your custom Weapon class)
        int reserveAmmo = 0;
        if (weapon is Weapon w)
            reserveAmmo = w.GetReserveAmmunition();

        // Update UI
        textCurrent.text = currentAmmo.ToString();
        textTotal.text   = reserveAmmo.ToString();
    }
}