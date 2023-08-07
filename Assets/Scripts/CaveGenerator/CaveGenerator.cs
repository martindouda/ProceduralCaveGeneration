using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using static PoissonSpheres;


[ExecuteInEditMode]
public class CaveGenerator : MonoBehaviour
{
    public static CaveGenerator Instance;

    [SerializeField] private Transform m_Start; private Vector3 m_LastStartPosition = new Vector3();
    [SerializeField] private Transform m_End;   private Vector3 m_LastEndPosition = new Vector3();

    [SerializeField] private Transform m_SpheresParent;
    [SerializeField] private GameObject m_SpherePrefab;
    [SerializeField] private Material[] m_Materials = new Material[(int)PoissonSpheres.SphereType._SIZE];

    [SerializeField] private Transform m_HorizonsParent;

    [Space(20)]
    [Header("RENDERING OPTIONS")]
    [SerializeField] private bool m_RenderPoints = false;
    [SerializeField] private float m_PointSize = 0.5f;
    [SerializeField] private bool m_RenderNeighboursVisualization = true;
    [Space(20)]
    [Header("POISSON SPHERES")]
    [SerializeField] private Vector3 m_Size = new Vector3(10.0f, 10.0f, 10.0f);
    [SerializeField][Range(0.5f, 5.0f)] private float m_MinSphereRadius = 1.0f;
    [SerializeField][Range(0.5f, 5.0f)] private float m_MaxSphereRadius = 3.0f;
    [SerializeField][Range(1.0f, 10.0f)] private float m_SpacingLimit = 2.0f;
    [SerializeField][Range(1, 100)] private int m_NumSamplesBeforeRejection = 30;
    [Space(20)]
    [Header("SPHERES CONNECTION")]
    [SerializeField][Range(1, 10)] private int m_SearchDistance = 5;
    [SerializeField][Range(1, 100)] private int m_IdealNumOfNearest = 30;


    private float m_GenerationTime = 0.0f;
    private float m_VisualizationTime = 0.0f;

    // Poisson spheres
    PoissonSpheres m_PoissonSpheres;
    [HideInInspector]public int VisualizedSphere = 0;

    private List<Horizon> m_Horizons = new List<Horizon>();
    private float m_CheapestHorizon = 0.0f;

    private int m_MinNearest = 9999999;
    private int m_MaxNearest = -9999999;
    private float m_FurthestApartConnectedSpheres = 0.0f;
    private SpherePool m_SpherePool;


    public float GenerationTime { get => m_GenerationTime; }
    public float VisualizationTime { get => m_VisualizationTime; }
    public SpherePool Pool { get => m_SpherePool; }


    private void Start()
    {
        for (int i = m_SpheresParent.childCount - 1; i >= 0; i--)
        {
            Transform child = m_SpheresParent.GetChild(i);
            DestroyImmediate(child.gameObject);
        }
    }

    private void Update()
    {
        Instance = this;
        if (m_PoissonSpheres == null) return;

        if (m_Start.position != m_LastStartPosition)
        {
            m_LastStartPosition = m_Start.position;
            Visualize();
        } 
        else if (m_End.position != m_LastEndPosition)
        {
            m_LastEndPosition = m_End.position;
            Visualize();
        }
    }

    public void Generate()
    {
        m_PoissonSpheres = new PoissonSpheres(m_Size, m_MinSphereRadius, m_MaxSphereRadius, m_SpacingLimit);
        m_SpherePool = GetComponent<SpherePool>();

        float time = Time.realtimeSinceStartup;
        m_PoissonSpheres.GeneratePoints(m_NumSamplesBeforeRejection);
        ConnectNearest(m_SearchDistance, m_IdealNumOfNearest);
        m_GenerationTime = Time.realtimeSinceStartup - time;

        Visualize();
    }

