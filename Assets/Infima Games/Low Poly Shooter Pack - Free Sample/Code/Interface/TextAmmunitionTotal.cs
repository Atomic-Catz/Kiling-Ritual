using System.Globalization;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    public class TextAmmunitionTotal : ElementText
    {
        protected override void Tick()
        {
            if (equippedWeapon == null)
                return;

            int reserveAmmo = 0;

            // Only our custom Weapon class has reserve ammo
            if (equippedWeapon is Weapon w)
                reserveAmmo = w.GetReserveAmmunition();

            // Update text
            textMesh.text = reserveAmmo.ToString(CultureInfo.InvariantCulture);
        }
    }
}