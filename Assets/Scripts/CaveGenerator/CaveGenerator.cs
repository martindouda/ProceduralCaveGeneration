/*
 * Project: Procedural Generation of Cave Systems
 * File: CaveGenerator.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This is the core file of the entire project. It contains all the variables to configure the cave generation process.
 * It manages the entire process of generating Poisson spheres and the paths over them, which are then used by mesh generator
 * to generate all the meshes.
 */

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using static PoissonSpheres;

[ExecuteInEditMode, RequireComponent(typeof(SpherePool))]
public class CaveGenerator : MonoBehaviour
{
    [Space(20)][Header("PREFABS AND PARENTS")]
    [SerializeField] private Transform m_KeyPointsParent;
    [SerializeField] private Transform m_LineRenderersParent;
    [SerializeField] private Transform m_HorizonsParent;
    [SerializeField] private Transform m_FracturesParent;
    [SerializeField] private Transform m_StalactitesParent;
    [SerializeField] private Transform m_StalagmitesParent;

    [SerializeField] private LineRenderer m_LineRendererPrefab;

    [SerializeField] private SweepingPrimitiveGenerator m_SweepingPrimitiveGenerator;

    [SerializeField] private Material[] m_Materials = new Material[(int)PoissonSpheres.SphereType._SIZE];
    [Space(40), Header("1) SPHERES - POISSON SPHERES")]
    [SerializeField] private Vector3Int m_Size = new Vector3Int(50, 50, 50);
    [SerializeField, Range(0.5f, 5.0f)] private float m_MinSphereRadius = 1.0f;
    [SerializeField, Range(0.5f, 5.0f)] private float m_MaxSphereRadius = 3.0f;
    [SerializeField, Range(1.0f, 10.0f)] private float m_SpacingLimit = 2.0f;
    [SerializeField, Range(1, 100)] private int m_NumSamplesBeforeRejection = 30;
    [Space(20), Header("2) SPHERES - NEIGHBOUR CONNECTION")]
    [SerializeField, Range(1, 10)] private int m_SearchDistance = 5;
    [SerializeField, Range(1, 100)] private int m_IdealNumOfNearest = 30;
    [Space(20), Header("3) PATHS - GENERATION")]
    [SerializeField, Range(0.0f, 100.0f)] private float m_HorizonsWeight = 10.0f;
    [SerializeField, Range(0.0f, 100.0f)] private float m_FracturesWeight = 10.0f;
    [Space(20), Header("4) PATHS - PRONING")]
    [SerializeField, Range(0.0f, 30.0f)] private float m_ProningExponent = 1.0f;
    [Space(20), Header("5) PATHS - RAMIFICATION")]
    [SerializeField, Range(0.0f, 1.0f)] private float m_BranchesPerPathNodeCoefficient = 0.5f;
    [SerializeField, Range(0.0f, 100.0f)] private float m_MaxDistFromPath = 10.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_ProbabilityOfBranchSpawn = 0.5f;
    [Space(20), Header("6) MESH - SWEEPING PRIMITIVES")]
    [SerializeField, Range(0.1f, 10.0f)] private float m_TerrainEditsPerUnit = 2.0f;
    [SerializeField, Range(0.1f, 5.0f)] private float m_PrimitiveRadius = 2.5f;
    [SerializeField, Range(0.1f, 2.0f)] private float m_DiscRadius = 1.0f;
    [SerializeField, Range(0.01f, 1.0f)] private float m_DiscPower = 1.0f;
    [Space(20), Header("7) MESH - MARCHING CUBES")]
    [SerializeField, Range(0.2f, 2.0f)] private float m_MarchingCubesScale = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_MarchingCubesBoundry = 0.5f;
    [Space(20), Header("8) SPELEOTHEMS")]
    [SerializeField, Range(0.1f, 5.0f)] private float m_StalactitesMaxHeight = 2.0f;
    [SerializeField, Range(1.0f,45.0f)] private float m_CeilingAngleLimit = 15.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_StalactiteSpawnProbability = 0.1f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_StrawProbability = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_StalagmiteBelowStalactiteSpawnProbability = 0.5f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_StalagmiteHeightCoefficient = 0.5f;
    [SerializeField, Range(1.0f, 2.0f)] private float m_StalagmiteRadiusCoefficient = 1.2f;
    [Space(20), Header("9) CAVE LAKES")]
    [SerializeField, Range(1.0f, 100.0f)] private float m_GroundWaterLevelHeight = 15.0f;

