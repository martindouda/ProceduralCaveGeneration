/*
 * Project: Procedural Generation of Cave Systems
 * File: FreeCamCameraController.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This file serves as the primary controller for the free-flying camera within the Unity environment. It provides
 * functionality for camera movement, rotation, and spotlight control. Additionally, it manages user input for toggling camera
 * rotation, spotlight visibility, and fixing the spotlight to the camera's position.
*/

using System.ComponentModel;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

// This class provides functionality for controlling a free-flying camera in Unity.
public class FreeCamCameraController : MonoBehaviour
{
    [SerializeField] private Transform m_SpotLight; // Reference to the spotlight's transform
    [SerializeField] private SpotLight m_SpotLightLight; // Reference to the spotlight component

    [SerializeField] private float m_Speed = 20.0f; // Movement speed of the camera
    [SerializeField] private float m_LookSpeed = 1.0f; // Speed of camera rotation
    [SerializeField] private float m_SpotLightLerpCoef = 0.06f; // Coefficient for smoothing spotlight rotation

    private bool m_SpotLightFixed = false; // Indicates if the spotlight is fixed to the camera's position
    private bool m_RotationEnabled = true; // Indicates if camera rotation is enabled
    private float m_RotationX = 0.0f; // Stores the rotation around the X axis

    // Initialization method called when the script is first enabled
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Locks the cursor to the center of the screen
        Cursor.visible = false; // Hides the cursor
    }

    // Update method called once per frame
    private void Update()
    {
        // Toggle camera rotation on/off with the 'C' key
        if (Input.GetKeyDown(KeyCode.C))
        {
            m_RotationEnabled = !m_RotationEnabled;
            if (m_RotationEnabled)
            {
                Cursor.lockState = CursorLockMode.Locked; // Locks the cursor
                Cursor.visible = false; // Hides the cursor
            }
            else
            {
                Cursor.lockState = CursorLockMode.None; // Unlocks the cursor
                Cursor.visible = true; // Shows the cursor
            }
        }

        // Toggle spotlight visibility with the 'F' key
        if (Input.GetKeyDown(KeyCode.F))
        {
            m_SpotLight.gameObject.SetActive(!m_SpotLight.gameObject.activeSelf);
        }

        // Toggle whether the spotlight is fixed to the camera's position with the 'V' key
        if (Input.GetKeyDown(KeyCode.V))
        {
            m_SpotLightFixed = !m_SpotLightFixed;
        }

        // Calculate movement vector based on player input
        Vector3 moveVector = Vector3.zero;
        float speedModified = m_Speed;
        if (Input.GetKey(KeyCode.W))
        {
            moveVector += Camera.main.transform.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveVector -= Camera.main.transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveVector += Camera.main.transform.right;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveVector -= Camera.main.transform.right;
        }
        if (Input.GetKey(KeyCode.E))
        {
            moveVector += Vector3.up;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            moveVector -= Vector3.up;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            speedModified *= 2.0f;
        }
        
        // Move the camera based on the calculated move vector
        transform.position += moveVector.normalized * speedModified * Time.deltaTime;
                    
        // Update spotlight position and rotation if it's not fixed
        if (!m_SpotLightFixed)
        {
            m_SpotLight.position = transform.position;
            m_SpotLight.rotation = Quaternion.Slerp(m_SpotLight.rotation, Camera.main.transform.rotation, m_SpotLightLerpCoef);
        }
        
        // Perform camera rotation if rotation is enabled
        if (!m_RotationEnabled) return;

        m_RotationX -= Input.GetAxis("Mouse Y") * m_LookSpeed;
        m_RotationX = Mathf.Clamp(m_RotationX, -90.0f, 90.0f);
        Camera.main.transform.localRotation = Quaternion.Euler(m_RotationX, 0.0f, 0.0f);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * m_LookSpeed, 0.0f);
    }
}