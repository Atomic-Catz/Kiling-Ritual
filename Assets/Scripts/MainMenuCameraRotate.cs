using UnityEngine;

public class MainMenuCameraRotate : MonoBehaviour
{

    [Header("Rotation Settings")] [Tooltip("Adjust the values to change the speed and direction of the rotation.")]
    public Vector3 rotationSpeed = new Vector3(0f, 2f, 0f);
    
    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);
    }
}