    [SerializeField]

    // Poisson spheres
    private PoissonSpheres m_PoissonSpheres;
    [HideInInspector] public int VisualizedSphere = 0;

    private float m_FurthestApartConnectedSpheres = 0.0f;
    private SpherePool m_SpherePool; public SpherePool Pool { get => m_SpherePool; }

    // A* and paths
    private List<KeyPoint> m_KeyPoints;
    private List<Horizon> m_Horizons;
    private float m_CheapestHorizon = 0.0f;
    private List<float> m_CachedHorizonsHeights;
    private List<Fracture> m_Fractures;
    private List<Path> m_Paths;

    // Mesh generator
    private MeshGenerator m_MeshGenerator;

    [HideInInspector] public float GenerationTime = 0.0f;
    [HideInInspector] public float VisualizationTime = 0.0f;


    // Initializes the variables.
    private void InitializeVariables()
    {
        m_KeyPoints = new List<KeyPoint>();
        m_Horizons = new List<Horizon>();
        m_CachedHorizonsHeights = new List<float>();
        m_Fractures = new List<Fracture>();
        m_SpherePool = GetComponent<SpherePool>();
        m_SpherePool.CleanUpSpheresOnSceneLoad();
        m_Paths = new List<Path>();
        m_MeshGenerator = GetComponent<MeshGenerator>();
    }

