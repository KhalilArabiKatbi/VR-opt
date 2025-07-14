using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct MeshDeformationSystem : ISystem
{
    private ComputeShader meshUpdater;
    private int kernelId;

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer pointPosBuffer;
    private ComputeBuffer weightMapBuffer;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        meshUpdater = Resources.Load<ComputeShader>("MeshUpdater");
        kernelId = meshUpdater.FindKernel("CSMain");
    }

    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<SpringPoint, LocalToWorld>().Build();
        var springPoints = query.ToComponentDataArray<SpringPoint>(Allocator.TempJob);

        var meshDeformer = SystemAPI.GetSingleton<MeshDeformerComponent>();
        var mesh = meshDeformer.mesh;
        var vertices = mesh.vertices;

        if (vertexBuffer == null || vertexBuffer.count != vertices.Length)
        {
            if (vertexBuffer != null) vertexBuffer.Release();
            vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        }

        if (pointPosBuffer == null || pointPosBuffer.count != springPoints.Length)
        {
            if (pointPosBuffer != null) pointPosBuffer.Release();
            pointPosBuffer = new ComputeBuffer(springPoints.Length, sizeof(float) * 3);
        }

        if (weightMapBuffer == null || weightMapBuffer.count != meshDeformer.vertexWeightBindings.Length)
        {
            if (weightMapBuffer != null) weightMapBuffer.Release();
            weightMapBuffer = new ComputeBuffer(meshDeformer.vertexWeightBindings.Length, 3 * (sizeof(int) + sizeof(float)));
        }

        vertexBuffer.SetData(vertices);
        pointPosBuffer.SetData(springPoints);
        weightMapBuffer.SetData(meshDeformer.vertexWeightBindings);

        meshUpdater.SetBuffer(kernelId, "Vertices", vertexBuffer);
        meshUpdater.SetBuffer(kernelId, "PointsPositions", pointPosBuffer);
        meshUpdater.SetBuffer(kernelId, "VertexBindings", weightMapBuffer);

        meshUpdater.SetMatrix("worldToLocal", meshDeformer.worldToLocal);
        meshUpdater.Dispatch(kernelId, Mathf.CeilToInt(vertices.Length / 64f), 1, 1);

        var request = AsyncGPUReadback.Request(vertexBuffer);

        request.WaitForCompletion();

        if (!request.hasError)
        {
            if (mesh != null)
            {
                var data = request.GetData<float3>();
                mesh.SetVertices(data);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }
        }

        springPoints.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (vertexBuffer != null) vertexBuffer.Release();
        if (pointPosBuffer != null) pointPosBuffer.Release();
        if (weightMapBuffer != null) weightMapBuffer.Release();
    }
}
