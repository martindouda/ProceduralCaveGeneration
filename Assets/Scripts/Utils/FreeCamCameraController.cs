using System.ComponentModel;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class FreeCamCameraController : MonoBehaviour
{
    [SerializeField] private Transform m_SpotLight;
    [SerializeField] private SpotLight m_SpotLightLight;

    [SerializeField] private float m_Speed = 20.0f;
    [SerializeField] private float m_LookSpeed = 1.0f;
    [SerializeField] private float m_SpotLightLerpCoef = 0.06f;


    private bool m_SpotLightFixed = false;
    private bool m_RotationEnabled = true;
    private float m_RotationX = 0.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            m_RotationEnabled = !m_RotationEnabled;
            if (m_RotationEnabled)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            } else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            m_SpotLight.gameObject.SetActive(!m_SpotLight.gameObject.activeSelf);
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            m_SpotLightFixed = !m_SpotLightFixed;
        }


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
        transform.position += moveVector.normalized * speedModified * Time.deltaTime;
                    
        if (!m_SpotLightFixed)
        {
            m_SpotLight.position = transform.position;
            m_SpotLight.rotation = Quaternion.Slerp(m_SpotLight.rotation, Camera.main.transform.rotation, m_SpotLightLerpCoef);
        }
        
        if (!m_RotationEnabled) return;

        m_RotationX -= Input.GetAxis("Mouse Y") * m_LookSpeed;
        m_RotationX = Mathf.Clamp(m_RotationX, -90.0f, 90.0f);
        Camera.main.transform.localRotation = Quaternion.Euler(m_RotationX, 0.0f, 0.0f);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * m_LookSpeed, 0.0f);
    }
}