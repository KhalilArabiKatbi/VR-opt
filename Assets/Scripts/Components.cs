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
