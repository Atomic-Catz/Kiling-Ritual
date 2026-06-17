using System.Collections;
using UnityEngine;
using PurrNet;
using PurrNet.Transports;

public class NetworkStarter : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Wait exactly one frame to ensure the MapSpawner and spawn points are fully awake
        yield return null;

        // Check the variables we set in the Main Menu
        if (MainMenu.connectAsHost)
        {
            MainMenu.connectAsHost = false; // Reset it so it doesn't double-fire
            if (NetworkManager.main != null) NetworkManager.main.StartHost();
        }
        else if (MainMenu.connectAsClient)
        {
            MainMenu.connectAsClient = false; // Reset it
            if (NetworkManager.main != null)
            {
                // Inject the IP we typed in the Main Menu
                NetworkManager.main.GetComponent<UDPTransport>().address = MainMenu.joinIP;
                NetworkManager.main.StartClient();
            }
        }
    }
}