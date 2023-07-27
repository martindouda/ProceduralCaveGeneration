using System.Collections.Generic;
using UnityEngine;

public class PoissonSpheres
{
    private Vector3 m_Size;
    private float m_MinSphereRadius;
    private float m_MaxSphereRadius;
    private float m_CellSize;
    private float m_SpacingLimit;

    private int[,,] m_Grid;
    private int m_GridSizeX, m_GridSizeY, m_GridSizeZ;

    private List<Point> m_Points = new List<Point>();
    private List<Point> m_SpawnPoints = new List<Point>();

    public struct Point
    {
        public Vector3 pos;
        public float r;

        public Point(Vector3 pos, float r)
        {
            this.pos = pos;
            this.r = r;
        }
    }

    public PoissonSpheres(Vector3 size, float sphereRadius, float spacingLimit) 
    {
        m_Size = size;
        m_MinSphereRadius = sphereRadius;
        m_MaxSphereRadius = 2 * sphereRadius;
        m_CellSize = 2 * sphereRadius / Mathf.Sqrt(3);
        m_SpacingLimit = spacingLimit;

        m_GridSizeX = Mathf.CeilToInt(size.x / m_CellSize);
        m_GridSizeY = Mathf.CeilToInt(size.y / m_CellSize);
        m_GridSizeZ = Mathf.CeilToInt(size.z / m_CellSize);
    }

    public void GeneratePoints(int numSamplesBeforeRejection)
    {
        m_Points = new List<Point>();
        m_SpawnPoints = new List<Point>();
        m_Grid = new int[m_GridSizeX, m_GridSizeY, m_GridSizeZ];

        m_SpawnPoints.Add(new Point(m_Size / 2.0f, Random.Range(m_MinSphereRadius, m_MaxSphereRadius)));
        while (m_SpawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, m_SpawnPoints.Count);
            Point spawnerPoint = m_SpawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                float candidateRadius = Random.Range(m_MinSphereRadius, m_MaxSphereRadius);
                float minDistFromSpawn = spawnerPoint.r + candidateRadius;
                float distFromSpawn = Random.Range(minDistFromSpawn, m_SpacingLimit * minDistFromSpawn);
                Vector3 candidatePos = spawnerPoint.pos + distFromSpawn * Random.onUnitSphere;
                Point candidatePoint = new Point(candidatePos, candidateRadius);

                if (!pointIsValid(candidatePoint, spawnerPoint)) continue;

                m_Points.Add(candidatePoint);
                m_Grid[(int)(candidatePos.x / m_CellSize), (int)(candidatePos.y / m_CellSize), (int)(candidatePos.z / m_CellSize)] = m_Points.Count;
                m_SpawnPoints.Add(candidatePoint);
                candidateAccepted = true;
                break;
            }
            if (!candidateAccepted)
            {
                m_SpawnPoints.RemoveAt(spawnIndex);
            }
        }
    }

    private bool pointIsValid(Point point, Point spawnPoint)
    {
        if (point.pos.x < 0.0f || point.pos.x > m_Size.x || point.pos.y < 0.0f || point.pos.y > m_Size.y || point.pos.z < 0.0f || point.pos.z > m_Size.z) return false;

        float floatGridPosX = point.pos.x / m_CellSize;
        float floatGridPosY = point.pos.y / m_CellSize;
        float floatGridPosZ = point.pos.z / m_CellSize;

        float spawnRadiusSum = point.r + spawnPoint.r;
        int startX = Mathf.Max((int)(floatGridPosX - spawnRadiusSum), 0);
        int endX = Mathf.Min((int)(floatGridPosX + spawnRadiusSum), m_GridSizeX-1);
        int startY = Mathf.Max((int)(floatGridPosY - spawnRadiusSum), 0);
        int endY = Mathf.Min((int)(floatGridPosY + spawnRadiusSum), m_GridSizeY-1);
        int startZ = Mathf.Max((int)(floatGridPosZ - spawnRadiusSum), 0);
        int endZ = Mathf.Min((int)(floatGridPosZ + spawnRadiusSum), m_GridSizeZ-1);

        for (int z = startZ; z <= endZ; z++)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int index = m_Grid[x, y, z] - 1;
                    if (index == -1) continue;

                    float sqrDist = (point.pos - m_Points[index].pos).sqrMagnitude;
                    float radiusSum = point.r + m_Points[index].r;
                    if (sqrDist < radiusSum * radiusSum) return false;
                }
            }
        }

        return true;
    }

    public List<Point> GetPoints()
    {
        return m_Points;
    }
}
