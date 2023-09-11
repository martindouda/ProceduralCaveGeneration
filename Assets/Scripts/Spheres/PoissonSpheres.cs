using System.Collections.Generic;
using System.Xml.XPath;
using Unity.VisualScripting;
using UnityEditor.Search;
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

    public List<Point> Points { get => m_Points; }

    public Vector3 Size { get => m_Size; }

    public int GridSizeX { get => m_GridSizeX; }
    public int GridSizeY { get => m_GridSizeY; }
    public int GridSizeZ { get => m_GridSizeZ; }

    public int[,,] Grid { get => m_Grid; set => m_Grid = value; }

    public enum SphereType
    {
        WHITE, BLUE, RED, GREEN, _SIZE
    }

    public class Point
    {
        public Vector3 Pos;
        public float Radius;
        public List<NearestPoint> NextList;

        public SphereType VisualSphereType;

        public Point(Vector3 pos, float radius)
        {
            this.Pos = pos;
            this.Radius = radius;
            NextList = new List<NearestPoint>();
            VisualSphereType = SphereType.WHITE;
        }

        
    }

    public class NearestPoint : IHeapItem<NearestPoint>
    {

        private int m_HeapIndex;
        private int m_PointIndex;
        private float m_SqrDist;
        private float m_Dist;

        public NearestPoint(int pointIndex, float sqrDist)
        {
            m_PointIndex = pointIndex;
            m_SqrDist = sqrDist;
        }

        public void CalculateDist()
        {
            m_Dist = Mathf.Sqrt(m_SqrDist);
        }

        public int HeapIndex { get => m_HeapIndex; set => m_HeapIndex = value; }
        public int PointIndex { get => m_PointIndex; }
        public float Dist { get => m_Dist; }

        public int CompareTo(object obj)
        {
            NearestPoint other = obj as NearestPoint;
            return other.m_SqrDist.CompareTo(m_SqrDist);
        }
    }

    public PoissonSpheres(Vector3 size, float minSphereRadius, float maxSphereRadius, float spacingLimit) 
    {
        m_Size = size;
        m_MinSphereRadius = minSphereRadius;
        m_MaxSphereRadius = maxSphereRadius;
        m_CellSize = 2 * minSphereRadius / Mathf.Sqrt(3);
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
        for (int z = 0; z < m_GridSizeZ; z++)
        {
            for (int y = 0; y < m_GridSizeY; y++)
            {
                for (int x = 0; x < m_GridSizeX; x++)
                {
                    m_Grid[x, y, z] = -1;
                }
            }
        }


        m_SpawnPoints.Add(new Point(m_Size / 2.0f, Random.Range(m_MinSphereRadius, m_MaxSphereRadius)));
        while (m_SpawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, m_SpawnPoints.Count);
            Point spawnerPoint = m_SpawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                float candidateRadius = Random.Range(m_MinSphereRadius, m_MaxSphereRadius);
                float minDistFromSpawn = spawnerPoint.Radius + candidateRadius;
                float distFromSpawn = Random.Range(minDistFromSpawn, m_SpacingLimit * minDistFromSpawn);
                Vector3 candidatePos = spawnerPoint.Pos + distFromSpawn * Random.onUnitSphere;
                Point candidatePoint = new Point(candidatePos, candidateRadius);

                if (!pointIsValid(candidatePoint)) continue;

                m_Grid[(int)(candidatePos.x / m_CellSize), (int)(candidatePos.y / m_CellSize), (int)(candidatePos.z / m_CellSize)] = m_Points.Count;
                m_Points.Add(candidatePoint);
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

    private bool pointIsValid(Point point)
    {
        if (point.Pos.x < 0.0f || point.Pos.x > m_Size.x || point.Pos.y < 0.0f || point.Pos.y > m_Size.y || point.Pos.z < 0.0f || point.Pos.z > m_Size.z) return false;

        float floatGridPosX = point.Pos.x / m_CellSize;
        float floatGridPosY = point.Pos.y / m_CellSize;
        float floatGridPosZ = point.Pos.z / m_CellSize;

        float spawnRadiusSum = (point.Radius + m_MaxSphereRadius) / m_CellSize;
        int startX = Mathf.Max((int)(floatGridPosX - spawnRadiusSum), 0);
        int endX = Mathf.Min((int)(floatGridPosX + spawnRadiusSum), m_GridSizeX - 1);
        int startY = Mathf.Max((int)(floatGridPosY - spawnRadiusSum), 0);
        int endY = Mathf.Min((int)(floatGridPosY + spawnRadiusSum), m_GridSizeY - 1);
        int startZ = Mathf.Max((int)(floatGridPosZ - spawnRadiusSum), 0);
        int endZ = Mathf.Min((int)(floatGridPosZ + spawnRadiusSum), m_GridSizeZ - 1);

        for (int z = startZ; z <= endZ; z++)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int index = m_Grid[x, y, z];
                    if (index == -1) continue;

                    float sqrDist = (point.Pos - m_Points[index].Pos).sqrMagnitude;
                    float radiusSum = point.Radius + m_Points[index].Radius;
                    if (sqrDist < radiusSum * radiusSum) return false;
                }
            }
        }

        return true;
    }   

    public int FindNearestPointsIndex(Vector3 pos, int searchDistance)
    {
        Vector3 toCenterOffset = new Vector3(m_Size.x / 2.0f, 0.0f, m_Size.z / 2.0f);

        pos.x = Mathf.Clamp(pos.x, -toCenterOffset.x, toCenterOffset.x);
        pos.y = Mathf.Clamp(pos.y, 0, m_Size.y);
        pos.z = Mathf.Clamp(pos.z, -toCenterOffset.z, toCenterOffset.z);

        int startX = Mathf.Max((int)((pos.x + toCenterOffset.x) / m_CellSize) - searchDistance, 0);
        int endX = Mathf.Min((int)((pos.x + toCenterOffset.x) / m_CellSize) + searchDistance, m_GridSizeX - 1);
        int startY = Mathf.Max((int)(pos.y / m_CellSize) - searchDistance, 0);
        int endY = Mathf.Min((int)(pos.y / m_CellSize) + searchDistance, m_GridSizeY - 1);
        int startZ = Mathf.Max((int)((pos.z + toCenterOffset.z) / m_CellSize) - searchDistance, 0);
        int endZ = Mathf.Min((int)((pos.z + toCenterOffset.z) / m_CellSize) + searchDistance, m_GridSizeZ - 1);

        int searchWidth = (int)(2.0f * searchDistance) + 1;
        Heap<NearestPoint> heap = new Heap<NearestPoint>(searchWidth * searchWidth * searchWidth);


        for (int z = startZ; z <= endZ; z++)
        {
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int index = m_Grid[x, y, z];
                    if (index == -1) continue;

                    heap.Add(new NearestPoint(index, (m_Points[index].Pos - toCenterOffset - pos).sqrMagnitude));
                }
            }
        }

        return heap.Pop().PointIndex;
    }

    public Vector3 GetGridPos(Vector3 pos) 
    {
        return new Vector3(pos.x, pos.y, pos.z) / m_CellSize;
    }
}

