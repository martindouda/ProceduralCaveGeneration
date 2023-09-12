using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using static PoissonSpheres;

[ExecuteInEditMode]
public class CaveGenerator : MonoBehaviour
{
    public static CaveGenerator Instance;

    [SerializeField] private Transform m_KeyPointsParent;
    private List<KeyPoint> m_KeyPoints = new List<KeyPoint>();

    [SerializeField] private Transform m_LineRenderersParent;
    [SerializeField] private LineRenderer m_LineRendererPrefab;

    [SerializeField] private Material[] m_Materials = new Material[(int)PoissonSpheres.SphereType._SIZE];

    [SerializeField] private Transform m_HorizonsParent;
    [SerializeField] private Transform m_FracturesParent;

    [Space(20)][Header("RENDERING OPTIONS")]
    [SerializeField] private bool m_RenderPoints = false;
    [SerializeField] private float m_PointSize = 0.5f;
    [SerializeField] private bool m_RenderNeighboursVisualization = true;
    [SerializeField] private bool m_VisualizeOnPointMovement = true;
    [Space(20)][Header("POISSON SPHERES")]
    [SerializeField] private Vector3 m_Size = new Vector3(10.0f, 10.0f, 10.0f);
    [SerializeField][Range(0.5f, 5.0f)] private float m_MinSphereRadius = 1.0f;
    [SerializeField][Range(0.5f, 5.0f)] private float m_MaxSphereRadius = 3.0f;
    [SerializeField][Range(1.0f, 10.0f)] private float m_SpacingLimit = 2.0f;
    [SerializeField][Range(1, 100)] private int m_NumSamplesBeforeRejection = 30;
    [Space(20)][Header("SPHERES CONNECTION")]
    [SerializeField][Range(1, 10)] private int m_SearchDistance = 5;
    [SerializeField][Range(1, 100)] private int m_IdealNumOfNearest = 30;
    [Space(20)][Header("PATH GENERATION")]
    [SerializeField][Range(0.0f, 100.0f)] private float m_HorizonsWeight = 10.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float m_FracturesWeight = 10.0f;
    [Space(20)][Header("PRONING")]
    [SerializeField][Range(0.0f, 100.0f)] private float m_ProningExponent = 1.0f;

    private float m_GenerationTime = 0.0f;
    private float m_VisualizationTime = 0.0f;

    // Poisson spheres
    private PoissonSpheres m_PoissonSpheres;
    [HideInInspector]public int VisualizedSphere = 0;

    private List<Horizon> m_Horizons = new List<Horizon>();
    private float m_CheapestHorizon = 0.0f;
    private List<float> m_CachedHorizonsHeights;

    private List<Fracture> m_Fractures = new List<Fracture>();


    private int m_MinNearest = 9999999;
    private int m_MaxNearest = -9999999;
    private float m_FurthestApartConnectedSpheres = 0.0f;
    private SpherePool m_SpherePool;

    private List<Path> m_Paths = new List<Path>();


    public float GenerationTime { get => m_GenerationTime; }
    public float VisualizationTime { get => m_VisualizationTime; }
    public SpherePool Pool { get => m_SpherePool; }

    private void Awake()
    {
        m_SpherePool = GetComponent<SpherePool>();
    }

    private void Start()
    {
        m_SpherePool.CleanUpSpheresOnSceneLoad();
    }

