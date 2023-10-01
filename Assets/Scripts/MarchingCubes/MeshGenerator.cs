using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshCollider))]
public class MeshGenerator : MonoBehaviour
{
    private Mesh m_Mesh;

    private List<Vector3> m_Vertices;
    private List<int> m_Triangles;

    private Vector3Int m_Size;

    [SerializeField] private float m_Boundry = .5f;
    [SerializeField] private float m_BuildToolPower = 1.0f;
    [SerializeField] private float m_BildToolSize = 3f;

    Dictionary<Vector3, int> m_VertexDict;

    private float[] m_Grid;

    public void Generate(Vector3Int size)
    {
        m_Size = size + new Vector3Int(2, 2, 2);
        m_Mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = m_Mesh;

        CreateGrid();
        CreateShape();
        UpdateMesh();
    }

    private void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            MeshRayCastAdd();
        }
        if (Input.GetMouseButton(1))
        {
            MeshRayCastRemove();
        }
    }

    private void CreateGrid()
    {
        m_Grid = new float[(m_Size.x + 1) * (m_Size.z + 1) * (m_Size.y + 1)];
        for (int y = 1; y < m_Size.y; y++)
        {
            for (int z = 1; z < m_Size.z; z++)
            {
                for (int x = 1; x < m_Size.x; x++)
                {
                    m_Grid[x + z * (m_Size.x + 1) + y * (m_Size.x + 1) * (m_Size.z + 1)] = 1.0f;
                }
            }
        }
    }

    public void CreateShape()
    {
        m_Vertices = new List<Vector3>();
        m_Triangles = new List<int>();
        m_VertexDict = new Dictionary<Vector3, int>();

        for (int y = 0; y < m_Size.y; y++)
        {
            for (int z = 0; z < m_Size.z; z++)
            {
                for (int x = 0; x < m_Size.x; x++)
                {
                    int cubeIndex = 0;

                    float[] cubeValues = new float[]
                    {
                        GetFromGrid(x, y, z + 1),
                        GetFromGrid(x + 1, y, z + 1),
                        GetFromGrid(x + 1, y, z),
                        GetFromGrid(x, y, z),
                        GetFromGrid(x, y + 1, z + 1),
                        GetFromGrid(x + 1, y + 1, z + 1),
                        GetFromGrid(x + 1, y + 1, z),
                        GetFromGrid(x, y + 1, z),
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

                        Vector3 pos = new Vector3(x - m_Size.x / 2f, y, z - m_Size.z / 2f);
                        
                        AddVertex(pos, MarchingCubesTables.cubeCorners[a0], cubeValues[a0], MarchingCubesTables.cubeCorners[a1], cubeValues[a1]);
                        AddVertex(pos, MarchingCubesTables.cubeCorners[c0], cubeValues[c0], MarchingCubesTables.cubeCorners[c1], cubeValues[c1]);
                        AddVertex(pos, MarchingCubesTables.cubeCorners[b0], cubeValues[b0], MarchingCubesTables.cubeCorners[b1], cubeValues[b1]);
                    }
                }
            }
        }
    }

    private float GetFromGrid(int x, int y, int z)
    {
        return m_Grid[x + z * (m_Size.x + 1) + y * (m_Size.x + 1) * (m_Size.z + 1)];
    }

    private void AddVertex(Vector3 pos, Vector3 vert0, float val0, Vector3 vert1, float val1)
    {
        Vector3 vertexToAdd = pos + (vert0 + (m_Boundry - val0) * (vert1 - vert0) / (val1 - val0));
        if (m_VertexDict.ContainsKey(vertexToAdd))
        {
            m_Triangles.Add(m_VertexDict[vertexToAdd]);
            return;
        }
        m_VertexDict[vertexToAdd] = m_Vertices.Count;
        m_Triangles.Add(m_Vertices.Count);
        m_Vertices.Add(pos + (vert0 + (m_Boundry - val0) * (vert1 - vert0) / (val1 - val0)));
    }

    public void UpdateMesh()
    {
        m_Mesh.Clear();
        m_Mesh.SetVertices(m_Vertices);
        m_Mesh.SetTriangles(m_Triangles, 0);
        m_Mesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = m_Mesh;
    }

    private void MeshRayCastAdd()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            AddToTerain(hit.point);
            CreateShape();
            UpdateMesh();
        }
    }

    private void MeshRayCastRemove()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            RemoveFromTerrain(hit.point);
            CreateShape();
            UpdateMesh();
        }
    }

    public void AddToTerain(Vector3 worldPos)
    {
        for (float y = -m_BildToolSize; y <= m_BildToolSize; y++)
        {
            for (float z = -m_BildToolSize; z <= m_BildToolSize; z++)
            {
                for (float x = -m_BildToolSize; x <= m_BildToolSize; x++)
                {
                    Vector3 pos = new Vector3(worldPos.x + x, worldPos.y + y, worldPos.z + z);
                    float distance = (worldPos - pos).magnitude / m_BildToolSize;
                    if (distance > 1.0f) continue;
                    
                    Vector3Int gridPos = new Vector3Int((int)(pos.x + m_Size.x / 2f + .5f), (int)(pos.y + .5f), (int)(pos.z + m_Size.z / 2f + .5f));

                    if (gridPos.y < 0 || m_Size.x < gridPos.y || gridPos.z < 0 || m_Size.z < gridPos.z || gridPos.x < 0 || m_Size.x < gridPos.x) continue;

                    m_Grid[gridPos.x + gridPos.z * (m_Size.x + 1) + gridPos.y * (m_Size.x + 1) * (m_Size.z + 1)] += (1 - distance) * m_BuildToolPower;
                    Mathf.Clamp(m_Grid[gridPos.x + gridPos.z * (m_Size.x + 1) + gridPos.y * (m_Size.x + 1) * (m_Size.z + 1)], 0.0f, 1.0f);
                }
            }
        }
    } 
    
    public void RemoveFromTerrain(Vector3 worldPos)
    {
        for (float y = -m_BildToolSize; y <= m_BildToolSize; y++)
        {
            for (float z = -m_BildToolSize; z <= m_BildToolSize; z++)
            {
                for (float x = -m_BildToolSize; x <= m_BildToolSize; x++)
                {
                    Vector3 pos = new Vector3(worldPos.x + x, worldPos.y + y, worldPos.z + z);
                    float distance = (worldPos - pos).magnitude / m_BildToolSize;
                    if (distance > 1.0f) continue;
                    
                    Vector3Int gridPos = new Vector3Int((int)(pos.x + m_Size.x / 2f + .5f), (int)(pos.y + .5f), (int)(pos.z + m_Size.z / 2f + .5f));

                    if (gridPos.y < 0 || m_Size.x < gridPos.y || gridPos.z < 0 || m_Size.z < gridPos.z || gridPos.x < 0 || m_Size.x < gridPos.x) continue;

                    m_Grid[gridPos.x + gridPos.z * (m_Size.x + 1) + gridPos.y * (m_Size.x + 1) * (m_Size.z + 1)] -= (1 - distance) * m_BuildToolPower;
                    Mathf.Clamp(m_Grid[gridPos.x + gridPos.z * (m_Size.x + 1) + gridPos.y * (m_Size.x + 1) * (m_Size.z + 1)], 0.0f, 1.0f);
                }
            }
        }
    }
}