    // Draw the borders of the cave and the horizons.
    private void OnDrawGizmos()
    {
        
        Gizmos.color = Color.cyan;
        for (int i = 0; i < m_HorizonsParent.childCount; i++)
        {
            if (Selection.activeGameObject == m_HorizonsParent.GetChild(i).gameObject)
            {
                Vector3 size = m_Size / 2;
                for (int j = 0; j < m_HorizonsParent.childCount; j++)
                {
                    float y = m_HorizonsParent.GetChild(j).position.y;
                    Gizmos.DrawLine(new Vector3(-size.x, y, -size.z) + transform.position, new Vector3(size.x, y, size.z) + transform.position);
                    Gizmos.DrawLine(new Vector3(size.x, y, -size.z) + transform.position, new Vector3(-size.x, y, size.z) + transform.position);
                }
                break;
            }
        }
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(transform.position.x, transform.position.y + m_Size.y / 2, transform.position.z), m_Size);
    }

    // Generates poisson spheres.
    public void GeneratePoissonSpheres()
    {
        float time = Time.realtimeSinceStartup;

        InitializeVariables();
        ClearPaths();
        m_MeshGenerator.DeleteCaveMesh();
        m_MeshGenerator.ClearBotSpeleo();
        m_MeshGenerator.ClearTopSpeleo();
        m_MeshGenerator.DeleteWaterMesh();
        

        m_PoissonSpheres = new PoissonSpheres(m_Size, m_MinSphereRadius, m_MaxSphereRadius, m_SpacingLimit);
        m_PoissonSpheres.GeneratePoints(m_NumSamplesBeforeRejection);
        ConnectNearest(m_SearchDistance, m_IdealNumOfNearest);

        GenerationTime = Time.realtimeSinceStartup - time;

        // Tries to connect at most 'idealNumOfNeighbours' closest neighbours in a cube with side of 2 * 'searchDist'.
        void ConnectNearest(int searchDist, int idealNumOfNeighbours)
        {
            var points = m_PoissonSpheres.Points;
            var grid = m_PoissonSpheres.Grid;

            //object furthestApartSpheresLock = new object();
            //Parallel.For(0, points.Count, (i) => { 
            for (int i = 0; i < points.Count; i++)
            {
                Point p = points[i];
                Vector3Int gridPos = m_PoissonSpheres.GetGridPos(p.Pos);

                int startX = Mathf.Max(gridPos.x - searchDist, 0);
                int endX = Mathf.Min(gridPos.x + searchDist, m_PoissonSpheres.GridSizeX - 1);
                int startY = Mathf.Max(gridPos.y - searchDist, 0);
                int endY = Mathf.Min(gridPos.y + searchDist, m_PoissonSpheres.GridSizeY - 1);
                int startZ = Mathf.Max(gridPos.z - searchDist, 0);
                int endZ = Mathf.Min(gridPos.z + searchDist, m_PoissonSpheres.GridSizeZ - 1);

                int searchWidth = (int)(2.0f * searchDist) + 1;
                Heap<NearestPoint> heap = new Heap<NearestPoint>(searchWidth * searchWidth * searchWidth);



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

                heap.Pop(); // Pop itself from the heap;
                while (heap.Count > 0 && p.NextList.Count < idealNumOfNeighbours)
                {
                    var item = heap.Pop();
                    item.CalculateDist();
                    p.NextList.Add(item);


                    m_FurthestApartConnectedSpheres = Mathf.Max(item.Dist, m_FurthestApartConnectedSpheres);

                }
            }//);
             //Debug.Log(m_FurthestApartConnectedSpheres);
             //Debug.Log("min: " + m_MinNearest + ", max: " + m_MaxNearest);
        }
    }

    // Generates paths over the graph created by poisson spheres.
    public void GeneratePaths()
    {
        if (!CheckSpheresDistributionReady()) return;

        LoadKeyPoints();
        LoadHorizons();
        LoadFractures();
        m_Paths.Clear();

        Vector3[] keyPointsPositions = new Vector3[m_KeyPoints.Count];
        for (int i = 0; i < m_KeyPoints.Count; i++)
        {
            keyPointsPositions[i] = m_KeyPoints[i].transform.position;
        }

        Parallel.For(0, m_KeyPoints.Count - 1, (i) =>
        {
            for (int j = i + 1; j < m_KeyPoints.Count; j++)
            {
                FindShortestPath(m_PoissonSpheres.GetNearestPoint(keyPointsPositions[i], m_SearchDistance), m_PoissonSpheres.GetNearestPoint(keyPointsPositions[j], m_SearchDistance));
            }
        });

        PronePaths();
        GenerateBranches();

        foreach (var sphere in m_PoissonSpheres.Points)
        {
            sphere.VisualSphereType = SphereType.WHITE;
        }
        foreach (var path in m_Paths)
        {
            path.ColorPath();
        }

        // Prones a path between two points if there is a cheaper alternative passing through another point.
        void PronePaths()
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

        // Generates additional tunnels spreading from the paths.
        void GenerateBranches()
        {
            int pathsSizeBeforeAddition = m_Paths.Count;
            for (int i = 0; i < pathsSizeBeforeAddition; i++)
            {
                Path path = m_Paths[i];
                for (int j = 0; j < path.Points.Count * m_BranchesPerPathNodeCoefficient; j++)
                {
                    if (Random.value > m_ProbabilityOfBranchSpawn) continue;

                    Point pointOnPath = path.Points[Random.Range(0, path.Points.Count)];
                    Vector3 randomPos = Random.insideUnitSphere * m_MaxDistFromPath;
                    Point randomPoint = m_PoissonSpheres.GetNearestPoint(pointOnPath.Pos - new Vector3(m_PoissonSpheres.GridSizeX / 2.0f, 0.0f, m_PoissonSpheres.GridSizeZ / 2.0f) + randomPos, m_SearchDistance);

                    if (randomPoint == null) continue;

                    FindShortestPath(pointOnPath, randomPoint);
                }
            }
        }
    }

    // Calls all the functions in order to generate the entirity of the cave mesh.
    public void GenerateMesh()
    { 
        m_SweepingPrimitiveGenerator.LoadPixels();
        m_MeshGenerator.GenerateCaveMesh(m_Size, m_MarchingCubesScale, m_MarchingCubesBoundry, m_DiscPower, m_PrimitiveRadius, m_DiscRadius, m_CeilingAngleLimit);
        m_MeshGenerator.SweepPrimitives(m_Paths, m_TerrainEditsPerUnit, m_SweepingPrimitiveGenerator);
        m_MeshGenerator.PerformMarchingCubes();
        m_MeshGenerator.UpdateMesh();
    }

    // Caches the keypoints. 
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

    // Caches the horizons and their heights => it is necessary for multithreading (get_transform can be called only from the main thread).
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

    // Caches the fractures.
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

    // Class used by A* algorithm to find the cheapest path between two key points.
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


    private object m_PathsLock = new object();
    // Uses heap and A* to find the shortest path through Poisson's spheres conected to one another.
    public void FindShortestPath(Point startPoint, Point endPoint)
    {
        var points = m_PoissonSpheres.Points;


        bool[] closed = new bool[points.Count];
        float[] lowestFCost = new float[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            lowestFCost[i] = Mathf.Infinity;
        }


        Heap<Node> open = new Heap<Node>(points.Count * m_IdealNumOfNearest);
        float startHCost = (startPoint.Pos - endPoint.Pos).magnitude;
        open.Add(new Node(startPoint.Index, null, 0.0f, startHCost));
        lowestFCost[startPoint.Index] = startHCost;


        Node goalNode = null;
        while (open.Count > 0)
        {
            Node n = open.Pop();
            if (lowestFCost[n.PointIndex] < n.FCost) continue;

            if (n.PointIndex == endPoint.Index)
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
        lock(m_PathsLock)
        {
            m_Paths.Add(new Path(pointsOnPath, goalNode.GCost));
        }

        // Returns smoothly interpolated cost of the closest horizon above and below.
        float GetHorizonCost(float height)
        {
            for (int i = 1; i < m_Horizons.Count; i++)
            {
                if (height < m_CachedHorizonsHeights[i])
                {
                    // smooth step function
                    float normalizedDistanceBetweenHorizons = (height - m_CachedHorizonsHeights[i - 1]) / (m_CachedHorizonsHeights[i] - m_CachedHorizonsHeights[i - 1]);
                    return Mathf.SmoothStep(m_Horizons[i - 1].Cost, m_Horizons[i].Cost, normalizedDistanceBetweenHorizons) * m_HorizonsWeight;
                }
            }
            return 0.0f;
        }

        // Returns the cost of traveling in a direction.
        float GetFracturesCost(Vector3 direction)
        {
            float power = 2.0f;
            float fractureCost = m_Fractures.Count;
            foreach (Fracture f in m_Fractures)
            {
                fractureCost -= Mathf.Pow(1.0f - Mathf.Abs(Vector3.Dot(direction, f.NormalVector)), power);
            }

            return fractureCost * m_FracturesWeight;
        }
    }

    // Tries to prone a certain path.
    private void TryPronePath(Dictionary<Point, Dictionary<Point, Path>> dict, Point startPoint, Point endPoint)
    {
        float pathCost = dict[startPoint][endPoint].Cost;
        foreach (var inBetweenPoint in dict[startPoint].Keys)
        {
            if (!dict[inBetweenPoint].Keys.Contains(endPoint)) continue;
            
            float pathToInBetweenPointCost = dict[startPoint][inBetweenPoint].Cost;
            float pathFromInBetweenPointCost = dict[inBetweenPoint][endPoint].Cost;

            if (System.Math.Pow((double)pathCost, (double)m_ProningExponent) > System.Math.Pow((double)pathToInBetweenPointCost, (double)m_ProningExponent) + System.Math.Pow((double)pathFromInBetweenPointCost, (double)m_ProningExponent))
            {
                return;
            }
        }
        if (!m_Paths.Contains(dict[startPoint][endPoint])) m_Paths.Add(dict[startPoint][endPoint]);
    }

    // Generates the stalactites using current parameters.
    public void GenerateStalactites()
    {
        if (!CheckSpheresDistributionReady()) return;
        if (!CheckPathsGenerated()) return;   
        m_MeshGenerator.GenerateSpeleothems(m_StalactiteSpawnProbability, m_StalactitesMaxHeight, m_StrawProbability, m_StalagmiteBelowStalactiteSpawnProbability, m_StalagmiteHeightCoefficient, m_StalagmiteRadiusCoefficient);
    }

    // Generates cave lakes using the current parameters.
    public void GenerateWater()
    {
        if (!CheckSpheresDistributionReady()) return;
        if (!CheckPathsGenerated()) return;
        m_MeshGenerator.FindLowGrounds((int)(m_GroundWaterLevelHeight / m_MarchingCubesScale));
    }

    // Returns the sphere distribution state.
    public bool CheckSpheresDistributionReady()
    {
        if (m_PoissonSpheres != null) return true;
  
        Debug.LogWarning("Distribute spheres first!");
        return false;
    }

    // Returns the paths state.
    public bool CheckPathsGenerated()
    {
        if (m_Paths.Count != 0) return true;

        Debug.LogWarning("Generate paths first!");
        return false;
    }

    public void ClearPaths()
    {
        for (int i = m_LineRenderersParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(m_LineRenderersParent.GetChild(i).gameObject);
        }
    }

    // Enables or disables the path visualization.
    public void RenderPaths(bool visible)
    {
        if (visible)
        {
            ClearPaths();
            if (m_Paths == null) return;
            foreach (var path in m_Paths)
            {
                path.AddLineRenderer(m_PoissonSpheres, Instantiate(m_LineRendererPrefab, m_LineRenderersParent));
            }
        }
        else
        {
            ClearPaths();
        }
    }

    // Enables or disables the visibility of the caves mesh.
    public void RenderMesh(bool value)
    {
        GetComponent<MeshRenderer>().enabled = value;
    }

    // Updates the visiility of the poisson spheres.
    public void RenderPoissonSpheres(int option, bool neighbourVisualization)
    {
        m_SpherePool.NewRound();

        foreach (var sphere in m_PoissonSpheres.Points)
        {
            sphere.VisualSphereType = SphereType.WHITE;
        }
        foreach (var path in m_Paths)
        {
            path.ColorPath();
        }

        List<Point> points = m_PoissonSpheres.Points;
        Vector3 toCenterOffset = new Vector3(m_Size.x / 2, 0.0f, m_Size.z / 2);
        switch (option)
        {
            case 0:
                {
                    break;
                }
            case 1:
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (points[i].VisualSphereType != SphereType.GREEN) continue;
                        m_SpherePool.WakeSphere(points[i].Pos - toCenterOffset, 0.8f, m_Materials[(int)points[i].VisualSphereType], i);
                    }
                    break;
                }
            case 2:
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (points[i].VisualSphereType != SphereType.GREEN) continue;
                        m_SpherePool.WakeSphere(points[i].Pos - toCenterOffset, points[i].Radius * 2.0f, m_Materials[(int)points[i].VisualSphereType], i);
                    }
                    break;
                }
            case 3:
                {
                    if (VisualizedSphere > points.Count) VisualizedSphere = 0;

                    var examinedPoint = points[VisualizedSphere];
                    if (neighbourVisualization)
                    {
                        examinedPoint.VisualSphereType = PoissonSpheres.SphereType.BLUE;
                        foreach (var nearestPoint in examinedPoint.NextList)
                        {
                            points[nearestPoint.PointIndex].VisualSphereType = PoissonSpheres.SphereType.RED;
                        }
                    }
                    
                    for (int i = 0; i < points.Count; i++)
                    {
                        m_SpherePool.WakeSphere(points[i].Pos - toCenterOffset, points[i].Radius * 2.0f, m_Materials[(int)points[i].VisualSphereType], i);
                    }
                    break;
                }
        }
        m_SpherePool.PutUnusedToSleep();
    }

    // Enables or disables the key points visibility.
    public void RenderKeyPoints(bool value)
    {
        m_KeyPointsParent.gameObject.SetActive(value);
    }

    // Enables or disables the speleothems visibility.
    public void RenderSpeleothems(bool value)
    {
        m_StalactitesParent.gameObject.SetActive(value);
        m_StalagmitesParent.gameObject.SetActive(value);
    }

    // Enables or disables the cave lakes visiility.
    public void RenderWater(bool renderWater)
    {
        m_MeshGenerator.RenderWater(renderWater);
    }
}