    public void Visualize()
    {
        if (m_PoissonSpheres == null)
        {
            Debug.LogWarning("Generating on CaveGenerator is necessary!");
            return;
        }


        float time = Time.realtimeSinceStartup;


        m_SpherePool.NewRound();
        List<PoissonSpheres.Point> points = m_PoissonSpheres.Points;
        for (int i = 0; i < points.Count; i++)
        {
            points[i].VisualSphereType = PoissonSpheres.SphereType.WHITE;
        }


        if (m_RenderNeighboursVisualization)
        {
            if (VisualizedSphere > points.Count) VisualizedSphere = points.Count - 1;
            if (VisualizedSphere < 0) VisualizedSphere = 0;
            var examinedPoint = points[VisualizedSphere];
            examinedPoint.VisualSphereType = PoissonSpheres.SphereType.BLUE;
            foreach (var nearestPoint in examinedPoint.NextList)
            {
                points[nearestPoint.PointIndex].VisualSphereType = PoissonSpheres.SphereType.RED;
            }
        }


        FindShortestPath(m_Start.position, m_End.position, m_SearchDistance);
        Vector3 toCenterOffset = new Vector3(m_Size.x / 2, 0.0f, m_Size.z / 2);
        if (m_RenderPoints)
        {
            for (int i = 0; i < points.Count; i++)
            {
                m_SpherePool.WakeSphere(points[i].Pos - toCenterOffset, m_PointSize, m_Materials[(int)points[i].VisualSphereType], i);
            }
        } 
        else
        {
            for (int i = 0; i < points.Count; i++)
            {
                m_SpherePool.WakeSphere(points[i].Pos - toCenterOffset, points[i].Radius * 2.0f, m_Materials[(int)points[i].VisualSphereType], i);
            }
        }
        m_SpherePool.PutUnusedToSleep();


        m_VisualizationTime = Time.realtimeSinceStartup - time;
        //Debug.Log("Visualization took: " + m_VisualizationTime + "ms");
    }

    public void ConnectNearest(int searchDist, int idealNumOfNeighbours)
    {
        var points = m_PoissonSpheres.Points;
        var grid = m_PoissonSpheres.Grid;

        foreach (var p in points)
        {
            Vector3 gridPos = m_PoissonSpheres.GetGridPos(p.Pos);

            int gridPosX = (int)gridPos.x;
            int gridPosY = (int)gridPos.y;
            int gridPosZ = (int)gridPos.z;

            int startX = Mathf.Max(gridPosX - searchDist, 0);
            int endX = Mathf.Min(gridPosX + searchDist, m_PoissonSpheres.GridSizeX - 1);
            int startY = Mathf.Max(gridPosY - searchDist, 0);
            int endY = Mathf.Min(gridPosY + searchDist, m_PoissonSpheres.GridSizeY - 1);
            int startZ = Mathf.Max(gridPosZ - searchDist, 0);
            int endZ = Mathf.Min(gridPosZ + searchDist, m_PoissonSpheres.GridSizeZ - 1);

            int searchWidth = (int)(2.0f * searchDist) + 1;
            Heap<NearestPoint> heap = new Heap<NearestPoint>(searchWidth * searchWidth * searchWidth);


            int tempGridPos = grid[gridPosX, gridPosY, gridPosZ];
            grid[gridPosX, gridPosY, gridPosZ] = -1;
            for (int z = startZ; z <= endZ; z++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        int index = grid[x, y, z];
                        if (index == -1) continue;

                        heap.Add(new NearestPoint(index, (points[index].Pos - p.Pos).sqrMagnitude));
                    }
                }
            }
            grid[gridPosX, gridPosY, gridPosZ] = tempGridPos;


            while (heap.Count > 0 && p.NextList.Count < idealNumOfNeighbours)
            {
                var item = heap.Pop();
                item.CalculateDist();
                p.NextList.Add(item);

                m_FurthestApartConnectedSpheres = Mathf.Max(item.Dist, m_FurthestApartConnectedSpheres);
            }

