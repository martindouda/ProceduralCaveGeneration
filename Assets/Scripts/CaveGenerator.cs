using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class CaveGenerator : MonoBehaviour
{
    [SerializeField] private Transform m_Entrance;
    [SerializeField] private Transform m_Exit;

    [SerializeField] private float m_SphereRadius;

    void Start()
    {
    }

    void Update()
    {
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(m_Entrance.position, m_SphereRadius);
        Gizmos.DrawWireSphere(m_Exit.position, m_SphereRadius);
        Gizmos.DrawLine(m_Entrance.position, m_Exit.position);
    }

    public void Generate()
    {
        Debug.Log("Generating...");
    }
}
