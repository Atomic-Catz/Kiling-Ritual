using UnityEngine;
using TMPro;

namespace InfimaGames.LowPolyShooterPack
{
    public class PlayerProfileManager : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_InputField nameInputField;

        private const string NamePrefsKey = "PlayerNametag";

        private void Start()
        {
            // Load the saved name if it exists, otherwise generate a default one
            if (PlayerPrefs.HasKey(NamePrefsKey))
            {
                nameInputField.text = PlayerPrefs.GetString(NamePrefsKey);
            }
            else
            {
                string randomName = "Survivor_" + Random.Range(1000, 9999);
                nameInputField.text = randomName;
                SavePlayerName(randomName);
            }

            // Listen for when the player finishes typing
            nameInputField.onEndEdit.AddListener(SavePlayerName);
        }

        public void SavePlayerName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;

            // Save the name to the player's computer
            PlayerPrefs.SetString(NamePrefsKey, newName);
            PlayerPrefs.Save();
            
            Debug.Log($"[Profile] Name saved as: {newName}");
        }
    }
}