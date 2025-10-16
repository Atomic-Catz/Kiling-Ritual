using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance;

    public TextMeshProUGUI healthPercentage;
    
    [SerializeField]private TextMeshProUGUI promptText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public void UpdateText(string promptMessage)
    {
        promptText.text = promptMessage;
    }
    
}
