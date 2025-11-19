using UnityEngine;
using System.Globalization;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    public class TextAmmunitionCurrent : ElementText
    {
        [Header("Colors")]
        [SerializeField] private bool updateColor = true;
        [SerializeField] private float emptySpeed = 1.5f;
        [SerializeField] private Color emptyColor = Color.red;

        protected override void Tick()
        {
            if (equippedWeapon == null)
                return;

            // Current magazine ammo
            float current = equippedWeapon.GetAmmunitionCurrent();

            // Total magazine size
            float total = equippedWeapon.GetAmmunitionTotal();

            // Update text
            textMesh.text = current.ToString(CultureInfo.InvariantCulture);

            // Update color based on ammo count
            if (updateColor)
            {
                float colorAlpha = (current / total) * emptySpeed;
                textMesh.color = Color.Lerp(emptyColor, Color.white, colorAlpha);
            }
        }
    }
}