            m_MinNearest = Mathf.Min(p.NextList.Count, m_MinNearest);
            m_MaxNearest = Mathf.Max(p.NextList.Count, m_MaxNearest);
        }
        //Debug.Log(m_FurthestApartConnectedSpheres);
        //Debug.Log("min: " + m_MinNearest + ", max: " + m_MaxNearest);
    }

    private class Node : IHeapItem<Node>
    {
        private int m_HeapIndex;
        private int m_PointIndex;
        private float m_FCost;
        private float m_GCost;
        private float m_HCost;

        public Node Previous;

        public Node(int pointIndex, Node previous, float gCost, float hCost)
        {
            m_HeapIndex = -1;
            m_PointIndex = pointIndex;
            m_FCost = gCost + hCost;
            m_GCost = gCost;
            m_HCost = hCost;
            Previous = previous;
        }

        public int HeapIndex { get => m_HeapIndex; set => m_HeapIndex = value; }
        public int PointIndex { get => m_PointIndex; }
        public float FCost { get => m_FCost; }
        public float GCost { get => m_GCost; }
        public float HCost { get => m_HCost; }

        public int CompareTo(object obj)
        {
            Node other = obj as Node;
            return other.m_FCost.CompareTo(m_FCost);
        }
    }

    private void LoadHorizons()
    {
        Heap<Horizon> horizons = new Heap<Horizon>(1000); // max num of horizons is 1000
        for (int i = 0; i < m_HorizonsParent.childCount; i++)
        {
            Transform horizonTransform = m_HorizonsParent.GetChild(i);
            Horizon horizon = horizonTransform.GetComponent<Horizon>();
            horizons.Add(horizon);
        }
        m_Horizons.Clear();
        m_CheapestHorizon = Mathf.Infinity;
        while (horizons.Count > 0)
        {
            Horizon h = horizons.Pop();

            if (h.Cost < m_CheapestHorizon) m_CheapestHorizon = h.Cost;
            
            m_Horizons.Add(h);
        }
    }

    private float GetHorizonCost(float height)
    {
        for (int i = 1; i < m_Horizons.Count; i++)
        {
            if (height < m_Horizons[i].Height)
            {
                // smooth step function
                float normalizedDistanceBetweenHorizons =  (height - m_Horizons[i - 1].Height) / (m_Horizons[i].Height - m_Horizons[i - 1].Height);
                return Mathf.SmoothStep(m_Horizons[i - 1].Cost, m_Horizons[i].Cost, normalizedDistanceBetweenHorizons);
            }
        }
        return 0.0f;
    }

    public void FindShortestPath(Vector3 start, Vector3 end, int initialNEarestPointSearchDistance)
    {
        LoadHorizons();
        //float time = Time.realtimeSinceStartup;

        var points = m_PoissonSpheres.Points;

        int startIndex = m_PoissonSpheres.FindNearestPointsIndex(start, initialNEarestPointSearchDistance);
        int endIndex = m_PoissonSpheres.FindNearestPointsIndex(end, initialNEarestPointSearchDistance);
        var startPoint = points[startIndex];
        var endPoint = points[endIndex];


        bool[] closed = new bool[points.Count];
        float[] lowestFCost = new float[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            lowestFCost[i] = Mathf.Infinity;
        }


        Heap<Node> open = new Heap<Node>(points.Count * m_MaxNearest);
        float startHCost = (startPoint.Pos - endPoint.Pos).magnitude;
        open.Add(new Node(startIndex, null, 0.0f, startHCost));
        lowestFCost[startIndex] = startHCost;


        Node goalNode = null;
        while (open.Count > 0)
        {
            Node n = open.Pop();
            if (lowestFCost[n.PointIndex] < n.FCost) continue;

            if (n.PointIndex == endIndex)
            {
                goalNode = n;
                break;
            }

            var p = points[n.PointIndex];
            foreach (NearestPoint child in points[n.PointIndex].NextList)
            {
                var childPoint = points[child.PointIndex];

                float horizonCost = GetHorizonCost(childPoint.Pos.y);
                float gCost = n.GCost + child.Dist * horizonCost;
                float hCost = (childPoint.Pos - endPoint.Pos).magnitude;
                /////////
                hCost = hCost + hCost / m_FurthestApartConnectedSpheres * m_CheapestHorizon;

                Node newNode = new Node(child.PointIndex, n, gCost, hCost);

                if (lowestFCost[child.PointIndex] < newNode.FCost) continue;

                lowestFCost[child.PointIndex] = newNode.FCost;
                open.Add(newNode);
            }

            closed[n.PointIndex] = true;
        }

        if (goalNode == null)
        {
            Debug.LogWarning("No path found!!!");
            return;
        }
        Node node = goalNode.Previous;
        while (node != null)
        {
            points[node.PointIndex].VisualSphereType = SphereType.GREEN;
            //Debug.Log(node.GCost + " " + node.HCost + " " + node.FCost);
            node = node.Previous;
        }

        startPoint.VisualSphereType = SphereType.GREEN;
        endPoint.VisualSphereType = SphereType.GREEN;

        //Debug.Log("A* took: " + (Time.realtimeSinceStartup - time) + "ms");
    }
}
