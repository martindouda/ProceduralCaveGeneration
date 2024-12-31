/*
 * Project: Procedural Generation of Cave Systems
 * File: MeshGenerator.cs
 * Author: Martin Douda
 * Date: 2.5.2024
 * Description: This is responsible for every visible part of the cave. It procedurally generates the cave mesh, individual speleothems
 * and the cave lakes. During the cave mesh generation process it manages Primitive Sweeping algorithm over the volumetric array and the
 * construction of the mesh using the Marching Cubes algorithm. Speleothems are constructed individually based on certain conditions
 * and the cave lakes are created using the Marching Sqaures algorithm.
 */

using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


// This class is responsible for generating every mesh in the scene. This includes cave mesh, speleothems mesh and cave lakes mesh.
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
    private float m_CeilingAngleLimit;

    Dictionary<Vector3, int> m_VertexDict;

    private float[] m_Grid;

    // Speleothems
    [SerializeField] private Transform m_SpeleoNormalPrefab;
    [SerializeField] private Transform m_SpeleoStrawPrefab;
    [SerializeField] private Transform m_TopSpeleoParent;
    [SerializeField] private Transform m_BotSpeleoParent;

    [SerializeField] private Transform m_WaterMeshObject;

    // Compute
    [SerializeField] private ComputeManager m_ComputeManager;

    public void DeleteCaveMesh()
    {
        GetComponent<MeshFilter>().mesh = null;
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = m_Mesh;
        meshCollider.enabled = false;
        m_Mesh = null;
    }

    // Generates the cave mesh.
    public void GenerateCaveMesh(Vector3 sizeFloat, float scale, float boundry, float editPower, float primitiveRadius, float discRadius, float ceilingAngleLimit)
    {
        Vector3Int size = new Vector3Int((int)(sizeFloat.x / scale), (int)(sizeFloat.y / scale), (int)(sizeFloat.z / scale));
        m_Size = size;
        m_ArraySize = size + Vector3Int.one * Mathf.CeilToInt(primitiveRadius / scale + discRadius / scale) * 2;
        m_Scale = scale;
        m_Boundry = boundry;
        m_EditPower = editPower;
        m_PrimitiveRadius = primitiveRadius / scale;
        m_DiscRadius = discRadius / scale;
        m_CeilingAngleLimit = ceilingAngleLimit;
        m_Mesh = new Mesh();
        m_Mesh.name = "CaveMesh";
        GetComponent<MeshFilter>().mesh = m_Mesh;

        CreateGrid();
        UpdateMesh();
        m_Mesh.Clear();
        m_Mesh.SetVertices(m_Vertices);
        m_Mesh.SetNormals(m_Normals);
        m_Mesh.SetTriangles(m_Indices, 0);

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = m_Mesh;
        meshCollider.enabled = false;
    }

    // Initializes the 3D array for the Marching Cubes algorithm.
    private void CreateGrid()
    {
        m_Grid = new float[(m_ArraySize.x + 1) * (m_ArraySize.z + 1) * (m_ArraySize.y + 1)];
        for (int y = 0; y <= m_ArraySize.y; y++)
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

    // Performes Primitive Sweeping along the paths.
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

    [SerializeField] private bool m_UseComputeImpl = true;
    // Builds the mesh using the Marching Cubes algorithm.
    public void PerformMarchingCubes()
    {
        ClearTopSpeleo();
        ClearBotSpeleo();

        if (m_UseComputeImpl)
        {
            m_ComputeManager.CreateMarchingCubesMesh(m_Mesh, m_ArraySize, m_Grid, m_Boundry, m_Scale, Mathf.CeilToInt(m_PrimitiveRadius));
            return;
        }

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
                    float[] cubeValues = new float[]
                    {
                        GetFromGrid(x,     y,     z + 1),
                        GetFromGrid(x + 1, y,     z + 1),
                        GetFromGrid(x + 1, y,     z    ),
                        GetFromGrid(x,     y,     z    ),
                        GetFromGrid(x,     y + 1, z + 1),
                        GetFromGrid(x + 1, y + 1, z + 1),
                        GetFromGrid(x + 1, y + 1, z    ),
                        GetFromGrid(x,     y + 1, z    ),
                    };

                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (cubeValues[i] >= m_Boundry) cubeIndex |= 1 << i;
                    }

                    int startIndex = cubeIndex * MarchingCubesTables.triTableEntryLength;
                    for (int i = startIndex; MarchingCubesTables.triTable[i] != -1; i += 3)
                    {
                        int edgeIndex = MarchingCubesTables.triTable[i] * 2;
                        int a0 = MarchingCubesTables.edgeConnections[edgeIndex];
                        int a1 = MarchingCubesTables.edgeConnections[edgeIndex + 1];

                        edgeIndex = MarchingCubesTables.triTable[i + 1] * 2;
                        int b0 = MarchingCubesTables.edgeConnections[edgeIndex];
                        int b1 = MarchingCubesTables.edgeConnections[edgeIndex + 1];

                        edgeIndex = MarchingCubesTables.triTable[i + 2] * 2;
                        int c0 = MarchingCubesTables.edgeConnections[edgeIndex];
                        int c1 = MarchingCubesTables.edgeConnections[edgeIndex + 1];

                        Vector3 pos = new Vector3(x - m_ArraySize.x / 2f, y - Mathf.CeilToInt(m_PrimitiveRadius), z - m_ArraySize.z / 2f) * m_Scale;


                        Vector3 vertexPos1 = GetMarchingCubesVertex(pos, MarchingCubesTables.cubeCorners[a0], cubeValues[a0], MarchingCubesTables.cubeCorners[a1], cubeValues[a1]);
                        Vector3 vertexPos2 = GetMarchingCubesVertex(pos, MarchingCubesTables.cubeCorners[c0], cubeValues[c0], MarchingCubesTables.cubeCorners[c1], cubeValues[c1]);
                        Vector3 vertexPos3 = GetMarchingCubesVertex(pos, MarchingCubesTables.cubeCorners[b0], cubeValues[b0], MarchingCubesTables.cubeCorners[b1], cubeValues[b1]);

                        Vector3 normal = Vector3.Cross(vertexPos2 - vertexPos1, vertexPos3 - vertexPos1).normalized;

                        if (Vector3.Dot(normal, Vector3.down) > Mathf.Cos(Mathf.Deg2Rad * m_CeilingAngleLimit))    
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
        m_Mesh.Clear();
        m_Mesh.SetVertices(m_Vertices);
        m_Mesh.SetNormals(m_Normals);
        m_Mesh.SetTriangles(m_Indices, 0);
    }

    // Returns the adequate field from the 3D array.
    private  float GetFromGrid(int x, int y, int z)
    {
        return m_Grid[x + (m_ArraySize.x + 1) * (z + y * (m_ArraySize.z + 1))];
    }

    // Returns rounded real world position of a marching cubes vertex.
    private Vector3 GetMarchingCubesVertex(Vector3 pos, Vector3 vert0, float val0, Vector3 vert1, float val1)
    {
        Vector3 ret = pos + (vert0 + (m_Boundry - val0) * (vert1 - vert0) / (val1 - val0)) * m_Scale;
        return new Vector3(((int)(ret.x * 100.0f + 0.5f)) / 100.0f, ((int)(ret.y * 100.0f + 0.5f)) / 100.0f, ((int)(ret.z * 100.0f + 0.5f)) / 100.0f);
    }

    // Adds a new vertex to VBO or finds it in a dictionary if it already exists.
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

    // Updates the mesh comopnent.
    public void UpdateMesh()
    {
        /*m_Mesh.Clear();
        m_Mesh.SetVertices(m_Vertices);
        m_Mesh.SetNormals(m_Normals);
        m_Mesh.SetTriangles(m_Indices, 0);*/
    }
    
    // Subtracts a value from each point in the 3D array contained inside a sphere. The subtracted value is based on the distance of the certain point from the center of the sphere.
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

    public void DeleteWaterMesh()
    {
        m_WaterMeshObject.GetComponent<MeshFilter>().mesh = null;
        m_WaterVertices.Clear();
        m_WaterGridCoords.Clear();
        m_Minimas.Clear();
    }

    // Destroys all previously created stalactites.
    public void ClearTopSpeleo()
    {
        while (m_TopSpeleoParent.childCount > 0)
        {
            DestroyImmediate(m_TopSpeleoParent.GetChild(0).gameObject);
        }
    }

    // Destroys all previously created stalagmites.
    public void ClearBotSpeleo()
    {
        while (m_BotSpeleoParent.childCount > 0)
        {
            DestroyImmediate(m_BotSpeleoParent.GetChild(0).gameObject);
        }
    }

    [SerializeField] private float m_Radius = 0.05f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_RadiusFluctuation = 0.2f;

    [SerializeField] private float m_WidthExponent = 2.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_WidthExponentFluctutation = 1.0f;


    // Generates speleothems on predefined positions.
    public void GenerateSpeleothems(float spawnProbability, float maxHeight, float strawProbability, float stalagmiteProability, float stalagmiteHeightCoef, float stalagmiteRadiusCoef)
    {
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.enabled = true;
        ClearTopSpeleo();
        ClearBotSpeleo();

        Vector3 upCorrection = Vector3.up * Mathf.Sin(m_CeilingAngleLimit) * m_Radius * (1 + m_RadiusFluctuation);
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

                height = height * stalagmiteHeightCoef;
                radius = radius * stalagmiteRadiusCoef;
                widthExponent = widthExponent + 2.0f;
                GenerateBotSpeleo(botSpeleo.pos - upCorrection, height, radius, widthExponent);
            }
        }
    }

    // Specifies the speleothem type.
    enum SpeleoType
    {
        Normal, Straw
    }

    // Contains data about a stalagmite.
    struct BotSpeleo
    {
        public Vector3 pos;
        public float height;

        public bool generate;
    }

    [SerializeField] private int m_NumPointsHorizontal = 20;
    [SerializeField] private int m_NumPointsVertical = 20;

    // Generates a single stalactite.
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
                if (Vector3.Dot(hit.normal, Vector3.up) < Mathf.Cos(Mathf.Deg2Rad * m_CeilingAngleLimit)) return ret;

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


    // Generates a single stalagmite.
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
    private List<Vector3Int> m_WaterGridCoords = new List<Vector3Int>();

    // Each vertex inside the 3D array has an assigned group index, which points to a group instance. Group specifies the local water height based on its neighbouring groups.
    struct Group
    {
        public int waterHeight;
        
        public Group(int min, int max, int maxWaterHeightFound, float groundWaterLevelHeight)
        {
            if (maxWaterHeightFound == -1 || maxWaterHeightFound <= min)
            {
                this.waterHeight = Mathf.Clamp(Random.Range(min, max + 1), (int)groundWaterLevelHeight, max + 1);
                return;
            }
            if (maxWaterHeightFound > max)
            {
                this.waterHeight = maxWaterHeightFound;
                return;
            }
            this.waterHeight = Mathf.Clamp(Random.Range(maxWaterHeightFound, max + 1), (int)groundWaterLevelHeight, max + 1);
        }
    }



    private List<Vector3> m_WaterVertices = new List<Vector3>();
    private List<int> m_WaterIndices = new List<int>();

    Dictionary<Vector3, int> m_WaterVertexDict = new Dictionary<Vector3, int>();

    // Searches the 3D array for local minima.
    public void FindLowGrounds(float groundWaterLevelHeight)
    {
        m_Minimas = new List<Vector3Int>();
        m_WaterGridCoords = new List<Vector3Int>();

        m_WaterVertices = new List<Vector3>();
        m_WaterIndices = new List<int>();
        m_WaterVertexDict = new Dictionary<Vector3, int>();
    
        int sizeX = m_ArraySize.x + 1;
        int sizeY = m_ArraySize.y + 1;
        int sizeZ = m_ArraySize.z + 1;
        int size = sizeX * sizeY * sizeZ;
        int[] groupIds = new int[size];

        int GetGroupId(int x, int y, int z) { return groupIds[x + z * sizeX + y * sizeX * sizeZ]; }
        void SetGroupId(int x, int y, int z, int groupId) { groupIds[x + z * sizeX + y * sizeX * sizeZ] = groupId; }
        for (int i = 0; i < size; i++) groupIds[i] = -1;

        int[,] neighboursOnLevel = new int[,] { { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, -1 }, { 0, 1 }, { 1, -1 }, { 1, 0 }, { 1, 1 } };

        List<Group> groups = new List<Group>();
        for (int y = 0; y < m_ArraySize.y; y++)
        {
            for (int z = 0; z <= m_ArraySize.z; z++)
            {
                for (int x = 0; x <= m_ArraySize.x; x++)
                {
                    if (GetFromGrid(x, y, z) <= m_Boundry && GetGroupId(x, y, z) == -1)
                    {
                        ExploreGroup(new Vector3Int(x, y, z));
                        m_Minimas.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        // Launches a bradth-first search through the 3D array and marks the visited vertices with a group index.
        void ExploreGroup(Vector3Int start)
        {
            Queue<Vector3Int> q = new Queue<Vector3Int>();
            Queue<Vector3Int> aboveQ = new Queue<Vector3Int>();
            int groupId = groups.Count;
            int maxHeight = start.y;
            int maxWaterHeightFound = -1;
            q.Enqueue(start);
            SetGroupId(start.x, start.y, start.z, groupId);
            
            while (q.Count > 0)
            {
                while (q.Count > 0)
                {
                    Vector3Int v = q.Dequeue();

                    Vector3Int above = new Vector3Int(v.x, v.y + 1, v.z);
                    if (v.y < m_ArraySize.y && GetFromGrid(above.x, above.y, above.z) <= m_Boundry)
                    {
                        int id = GetGroupId(above.x, above.y, above.z);
                        if (id == -1)
                        {
                            if (above.y > maxHeight) maxHeight = above.y;
                            aboveQ.Enqueue(above);
                            SetGroupId(above.x, above.y, above.z, groupId);
                        } 
                        else
                        {
                            int waterHeight = groups[id].waterHeight;
                            if (maxWaterHeightFound < waterHeight) maxWaterHeightFound = waterHeight;
                        }
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int next = new Vector3Int(v.x + neighboursOnLevel[i, 0], v.y, v.z + neighboursOnLevel[i, 1]);
                        if (next.x < 0 || next.x >= m_ArraySize.x || next.z < 0 || next.z >= m_ArraySize.z || GetFromGrid(next.x, next.y, next.z) > m_Boundry || GetGroupId(next.x, next.y, next.z) != -1) continue;

                        q.Enqueue(next);
                        SetGroupId(next.x, next.y, next.z, groupId);
                    }
                }
                q = aboveQ;
                aboveQ = new Queue<Vector3Int>();
            }

            groups.Add(new Group(start.y, maxHeight, maxWaterHeightFound, groundWaterLevelHeight));
        }

        List<int> slices = new List<int>();
        HashSet<Vector3Int> waterSurfacePointsSet = new HashSet<Vector3Int>();
        for (int y = 0; y < m_ArraySize.y; y++)
        {
            bool containsPancake = false;
            for (int z = 0; z <= m_ArraySize.z; z++)
            {
                for (int x = 0; x <= m_ArraySize.x; x++)
                {
                    int groupId = GetGroupId(x, y, z);
                    if (groupId == -1 || y != groups[groupId].waterHeight) continue;

                    Vector3Int v = new Vector3Int(x, y, z);
                    if (!waterSurfacePointsSet.Contains(v))
                    {
                        containsPancake = true;
                        m_WaterGridCoords.Add(v);
                        waterSurfacePointsSet.Add(v);
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int next = new Vector3Int(x + neighboursOnLevel[i, 0], y, z + neighboursOnLevel[i, 1]);
                        if (!waterSurfacePointsSet.Contains(next))
                        {
                            m_WaterGridCoords.Add(next);
                            waterSurfacePointsSet.Add(v);
                            //SetGroupId(next.x, next.y, next.z, groupId);
                        }
                    }
                }
            }
            if (!containsPancake) continue;

            slices.Add(y);
        }


        // Builds the cave lake mesh employing the Marching Squares algorithm.
        foreach (int y in slices)
        {
            for (int z = 0; z < m_ArraySize.z; z++)
            {
                for (int x = 0; x < m_ArraySize.x; x++)
                {
                    float GetSquareValue(int x, int y, int z)
                    {
                        int groupId = GetGroupId(x, y, z);
                        return (groupId == -1 || y != groups[groupId].waterHeight) ? 0.5f : 0.0f;
                    }
                    float[] squareValues = new float[]
                    {
                        GetSquareValue(x + 1, y, z    ),
                        GetSquareValue(x + 1, y, z + 1),
                        GetSquareValue(x    , y, z + 1),
                        GetSquareValue(x    , y, z    ),
                    };

                    int squareIndex = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (squareValues[i] < m_Boundry) squareIndex |= 1 << i;
                    }
                    //if (squareValues[0] != 0 || squareValues[1] != 0 || squareValues[2] != 0 || squareValues[3] != 0) Debug.Log(string.Format("{0} {1} {2} {3} : {4}", squareValues[0], squareValues[1], squareValues[2], squareValues[3], squareIndex));

                    int[] edges = MarchingSquaresTables.triTable[squareIndex];
                    for (int i = 0; edges[i] != -1; i += 3)
                    {
                        int a0 = MarchingSquaresTables.edgeConnections[edges[i]][0];
                        int a1 = MarchingSquaresTables.edgeConnections[edges[i]][1];

                        int b0 = MarchingSquaresTables.edgeConnections[edges[i + 1]][0];
                        int b1 = MarchingSquaresTables.edgeConnections[edges[i + 1]][1];

                        int c0 = MarchingSquaresTables.edgeConnections[edges[i + 2]][0];
                        int c1 = MarchingSquaresTables.edgeConnections[edges[i + 2]][1];

                        Vector3 pos = new Vector3(x - m_ArraySize.x / 2f, y - Mathf.CeilToInt(m_PrimitiveRadius), z - m_ArraySize.z / 2f) * m_Scale;

                        Vector3 vertexPos1 = GetMarchingSquaresVertex(pos, MarchingSquaresTables.squareCorners[a0], squareValues[a0], MarchingSquaresTables.squareCorners[a1], squareValues[a1]);
                        Vector3 vertexPos2 = GetMarchingSquaresVertex(pos, MarchingSquaresTables.squareCorners[c0], squareValues[c0], MarchingSquaresTables.squareCorners[c1], squareValues[c1]);
                        Vector3 vertexPos3 = GetMarchingSquaresVertex(pos, MarchingSquaresTables.squareCorners[b0], squareValues[b0], MarchingSquaresTables.squareCorners[b1], squareValues[b1]);

                        AddSquaresVertex(vertexPos1);
                        AddSquaresVertex(vertexPos2);
                        AddSquaresVertex(vertexPos3);
                    }
                }
            }
        }
        MeshFilter meshFilter = m_WaterMeshObject.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.SetVertices(m_WaterVertices);
        mesh.SetTriangles(m_WaterIndices, 0);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    // Returns rounded real world position of a marching squares vertex.
    private Vector3 GetMarchingSquaresVertex(Vector3 pos, Vector3 vert0, float val0, Vector3 vert1, float val1)
    {
        Vector3 ret;
        if (val0 == val1)
        {
            ret = pos + vert0 * m_Scale;
        }
        else
        {
            ret = pos + (vert0 + (m_Boundry - val0) * (vert1 - vert0) / (val1 - val0)) * m_Scale;
        }
        return new Vector3(((int)(ret.x * 100.0f + 0.5f)) / 100.0f, ((int)(ret.y * 100.0f + 0.5f)) / 100.0f, ((int)(ret.z * 100.0f + 0.5f)) / 100.0f);
    }

    // Adds a new square vertex to VBO or finds it in a dictionary if it already exists.
    private void AddSquaresVertex(Vector3 pos)
    {
        if (m_WaterVertexDict.ContainsKey(pos))
        {
            m_WaterIndices.Add(m_WaterVertexDict[pos]);
            return;
        }
        m_WaterVertexDict[pos] = m_WaterVertices.Count;
        m_WaterIndices.Add(m_WaterVertices.Count);
        m_WaterVertices.Add(pos);
    }

    // Enables or disables the rendering of a water mesh.
    public void RenderWater(bool renderMesh)
    {
        m_WaterMeshObject.gameObject.SetActive(renderMesh);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;    
        for (int i = 0; i < Mathf.Min(m_Minimas.Count, 2000); i++)
        {
            Gizmos.DrawSphere(new Vector3(m_Minimas[i].x - m_ArraySize.x / 2f, m_Minimas[i].y - Mathf.CeilToInt(m_PrimitiveRadius), m_Minimas[i].z - m_ArraySize.z / 2f) * m_Scale, 0.5f);
        }
        Gizmos.color = Color.white;
        for (int i = 0; i < Mathf.Min(m_WaterGridCoords.Count, 10000); i++)
        {
            Gizmos.DrawSphere(new Vector3(m_WaterGridCoords[i].x - m_ArraySize.x / 2f, m_WaterGridCoords[i].y - Mathf.CeilToInt(m_PrimitiveRadius), m_WaterGridCoords[i].z - m_ArraySize.z / 2f) * m_Scale, 0.1f);
        }
    }
}