    private void Update()
    {
        Instance = this;
        if (m_PoissonSpheres == null) return;

        if (m_VisualizeOnPointMovement)
        {
            foreach (var keyPoint in m_KeyPoints)
            {
                if (keyPoint.transform.position != keyPoint.LastPos)
                {
                    keyPoint.LastPos = keyPoint.transform.position;
                    Visualize();
                    return;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < m_HorizonsParent.childCount; i++)
        {
            Gizmos.DrawSphere(m_HorizonsParent.GetChild(i).position, 1.0f);
        }
        Gizmos.DrawWireCube(new Vector3(transform.position.x, transform.position.y + m_Size.y/2, transform.position.z), m_Size);
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
        float time = Time.realtimeSinceStartup;
        if (m_PoissonSpheres == null)
        {
            Debug.LogWarning("Generating on CaveGenerator is necessary!");
            return;
        }


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


        ClearLines();
        LoadKeyPoints();
        LoadHorizons();
        LoadFractures();
        m_Paths.Clear();

        Vector3[] keyPointsPositions = new Vector3[m_KeyPoints.Count];
        for (int i = 0; i < m_KeyPoints.Count; i++)
        {
            keyPointsPositions[i] = m_KeyPoints[i].transform.position;
        }

        for (int i = 0; i < m_KeyPoints.Count - 1; i++)
        {
            Parallel.For(i + 1, m_KeyPoints.Count, (j) =>
            {
                FindShortestPath(keyPointsPositions[i], keyPointsPositions[j], m_SearchDistance);
            });
        }

        
        PronePaths();
        foreach (var path in m_Paths)
        {
            path.Visualize(m_PoissonSpheres, Instantiate(m_LineRendererPrefab, m_LineRenderersParent));
        }


        Vector3 toCenterOffset = new Vector3(m_Size.x / 2, 0.0f, m_Size.z / 2);
        if (m_RenderPoints)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].VisualSphereType == SphereType.WHITE) continue;
                m_SpherePool.WakeSphere(points[i].Pos - toCenterOffset, m_PointSize, m_Materials[(int)points[i].VisualSphereType], i);
            }
        } 
        else
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].VisualSphereType == SphereType.WHITE) continue;
                m_SpherePool.WakeSphere(points[i].Pos - toCenterOffset, points[i].Radius * 2.0f, m_Materials[(int)points[i].VisualSphereType], i);
            }
        }
        m_SpherePool.PutUnusedToSleep();


        m_VisualizationTime = Time.realtimeSinceStartup - time;
        //Debug.Log("Visualization took: " + m_VisualizationTime + "ms");
    }

    private void ClearLines()
    {
        for (int i = m_LineRenderersParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(m_LineRenderersParent.GetChild(i).gameObject);
        }
    }

    private void LoadKeyPoints()
    {
        m_KeyPoints.Clear();
        for (int i = 0; i < m_KeyPointsParent.childCount; i++)
        {
            Transform horizonTransform = m_KeyPointsParent.GetChild(i);
            KeyPoint keyPoint = horizonTransform.GetComponent<KeyPoint>();
            m_KeyPoints.Add(keyPoint);
        }
    }

    public void ConnectNearest(int searchDist, int idealNumOfNeighbours)
    {
        var points = m_PoissonSpheres.Points;
        var grid = m_PoissonSpheres.Grid;

        for (int i = 0; i < points.Count; i++)    
        {
            Point p = points[i];
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
        m_CachedHorizonsHeights.Clear();
        while (horizons.Count > 0)
        {
            Horizon h = horizons.Pop();

            if (h.Cost < m_CheapestHorizon) m_CheapestHorizon = h.Cost;
            
            m_Horizons.Add(h);
            m_CachedHorizonsHeights.Add(h.Height);
        }
    }

    private float GetHorizonCost(float height)
    {
        for (int i = 1; i < m_Horizons.Count; i++)
        {
            if (height < m_CachedHorizonsHeights[i])
            {
                // smooth step function
                float normalizedDistanceBetweenHorizons =  (height - m_CachedHorizonsHeights[i - 1]) / (m_CachedHorizonsHeights[i] - m_CachedHorizonsHeights[i - 1]);
                return Mathf.SmoothStep(m_Horizons[i - 1].Cost, m_Horizons[i].Cost, normalizedDistanceBetweenHorizons) * m_HorizonsWeight;
            }
        }
        return 0.0f;
    }
    private void LoadFractures()
    {
        m_Fractures.Clear();
        for (int i = 0; i < m_FracturesParent.childCount; i++)
        {
            Transform fractureTransform = m_FracturesParent.GetChild(i);
            Fracture fracture = fractureTransform.GetComponent<Fracture>();
            m_Fractures.Add(fracture);
        }
    }

    private float GetFracturesCost(Vector3 direction)
    {
        float power = 2.0f;
        float fractureCost = m_Fractures.Count;
        foreach (Fracture f in m_Fractures)
        {
            fractureCost -= Mathf.Pow(1.0f - Mathf.Abs(Vector3.Dot(direction, f.NormalVector)), power);
        }

        return fractureCost * m_FracturesWeight;
    }

    public void FindShortestPath(Vector3 start, Vector3 end, int initialNearestPointSearchDistance)
    {
        var points = m_PoissonSpheres.Points;

        int startIndex = m_PoissonSpheres.FindNearestPointsIndex(start, initialNearestPointSearchDistance);
        int endIndex = m_PoissonSpheres.FindNearestPointsIndex(end, initialNearestPointSearchDistance);
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

        /*Debug.Log(GetFracturesCost(new Vector3(1.0f, 1.0f, 1.0f).normalized));
        Debug.Log(GetFracturesCost(new Vector3(-1.0f, -1.0f, -1.0f).normalized));
        */
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

            Point point = points[n.PointIndex];
            foreach (NearestPoint child in point.NextList)
            {
                var childPoint = points[child.PointIndex];

                float horizonCost = GetHorizonCost(childPoint.Pos.y);
                float fractureCost = GetFracturesCost((point.Pos - childPoint.Pos).normalized);
                float gCost = n.GCost + child.Dist * (1.0f + horizonCost + fractureCost);

                float hCost = (childPoint.Pos - endPoint.Pos).magnitude * (1.0f + m_CheapestHorizon / m_FurthestApartConnectedSpheres);

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


        List<Point> pointsOnPath = new List<Point>();
        Node node = goalNode;
        while (node != null)
        {
            pointsOnPath.Add(points[node.PointIndex]);
            node = node.Previous;
        }

        m_Paths.Add(new Path(pointsOnPath, goalNode.GCost));
    }

    private class PathToPoint
    {
        private Path m_Path;
        private Point m_OtherPoint;

        public Point OtherPoint { get => m_OtherPoint;  }

        public PathToPoint(Path path, Point otherPoint)
        {
            m_Path = path;
            m_OtherPoint = otherPoint;
        }
    }

    private void PronePaths()
    {
        var points = m_PoissonSpheres.Points;

        Dictionary<Point, Dictionary<Point, Path>> dict = new Dictionary<Point, Dictionary<Point, Path>>();
        foreach (Path path in m_Paths)
        {
            if (!dict.ContainsKey(path.Start))
            {
                dict.Add(path.Start, new Dictionary<Point, Path>());
            }
            if (!dict.ContainsKey(path.End))
            {
                dict.Add(path.End, new Dictionary<Point, Path>());
            }
            dict[path.Start][path.End] = path;
            dict[path.End][path.Start] = path;
        }

        m_Paths.Clear();
        foreach (Point point in dict.Keys)
        {
            foreach (Point otherPoint in dict[point].Keys)
            {
                TryPronePath(dict, point, otherPoint);
            }
        }
    }

    private void TryPronePath(Dictionary<Point, Dictionary<Point, Path>> dict, Point startPoint, Point endPoint)
    {
        float pathCost = dict[startPoint][endPoint].Cost;
        foreach (var inBetweenPoint in dict[startPoint].Keys)
        {
            if (!dict[inBetweenPoint].Keys.Contains(endPoint)) continue;
            
            float pathToInBetweenPointCost = dict[startPoint][inBetweenPoint].Cost;
            float pathFromInBetweenPointCost = dict[inBetweenPoint][endPoint].Cost;

            if (Mathf.Pow(pathCost, m_ProningExponent) > Mathf.Pow(pathToInBetweenPointCost, m_ProningExponent) + Mathf.Pow(pathFromInBetweenPointCost, m_ProningExponent))
            {
                return;
            }
        }
        if (!m_Paths.Contains(dict[startPoint][endPoint])) m_Paths.Add(dict[startPoint][endPoint]);
    }
}
