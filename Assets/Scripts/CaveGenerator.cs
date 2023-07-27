using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class CaveGenerator : MonoBehaviour
{
    [SerializeField] private Transform m_Entrance;
    [SerializeField] private Transform m_Exit;
    [SerializeField] private Transform m_PivotsParent;
    [SerializeField] private GameObject m_SpherePrefab;
    [Space(40)]
    [SerializeField] private Vector3 m_Size = new Vector3(10.0f, 10.0f, 10.0f);
    [SerializeField][Range(0.5f, 3.0f)] private float m_SphereRadius = 0.4f;
    [SerializeField][Range(1.0f, 10.0f)] private float m_SpacingLimit = 2.0f;
    [SerializeField][Range(1, 100)] private int m_NumSamplesBeforeRejection = 30;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(m_Entrance.position, m_SphereRadius / 2.0f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(m_Exit.position, m_SphereRadius / 2.0f);

        /*Gizmos.color = Color.cyan;
        foreach (Transform t in m_Pivots)
        {
            Gizmos.DrawSphere(t.position, m_SphereRadius);
        }*/
        /*if (m_Pivots.Count > 0)
        {
            Gizmos.DrawLine(m_Entrance.position, m_Pivots[0].position);
            for (int i = 0; i < m_Pivots.Count-1; i++)
            {
                Gizmos.DrawLine(m_Pivots[i].position, m_Pivots[i+1].position);
            }
            Gizmos.DrawLine(m_Pivots[m_Pivots.Count - 1].position, m_Exit.position);
            return;
        }*/
        
        Gizmos.DrawLine(m_Entrance.position, m_Exit.position);
    }

    public void Generate()
    {
        Debug.Log("Generating...");
        PoissonSpheres poissonSpheres = new PoissonSpheres(m_Size, m_SphereRadius, m_SpacingLimit);
        poissonSpheres.GeneratePoints(m_NumSamplesBeforeRejection);

        while (m_PivotsParent.childCount > 0)
        {
            DestroyImmediate(m_PivotsParent.GetChild(0).gameObject);
        }

        List<PoissonSpheres.Point> points = poissonSpheres.GetPoints();
        Vector3 toCenterOffset = new Vector3(m_Size.x / 2, 0.0f, m_Size.z / 2);
        for (int i = 0; i < points.Count; i++)
        {
            GameObject go = Instantiate(m_SpherePrefab, m_PivotsParent);
            go.transform.position = points[i].pos - toCenterOffset;
            go.transform.localScale = Vector3.one * points[i].r * 2.0f;
        }
    }

    public void Delete()
    {
        while (m_PivotsParent.childCount > 0)
        {
            DestroyImmediate(m_PivotsParent.GetChild(0).gameObject);
        }
    }
}
