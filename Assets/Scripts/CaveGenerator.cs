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
    [SerializeField][Range(0.5f, 5.0f)] private float m_MinSphereRadius = 1.0f;
    [SerializeField][Range(0.5f, 5.0f)] private float m_MaxSphereRadius = 3.0f;
    [SerializeField][Range(1.0f, 10.0f)] private float m_SpacingLimit = 2.0f;
    [SerializeField][Range(1, 100)] private int m_NumSamplesBeforeRejection = 30;
    [Space(20)]
    [SerializeField][Range(1, 10)] private int m_SearchDistance = 5;
    [SerializeField][Range(10, 100)] private int m_IdealNumOfNearest = 30;

    private float m_GenerationTime = 0.0f;


    class Node : IHeapItem<Node>
    {
        public int index;

        public Node(int index)
        {
            this.index = index;
        }

        public int HeapIndex { get => index; set => index = value; }

        public int CompareTo(object obj)
        {
            Node other = obj as Node;
            return other.index.CompareTo(index);
        }
    }

    Heap<Node> heap = new Heap<Node>(100);
    public int HeapAddedNum = 0;

    private void OnDrawGizmos()
    {
        /*Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(m_Entrance.position, m_SphereRadius / 2.0f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(m_Exit.position, m_SphereRadius / 2.0f);*/

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
        
        //Gizmos.DrawLine(m_Entrance.position, m_Exit.position);
    }

    public void Generate()
    {
        PoissonSpheres poissonSpheres = new PoissonSpheres(m_Size, m_MinSphereRadius, m_MaxSphereRadius, m_SpacingLimit);

        float time = Time.realtimeSinceStartup;
        poissonSpheres.GeneratePoints(m_NumSamplesBeforeRejection);
        poissonSpheres.ConnectNearest(m_SearchDistance, m_IdealNumOfNearest);
        m_GenerationTime = Time.realtimeSinceStartup - time;

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

    public float GetGenerationTime()
    {
        return m_GenerationTime;
    }
}
