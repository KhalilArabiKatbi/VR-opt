using Unity.Entities;
using Unity.Mathematics;

public struct SpringPoint : IComponentData
{
    public float3 position;
    public float3 velocity;
    public float3 force;
    public float3 acc;

    public float mass;
    public int isFixed;

    public float bounciness;
    public float friction;

    public float3 boundsMin;
    public float3 boundsMax;

    public int isMeshVertex;
    public int triangleIndex;

    public float3 initialPosition;
    public float3 predictedPosition;
}

public struct SpringConnection : IComponentData
{
    public Entity a;
    public Entity b;
    public float restLength;
    public float springConstant;
    public float damperConstant;
}

public struct Gravity : IComponentData
{
    public float3 Value;
}

public struct Ground : IComponentData
{
    public float Level;
    public float Bounce;
    public float Friction;
}

public struct MeshDeformerComponent : IComponentData
{
    public UnityEngine.Mesh mesh;
    public UnityEngine.Matrix4x4 worldToLocal;
    public Unity.Collections.NativeArray<VertexWeightBinding> vertexWeightBindings;
}

[System.Serializable]
public struct WeightedPoint
{
    public int index;
    public float weight;
}

[System.Serializable]
public struct VertexWeightBinding
{
    public WeightedPoint wp0;
    public WeightedPoint wp1;
    public WeightedPoint wp2;
}
