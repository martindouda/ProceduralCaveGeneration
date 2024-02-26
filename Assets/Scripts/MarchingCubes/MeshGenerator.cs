using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;


[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    private Mesh m_Mesh;

    private List<Vector3> m_Vertices;
    private List<Vector3> m_Normals;
    private List<int> m_Triangles;

    private Vector3 m_Size;
    private Vector3Int m_ArraySize;

    private float m_Boundry = 0.5f;
    private float m_Scale = 1.0f;
    private float m_EditPower = 1.0f;
    private float m_PrimitiveRadius;
    private float m_DiscRadius;


    Dictionary<Vector3, int> m_VertexDict;

    private float[] m_Grid;

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
        GetComponent<MeshFilter>().mesh = m_Mesh;

        CreateGrid();
        CreateShape();
        UpdateMesh();
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
        m_Vertices = new List<Vector3>();
        m_Normals = new List<Vector3>();
        m_Triangles = new List<int>();
        m_VertexDict = new Dictionary<Vector3, int>();

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
            m_Triangles.Add(m_VertexDict[pos]);
            m_Normals[m_VertexDict[pos]] += normal;
            return;
        }
        m_VertexDict[pos] = m_Vertices.Count;
        m_Triangles.Add(m_Vertices.Count);
        m_Vertices.Add(pos);
        m_Normals.Add(normal);
    }

    public void UpdateMesh()
    {
        m_Mesh.Clear();
        m_Mesh.SetVertices(m_Vertices);
        m_Mesh.SetNormals(m_Normals);
        m_Mesh.SetTriangles(m_Triangles, 0);
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
}
