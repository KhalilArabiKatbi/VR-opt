using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct GravitySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Gravity>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gravity = SystemAPI.GetSingleton<Gravity>();

        var job = new GravityJob
        {
            gravity = gravity.Value,
            deltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public partial struct GravityJob : IJobEntity
    {
        public float3 gravity;
        public float deltaTime;

        public void Execute(ref SpringPoint springPoint, in Translation translation)
        {
            if (springPoint.isFixed == 0)
            {
                springPoint.velocity += gravity * deltaTime;
            }
        }
    }
}

public struct Gravity : IComponentData
{
    public float3 Value;
}
