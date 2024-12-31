using System.Collections.Generic;
using UnityEngine;


public class ComputeManager : MonoBehaviour
{
    [SerializeField] private ComputeShader m_MarchingCubesCompute;
    private ComputeBuffer m_GridBuffer;
    private ComputeBuffer m_VerticesBuffer;
    private ComputeBuffer m_IndexCounterBuffer;

    private void DispatchWrapper(ComputeShader computeShader, int kernelId, Vector3Int workSize)
    {
        uint groupSizeX, groupSizeY, groupSizeZ;
        computeShader.GetKernelThreadGroupSizes(kernelId, out groupSizeX, out groupSizeY, out groupSizeZ);
        Vector3Int numGroups = new Vector3Int(
            (int)((workSize.x + groupSizeX - 1) / groupSizeX),
            (int)((workSize.y + groupSizeY - 1) / groupSizeY),
            (int)((workSize.z + groupSizeZ - 1) / groupSizeZ));
        computeShader.Dispatch(kernelId, numGroups.x, numGroups.y, numGroups.z);
    }

    public void CreateMarchingCubesMesh(Mesh mesh, Vector3Int workSize, float[] grid, float boundry, float scale, float primitiveRadius) // workSize is dimesnsions of grid - 1
    {
        // Get kernel ids
        int marchingCubesKernelId = m_MarchingCubesCompute.FindKernel("MarchingCubesKernel");

        // Init grid buffer
        m_GridBuffer = new ComputeBuffer(grid.Length, sizeof(float));
        m_GridBuffer.SetData(grid);
        m_MarchingCubesCompute.SetBuffer(marchingCubesKernelId, "u_Grid", m_GridBuffer);

        // Init vertices buffer
        m_VerticesBuffer = new ComputeBuffer(15 * grid.Length, 3 * sizeof(float));
        m_MarchingCubesCompute.SetBuffer(marchingCubesKernelId, "u_Vertices", m_VerticesBuffer);

        // Init index counter buffer
        m_IndexCounterBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);
        int[] zero = { 0 };
        m_IndexCounterBuffer.SetData(zero);
        m_IndexCounterBuffer.SetCounterValue(0);
        m_MarchingCubesCompute.SetBuffer(marchingCubesKernelId, "u_IndexCounter", m_IndexCounterBuffer);

        // Set uniforms
        m_MarchingCubesCompute.SetInts("u_WorkSize", workSize.x, workSize.y, workSize.z);
        m_MarchingCubesCompute.SetFloat("u_Boundry", boundry);
        m_MarchingCubesCompute.SetFloat("u_Scale", scale);
        m_MarchingCubesCompute.SetFloat("u_PrimitiveRadius", primitiveRadius);

        // Dispatch the kernels
        DispatchWrapper(m_MarchingCubesCompute, marchingCubesKernelId, workSize);

        // Get the data
        int[] counterValue = new int[1];
        m_IndexCounterBuffer.GetData(counterValue);
        int numVertices = counterValue[0];

        Vector3[] vertices = new Vector3[numVertices];
        m_VerticesBuffer.GetData(vertices, 0, 0, numVertices);

        // Filter out the duplicate vertices and create an index buffer
        List<Vector3> compressedVertices = new List<Vector3>();
        List<int> compressedIndices = new List<int>();
        Dictionary<Vector3, int> vertexDict = new Dictionary<Vector3, int>();
        foreach (Vector3 vertex in vertices)
        {
            if (vertexDict.ContainsKey(vertex))
            {
                compressedIndices.Add(vertexDict[vertex]);
                //m_Normals[vertexDict[vertex]] += normal;
                continue;
            }
            vertexDict[vertex] = compressedVertices.Count;
            compressedIndices.Add(compressedVertices.Count);
            compressedVertices.Add(vertex);
        }

        // Update the mesh
        mesh.Clear();
        mesh.SetVertices(compressedVertices);
        mesh.SetTriangles(compressedIndices, 0);
        mesh.RecalculateNormals();

        m_GridBuffer.Dispose();
        m_VerticesBuffer.Dispose();
        m_IndexCounterBuffer.Dispose();
    }
}
