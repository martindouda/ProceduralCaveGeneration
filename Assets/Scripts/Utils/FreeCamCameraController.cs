using System.ComponentModel;
using UnityEngine;

public class FreeCamCameraController : MonoBehaviour
{
    [SerializeField] private float m_Speed = 1.0f;
    [SerializeField] private float m_LookSpeed = 1.0f;


    private float m_RotationX = 0.0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += Camera.main.transform.forward * m_Speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position += -Camera.main.transform.forward * m_Speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += Camera.main.transform.right * m_Speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position += -Camera.main.transform.right * m_Speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            transform.position += Vector3.up * m_Speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.position += -Vector3.up * m_Speed * Time.deltaTime;
        }


        m_RotationX -= Input.GetAxis("Mouse Y") * m_LookSpeed;
        m_RotationX = Mathf.Clamp(m_RotationX, -90.0f, 90.0f);
        Camera.main.transform.localRotation = Quaternion.Euler(m_RotationX, 0.0f, 0.0f);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * m_LookSpeed, 0.0f);
    }
}