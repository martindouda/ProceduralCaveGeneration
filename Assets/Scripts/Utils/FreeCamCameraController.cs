using System.ComponentModel;
using UnityEngine;

public class FreeCamCameraController : MonoBehaviour
{
    [SerializeField] private Transform m_SpotLight;

    [SerializeField] private float m_Speed = 20.0f;
    [SerializeField] private float m_LookSpeed = 1.0f;
    [SerializeField] private float m_SpotLightLerpCoef = 0.06f;


    private float m_RotationX = 0.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
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
        if (Input.GetKey(KeyCode.Space))
        {
            moveVector += Vector3.up;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveVector -= Vector3.up;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            speedModified *= 2.0f;
        }
        transform.position += moveVector.normalized * speedModified * Time.deltaTime;

        m_RotationX -= Input.GetAxis("Mouse Y") * m_LookSpeed;
        m_RotationX = Mathf.Clamp(m_RotationX, -90.0f, 90.0f);
        Camera.main.transform.localRotation = Quaternion.Euler(m_RotationX, 0.0f, 0.0f);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * m_LookSpeed, 0.0f);
        m_SpotLight.position = transform.position;
        m_SpotLight.rotation = Quaternion.Slerp(m_SpotLight.rotation, Camera.main.transform.rotation, m_SpotLightLerpCoef);
    }
}