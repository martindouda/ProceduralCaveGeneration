using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.HighDefinition.ScalableSettingLevelParameter;


[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    private Mesh m_Mesh;
    
    private List<Vector3> m_Vertices;
    private List<Vector3> m_Normals;
    private List<int> m_Indices;

    private List<Vector3> m_TopSpeleosLocations;

    private Vector3 m_Size;
    private Vector3Int m_ArraySize;

    private float m_Boundry = 0.5f;
    private float m_Scale = 1.0f;
    private float m_EditPower = 1.0f;
    private float m_PrimitiveRadius;
    private float m_DiscRadius;

    Dictionary<Vector3, int> m_VertexDict;

    private float[] m_Grid;

    // Speleothems
    [SerializeField] private Transform m_SpeleoNormalPrefab;
    [SerializeField] private Transform m_SpeleoStrawPrefab;
    [SerializeField] private Transform m_TopSpeleoParent;
    [SerializeField] private Transform m_BotSpeleoParent;
    [SerializeField, Range(1.0f, 45.0f)] private float m_AngleLimit = 3.0f;

    public void Generate(Vector3 sizeFloat, float scale, float boundry, float editPower, float primitiveRadius, float discRaius)
    {
        Vector3Int size = new Vector3Int((int)(sizeFloat.x / scale), (int)(sizeFloat.y / scale), (int)(sizeFloat.z / scale));
        m_Size = size;
        m_ArraySize = size + Vector3Int.one * Mathf.CeilToInt(primitiveRadius / scale) * 2;
        m_Scale = scale;
        m_Boundry = boundry;
        m_EditPower = editPower;
        m_PrimitiveRadius = primitiveRadius / scale;
        m_DiscRadius = discRaius / scale;
        m_Mesh = new Mesh();
        m_Mesh.name = "CaveMesh";
        GetComponent<MeshFilter>().mesh = m_Mesh;

        CreateGrid();
        CreateShape();
        UpdateMesh();

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = m_Mesh;
        meshCollider.enabled = false;
    }

    private void CreateGrid()
    {
        m_Grid = new float[(m_ArraySize.x + 1) * (m_ArraySize.z + 1) * (m_ArraySize.y + 1)];
        for (int y = 0; y < m_ArraySize.y; y++)
        {
            for (int z = 0; z <= m_ArraySize.z; z++)
            {
                for (int x = 0; x <= m_ArraySize.x; x++)
                {
                    m_Grid[x + z * (m_ArraySize.x + 1) + y * (m_ArraySize.x + 1) * (m_ArraySize.z + 1)] = 1.0f;
                }
            }
        }
    }

    public void SweepPrimitives(List<Path> paths, float terrainEditsPerUnit, SweepingPrimitiveGenerator sweepingPrimitiveGenerator)
    {
        foreach (var path in paths)
        {
            float pathLength = 0.0f;
            for (int i = 0; i < path.Points.Count - 1; i++)
            {
                pathLength += (path.Points[i].Pos - path.Points[i + 1].Pos).magnitude;
            }
            int stepsCount = Mathf.CeilToInt(pathLength * terrainEditsPerUnit);
            float stepSize = pathLength / stepsCount;
            for (int i = 0; i < stepsCount; i++)
            {
                float continousIndex = (i * stepSize) / pathLength * (path.Points.Count - 1);
                Vector3 tangent = (path.Points[(int)continousIndex].Pos - path.Points[(int)continousIndex + 1].Pos).normalized;
                Vector3 posToEdit = Vector3.Lerp(path.Points[(int)continousIndex].Pos, path.Points[(int)continousIndex + 1].Pos, continousIndex - (int)continousIndex) / m_Scale;

                Vector3 pos = posToEdit - new Vector3(m_Size.x / 2.0f, 0.0f, m_Size.z / 2.0f);
                var primitivePoints = sweepingPrimitiveGenerator.GeneratePoints(tangent, pos.y / m_Size.y, m_PrimitiveRadius, m_DiscRadius);
                foreach (var point in primitivePoints)
                {
                    RemoveFromTerrain(pos + point);
                }
            }
        }
    }

    public void CreateShape()
    {
        ClearTopSpeleo();
        ClearBotSpeleo();
            
        m_Vertices = new List<Vector3>();
        m_Normals = new List<Vector3>();
        m_Indices = new List<int>();
        m_VertexDict = new Dictionary<Vector3, int>();
        m_TopSpeleosLocations = new List<Vector3>();

        for (int y = 0; y < m_ArraySize.y; y++)
        {
            for (int z = 0; z < m_ArraySize.z; z++)
            {
                for (int x = 0; x < m_ArraySize.x; x++)
                {
                    int cubeIndex = 0;

                    float[] cubeValues = new float[]
                    {
                        GetFromGrid(x,      y,      z + 1),
                        GetFromGrid(x + 1,  y,      z + 1),
                        GetFromGrid(x + 1,  y,      z),
                        GetFromGrid(x,      y,      z),
                        GetFromGrid(x,      y + 1,  z + 1),
                        GetFromGrid(x + 1,  y + 1,  z + 1),
                        GetFromGrid(x + 1,  y + 1,  z),
                        GetFromGrid(x,      y + 1,  z),
                    };


                    for (int i = 0; i < 8; i++)
                    {
                        if (cubeValues[i] >= m_Boundry) cubeIndex |= 1 << i;
                    }

                    int[] edges = MarchingCubesTables.triTable[cubeIndex];

                    for (int i = 0; edges[i] != -1; i += 3)
                    {
                        int a0 = MarchingCubesTables.edgeConnections[edges[i]][0];
                        int a1 = MarchingCubesTables.edgeConnections[edges[i]][1];

                        int b0 = MarchingCubesTables.edgeConnections[edges[i + 1]][0];
                        int b1 = MarchingCubesTables.edgeConnections[edges[i + 1]][1];

                        int c0 = MarchingCubesTables.edgeConnections[edges[i + 2]][0];
                        int c1 = MarchingCubesTables.edgeConnections[edges[i + 2]][1];

                        Vector3 pos = new Vector3(x - m_ArraySize.x / 2f, y - Mathf.CeilToInt(m_PrimitiveRadius), z - m_ArraySize.z / 2f) * m_Scale;


                        Vector3 vertexPos1 = GetMarchingCubesVertex(pos, MarchingCubesTables.cubeCorners[a0], cubeValues[a0], MarchingCubesTables.cubeCorners[a1], cubeValues[a1]);
                        Vector3 vertexPos2 = GetMarchingCubesVertex(pos, MarchingCubesTables.cubeCorners[c0], cubeValues[c0], MarchingCubesTables.cubeCorners[c1], cubeValues[c1]);
                        Vector3 vertexPos3 = GetMarchingCubesVertex(pos, MarchingCubesTables.cubeCorners[b0], cubeValues[b0], MarchingCubesTables.cubeCorners[b1], cubeValues[b1]);

                        Vector3 normal = Vector3.Cross(vertexPos2 - vertexPos1, vertexPos3 - vertexPos1).normalized;

                        if (Vector3.Dot(normal, Vector3.down) > Mathf.Cos(Mathf.Deg2Rad * m_AngleLimit))    
                        {
                            float u = Random.value;
                            float v = Random.value;
                            if (u + v > 1.0f)
                            {
                                u = 1.0f - u;
                                v = 1.0f - v;
                            }
                            float w = 1.0f - u - v;
                            m_TopSpeleosLocations.Add(vertexPos1 * u + vertexPos2 * v + vertexPos3 * w);
                        }
                        AddVertex(vertexPos1, normal);
                        AddVertex(vertexPos2, normal);
                        AddVertex(vertexPos3, normal);
                    }
                }
            }
        }
        for (int i = 0; i < m_Normals.Count; i++)
        {
            m_Normals[i] = m_Normals[i].normalized;
        }
    }

    private float GetFromGrid(int x, int y, int z)
    {
        return m_Grid[x + z * (m_ArraySize.x + 1) + y * (m_ArraySize.x + 1) * (m_ArraySize.z + 1)];
    }

    private Vector3 GetMarchingCubesVertex(Vector3 pos, Vector3 vert0, float val0, Vector3 vert1, float val1) {
        Vector3 ret = pos + (vert0 + (m_Boundry - val0) * (vert1 - vert0) / (val1 - val0)) * m_Scale;
        return new Vector3(((int)(ret.x * 100.0f + 0.5f)) / 100.0f, ((int)(ret.y * 100.0f + 0.5f)) / 100.0f, ((int)(ret.z * 100.0f + 0.5f)) / 100.0f);
    }

    private void AddVertex(Vector3 pos, Vector3 normal)
    {
        if (m_VertexDict.ContainsKey(pos))
        {
            m_Indices.Add(m_VertexDict[pos]);
            m_Normals[m_VertexDict[pos]] += normal;
            return;
        }
        m_VertexDict[pos] = m_Vertices.Count;
        m_Indices.Add(m_Vertices.Count);
        m_Vertices.Add(pos);
        m_Normals.Add(normal);
    }

    public void UpdateMesh()
    {
        m_Mesh.Clear();
        m_Mesh.SetVertices(m_Vertices);
        m_Mesh.SetNormals(m_Normals);
        m_Mesh.SetTriangles(m_Indices, 0);
    }
    
    public void RemoveFromTerrain(Vector3 worldPos)
    {
        for (float y = -m_PrimitiveRadius; y <= m_PrimitiveRadius; y++)
        {
            for (float z = -m_PrimitiveRadius; z <= m_PrimitiveRadius; z++)
            {
                for (float x = -m_PrimitiveRadius; x <= m_PrimitiveRadius; x++)
                {
                    Vector3 pos = new Vector3(worldPos.x + x, worldPos.y + y, worldPos.z + z);
                    float distance = (worldPos - pos).magnitude / m_PrimitiveRadius;
                    if (distance > 1.0f) continue;
                    
                    Vector3Int gridPos = new Vector3Int((int)(pos.x + m_ArraySize.x / 2f + .5f), (int)(pos.y + Mathf.CeilToInt(m_PrimitiveRadius) + .5f), (int)(pos.z + m_ArraySize.z / 2f + .5f));

                    if (gridPos.y < 0 || m_ArraySize.x < gridPos.y || gridPos.z < 0 || m_ArraySize.z < gridPos.z || gridPos.x < 0 || m_ArraySize.x < gridPos.x) continue;

                    m_Grid[gridPos.x + gridPos.z * (m_ArraySize.x + 1) + gridPos.y * (m_ArraySize.x + 1) * (m_ArraySize.z + 1)] -= (1 - distance) * (1 - distance) * m_EditPower;
                    Mathf.Clamp(m_Grid[gridPos.x + gridPos.z * (m_ArraySize.x + 1) + gridPos.y * (m_ArraySize.x + 1) * (m_ArraySize.z + 1)], 0.0f, 1.0f);
                }
            }
        }
    }


    public void ClearTopSpeleo()
    {
        while (m_TopSpeleoParent.childCount > 0)
        {
            DestroyImmediate(m_TopSpeleoParent.GetChild(0).gameObject);
        }
    }

    public void ClearBotSpeleo()
    {
        while (m_BotSpeleoParent.childCount > 0)
        {
            DestroyImmediate(m_BotSpeleoParent.GetChild(0).gameObject);
        }
    }

    [SerializeField, Range(0.0f, 1.0f)] private float m_StalagmiteHeightCoefficient = 0.5f;
    [SerializeField, Range(1.0f, 2.0f)] private float m_StalagmiteRadiusCoefficient = 1.2f;

    [SerializeField] private float m_Radius = 0.05f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_RadiusFluctuation = 0.2f;

    [SerializeField] private float m_WidthExponent = 2.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_WidthExponentFluctutation = 1.0f;


    public void GenerateSpeleothems(float spawnProbability, float stalagmiteProability, float strawProbability, float maxHeight)
    {
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.enabled = true;
        ClearTopSpeleo();
        ClearBotSpeleo();

        Vector3 upCorrection = Vector3.up * Mathf.Sin(m_AngleLimit) * m_Radius * (1 + m_RadiusFluctuation);
        foreach (var pos in m_TopSpeleosLocations)
        {
            if (Random.value > spawnProbability) continue;

            float randVal = Mathf.Clamp(1.0f - Random.value * Random.value, 0.1f, 1.0f);
            if (Random.value < strawProbability)
            {
                float strawHeight = maxHeight * randVal                                                                             * 0.5f;
                float strawRadius = m_Radius * randVal * (1 + m_RadiusFluctuation * (Random.value * 2 - 1.0f))                      * 0.2f;
                float strawWidthExponent = m_WidthExponent                                                                          * 10.0f;
                GenerateTopSpeleo(pos + upCorrection, strawHeight, strawRadius, strawWidthExponent, SpeleoType.Straw);
            }
            else
            {
                float height = maxHeight * randVal;
                float radius = m_Radius * randVal * (1 + m_RadiusFluctuation * (Random.value * 2 - 1.0f));
                float widthExponent = m_WidthExponent * (1 + m_WidthExponentFluctutation * Random.Range(-1.0f, 1.0f));
                BotSpeleo botSpeleo = GenerateTopSpeleo(pos + upCorrection, height, radius, widthExponent, SpeleoType.Normal);

                if (!botSpeleo.generate || Random.value > stalagmiteProability) continue;

                height = height * m_StalagmiteHeightCoefficient;
                radius = radius * m_StalagmiteRadiusCoefficient;
                widthExponent = widthExponent + 2.0f;
                GenerateBotSpeleo(botSpeleo.pos - upCorrection, height, radius, widthExponent);
            }
        }
    }

    enum SpeleoType
    {
        Normal, Straw
    }

    struct BotSpeleo
    {
        public Vector3 pos;
        public float height;

        public bool generate;
    }

    [SerializeField] private int m_NumPointsHorizontal = 20;
    [SerializeField] private int m_NumPointsVertical = 20;

    private BotSpeleo GenerateTopSpeleo(Vector3 pos, float height, float radius, float widthExponent, SpeleoType speleoType)
    {
        BotSpeleo ret = new BotSpeleo() { generate = false };

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        float segmentHeight = height / (m_NumPointsVertical - 1.0f);

        for (int y = 0; y < m_NumPointsVertical; y++)
        {
            for (int i = 0; i < m_NumPointsHorizontal; i++)
            {
                float angle = (float)i / m_NumPointsHorizontal * Mathf.PI * 2.0f;
                float level = 1.0f - Mathf.Pow((float)y / m_NumPointsVertical, widthExponent);
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius * level, -y * segmentHeight, Mathf.Sin(angle) * radius * level));
            }
        }
        Vector3 tipPos;
        if (widthExponent > 1.0f)
        {
            tipPos = new Vector3(0.0f, -(m_NumPointsVertical - 1) * segmentHeight, 0.0f);
        }
        else
        {
            tipPos = new Vector3(0.0f, -m_NumPointsVertical * segmentHeight, 0.0f);
        }
        vertices.Add(tipPos);


        Ray ray = new Ray(pos, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 20.0f))
        {
            MeshCollider meshCollider = hit.collider as MeshCollider;

            if (meshCollider != null)
            {
                if (Vector3.Dot(hit.normal, Vector3.up) < Mathf.Cos(Mathf.Deg2Rad * m_AngleLimit)) return ret;

                ret.generate = true;
                ret.pos = hit.point;
            }
        }


        for (int y = 0; y < m_NumPointsVertical - 1; y++)
        {
            for (int i = 0; i < m_NumPointsHorizontal; i++)
            {
                indices.Add(i + y * m_NumPointsHorizontal);
                indices.Add((i + 1) % m_NumPointsHorizontal + y * m_NumPointsHorizontal);
                indices.Add(i + (y + 1) * m_NumPointsHorizontal);

                indices.Add((i + 1) % m_NumPointsHorizontal + y * m_NumPointsHorizontal);
                indices.Add((i + 1) % m_NumPointsHorizontal + (y + 1) * m_NumPointsHorizontal);
                indices.Add(i + (y + 1) * m_NumPointsHorizontal);
            }
        }
        for (int i = 0; i < m_NumPointsHorizontal; i++)
        {
            indices.Add(i + (m_NumPointsVertical - 1) * m_NumPointsHorizontal);
            indices.Add((i + 1) % m_NumPointsHorizontal + (m_NumPointsVertical - 1) * m_NumPointsHorizontal);
            indices.Add(m_NumPointsVertical * m_NumPointsHorizontal);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();

        Transform t;

        if (speleoType == SpeleoType.Straw) t = Instantiate(m_SpeleoStrawPrefab, m_TopSpeleoParent);
        else t = Instantiate(m_SpeleoNormalPrefab, m_TopSpeleoParent);

        t.position = pos;
        t.GetComponentInChildren<MeshFilter>().mesh = mesh;

        return ret;
    }

    private void GenerateBotSpeleo(Vector3 pos, float height, float radius, float widthExponent)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        float segmentHeight = height / (m_NumPointsVertical - 1.0f);

        for (int y = 0; y < m_NumPointsVertical; y++)
        {
            for (int i = 0; i < m_NumPointsHorizontal; i++)
            {
                float angle = (float)i / m_NumPointsHorizontal * Mathf.PI * 2.0f;
                float level = 1.0f - Mathf.Pow((float)y / m_NumPointsVertical, widthExponent);
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius * level, y * segmentHeight, Mathf.Sin(angle) * radius * level));
            }
        }
        Vector3 tipPos = new Vector3(0.0f, (m_NumPointsVertical - 1) * segmentHeight, 0.0f);
        vertices.Add(tipPos);

        for (int y = 0; y < m_NumPointsVertical - 1; y++)
        {
            for (int i = 0; i < m_NumPointsHorizontal; i++)
            {
                indices.Add(i + y * m_NumPointsHorizontal);
                indices.Add(i + (y + 1) * m_NumPointsHorizontal);
                indices.Add((i + 1) % m_NumPointsHorizontal + y * m_NumPointsHorizontal);

                indices.Add((i + 1) % m_NumPointsHorizontal + y * m_NumPointsHorizontal);
                indices.Add(i + (y + 1) * m_NumPointsHorizontal);
                indices.Add((i + 1) % m_NumPointsHorizontal + (y + 1) * m_NumPointsHorizontal);
            }
        }
        for (int i = 0; i < m_NumPointsHorizontal; i++)
        {
            indices.Add(i + (m_NumPointsVertical - 1) * m_NumPointsHorizontal);
            indices.Add(m_NumPointsVertical * m_NumPointsHorizontal);
            indices.Add((i + 1) % m_NumPointsHorizontal + (m_NumPointsVertical - 1) * m_NumPointsHorizontal);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
                
        Transform t = Instantiate(m_SpeleoNormalPrefab, m_BotSpeleoParent);
        t.position = pos;
        t.GetComponentInChildren<MeshFilter>().mesh = mesh;
    }


    private List<Vector3Int> m_Minimas = new List<Vector3Int>();
    private List<Vector3Int> m_HighestPointsNearMinimas = new List<Vector3Int>();
    private List<Vector3Int> m_TEMP = new List<Vector3Int>();
    [SerializeField] private int m_SearchHeight = 1;
    public void FindLowGrounds()
    {
        m_Minimas = new List<Vector3Int>();
        m_HighestPointsNearMinimas = new List<Vector3Int>();
        m_TEMP = new List<Vector3Int>();
        bool[] visited = new bool[(m_ArraySize.x + 1) * (m_ArraySize.z + 1) * (m_ArraySize.y + 1)];
        bool GetVisited(int x, int y, int z) { return visited[x + z * (m_ArraySize.x + 1) + y * (m_ArraySize.x + 1) * (m_ArraySize.z + 1)]; }
        void SetVisited(int x, int y, int z) { visited[x + z * (m_ArraySize.x + 1) + y * (m_ArraySize.x + 1) * (m_ArraySize.z + 1)] = true; }
        for (int y = 0; y < m_ArraySize.y; y++)
        {
            for (int z = 0; z <= m_ArraySize.z; z++)
            {
                for (int x = 0; x <= m_ArraySize.x; x++)
                {
                    visited[x + z * (m_ArraySize.x + 1) + y * (m_ArraySize.x + 1) * (m_ArraySize.z + 1)] = false;
                }
            }
        }

        int[,] neighboursOnLevel = new int[,] { { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, -1 }, { 0, 1 }, { 1, -1 }, { 1, 0 }, { 1, 1 } };

        for (int y = 0; y < m_ArraySize.y; y++)
        {
            for (int z = 0; z <= m_ArraySize.z; z++)
            {
                for (int x = 0; x <= m_ArraySize.x; x++)
                {
                    if (GetFromGrid(x, y, z) > m_Boundry || GetVisited(x, y, z)) continue;

                    ExplorePancake(new Vector3Int(x, y, z));
                }
            }
        }
        
        for (int i = 0; i < m_HighestPointsNearMinimas.Count; i++)
        {
            CreateWaterShape(m_HighestPointsNearMinimas[i]);
        }

        void ExplorePancake(Vector3Int start)
        {
            Vector3Int highestPoint = start;
            Queue<Vector3Int> q = new Queue<Vector3Int>();
            Queue<Vector3Int> aboveQ = new Queue<Vector3Int>();
            HashSet<Vector3Int> pancakeVisited = new HashSet<Vector3Int>();
            q.Enqueue(start);
            SetVisited(start.x, start.y, start.z);
            pancakeVisited.Add(start);

            bool continuesDown = false;
            for (int sh = 0; sh < m_SearchHeight; sh++)
            {
                while (q.Count > 0)
                {
                    Vector3Int v = q.Dequeue();
                    if (v.y > highestPoint.y) { highestPoint = v; }
                    if (v.y > 0 && GetFromGrid(v.x, v.y - 1, v.z) <= m_Boundry && !pancakeVisited.Contains(new Vector3Int(v.x, v.y - 1, v.z))) continuesDown = true;

                    if (v.y < m_ArraySize.y && GetFromGrid(v.x, v.y + 1, v.z) <= m_Boundry && start.y - v.y + 1 < m_SearchHeight)
                    {
                        Vector3Int above = new Vector3Int(v.x, v.y + 1, v.z);
                        aboveQ.Enqueue(above);
                        pancakeVisited.Add(above);
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int next = new Vector3Int(v.x + neighboursOnLevel[i, 0], v.y, v.z + neighboursOnLevel[i, 1]);
                        if (next.x < 0 || next.x >= m_ArraySize.x || next.z < 0 || next.z >= m_ArraySize.z || GetFromGrid(next.x, next.y, next.z) > m_Boundry) continue;

                        if (pancakeVisited.Contains(next)) continue;

                        q.Enqueue(next);
                        SetVisited(next.x, next.y, next.z);
                        pancakeVisited.Add(next);
                    }
                }
                q = aboveQ;
                aboveQ = new Queue<Vector3Int>();
            }
            if (!continuesDown)
            {
                m_Minimas.Add(start);
                m_HighestPointsNearMinimas.Add(highestPoint);
            }
        }

        void CreateWaterShape(Vector3Int start)
        {
            List<Vector3Int> positions = new List<Vector3Int>();
            Queue<Vector3Int> q = new Queue<Vector3Int>();
            HashSet<Vector3Int> pancakeVisited = new HashSet<Vector3Int>();
            q.Enqueue(start);
            pancakeVisited.Add(start);
            positions.Add(start);
            m_TEMP.Add(start);

            for (int sh = 0; sh < m_SearchHeight; sh++)
            {
                while (q.Count > 0)
                {
                    Vector3Int v = q.Dequeue();
                    positions.Add(v);
                    
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int next = new Vector3Int(v.x + neighboursOnLevel[i, 0], v.y, v.z + neighboursOnLevel[i, 1]);
                        if (next.x < 0 || next.x >= m_ArraySize.x || next.z < 0 || next.z >= m_ArraySize.z) continue;
                        
                        m_TEMP.Add(next);
                        positions.Add(next);

                        if (GetFromGrid(next.x, next.y, next.z) > m_Boundry || pancakeVisited.Contains(next)) continue;

                        q.Enqueue(next);
                        pancakeVisited.Add(next);
                    }
                }
            }
        }

}


private void OnDrawGizmos()
    {
        for (int i = 0; i < Mathf.Min(m_HighestPointsNearMinimas.Count, 2000); i++)
        {
            Gizmos.DrawSphere(new Vector3(m_HighestPointsNearMinimas[i].x - m_ArraySize.x / 2f, m_HighestPointsNearMinimas[i].y - Mathf.CeilToInt(m_PrimitiveRadius), m_HighestPointsNearMinimas[i].z - m_ArraySize.z / 2f) * m_Scale, 1.0f);
        }
        for (int i = 0; i < Mathf.Min(m_TEMP.Count, 2000); i++)
        {
            Gizmos.DrawSphere(new Vector3(m_TEMP[i].x - m_ArraySize.x / 2f, m_TEMP[i].y - Mathf.CeilToInt(m_PrimitiveRadius), m_TEMP[i].z - m_ArraySize.z / 2f) * m_Scale, 0.1f);
        }
    }
}
