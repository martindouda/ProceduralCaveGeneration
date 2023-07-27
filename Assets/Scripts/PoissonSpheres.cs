using System.Collections.Generic;
using UnityEngine;

public class PoissonSpheres
{
    private Vector3 m_Size;
    private float m_SphereRadius;
    private float m_CellSize;
    private float m_SpacingLimit;

    private int[,,] m_Grid;
    private int m_GridSizeX, m_GridSizeY, m_GridSizeZ;

    private List<Vector3> m_Points = new List<Vector3>();
    private List<Vector3> m_SpawnPoints = new List<Vector3>();

    public PoissonSpheres(Vector3 size, float sphereRadius, float spacingLimit) 
    {
        m_Size = size;
        m_SphereRadius = sphereRadius;
        m_CellSize = sphereRadius / Mathf.Sqrt(3);
        m_SpacingLimit = spacingLimit;

        m_GridSizeX = Mathf.CeilToInt(size.x / m_CellSize);
        m_GridSizeY = Mathf.CeilToInt(size.y / m_CellSize);
        m_GridSizeZ = Mathf.CeilToInt(size.z / m_CellSize);
    }

    public void GeneratePoints(int numSamplewsBeforeRejection)
    {
        m_Points = new List<Vector3>();
        m_SpawnPoints = new List<Vector3>();
        m_Grid = new int[m_GridSizeX, m_GridSizeY, m_GridSizeZ];


        m_SpawnPoints.Add(m_Size / 2.0f);
        while (m_SpawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, m_SpawnPoints.Count);
            Vector3 spawnerCenter = m_SpawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSamplewsBeforeRejection; i++)
            {
                Vector3 candidate = spawnerCenter + Random.Range(m_SphereRadius, m_SpacingLimit * m_SphereRadius) * Random.onUnitSphere;

                if (!pointIsValid(candidate)) continue;

                m_Points.Add(candidate);
                m_Grid[(int)(candidate.x / m_CellSize), (int)(candidate.y / m_CellSize), (int)(candidate.z / m_CellSize)] = m_Points.Count;
                m_SpawnPoints.Add(candidate);
                candidateAccepted = true;

                break;
            }
            if (!candidateAccepted)
            {
                m_SpawnPoints.RemoveAt(spawnIndex);
            }
        }
    }

    private bool pointIsValid(Vector3 point)
    {
        if (point.x < 0.0f || point.x > m_Size.x || point.y < 0.0f || point.y > m_Size.y || point.z < 0.0f || point.z > m_Size.z) return false;

        int gridPosX = (int)(point.x / m_CellSize);
        int gridPosY = (int)(point.y / m_CellSize);
        int gridPosZ = (int)(point.z / m_CellSize);

        int startX = Mathf.Max(gridPosX - 2, 0);
        int endX = Mathf.Min(gridPosX + 2, m_GridSizeX-1);
        int startY = Mathf.Max(gridPosY - 2, 0);
        int endY = Mathf.Min(gridPosY + 2, m_GridSizeY-1);
        int startZ = Mathf.Max(gridPosZ - 2, 0);
        int endZ = Mathf.Min(gridPosZ + 2, m_GridSizeZ-1);

        for (int z = startZ; z <= endZ; z++)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int index = m_Grid[x, y, z] - 1;
                    if (index == -1) continue;

                    float sqrDist = (point - m_Points[index]).sqrMagnitude;
                    if (sqrDist < m_SphereRadius * m_SphereRadius) return false;
                }
            }
        }

        return true;
    }

    public List<Vector3> GetPoints()
    {
        return m_Points;
    }
}
