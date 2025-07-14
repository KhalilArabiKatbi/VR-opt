using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(GravitySystem))]
public partial struct SpringSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SpringConnection>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var springJob = new SpringJob
        {
            springPoints = SystemAPI.GetBuffer<SpringPoint>(false),
        };

        state.Dependency = springJob.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public partial struct SpringJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public BufferAccessor<SpringPoint> springPoints;

        public void Execute(in SpringConnection connection)
        {
            var pointA = springPoints[connection.a.Index];
            var pointB = springPoints[connection.b.Index];

            float3 direction = pointB.position - pointA.position;
            float distance = math.length(direction);
            if (distance > 0f)
            {
                direction = direction / distance;
                // Calculate spring force using Hooke's Law
                float stretch = distance - connection.restLength;
                float3 springForce = connection.springConstant * stretch * direction;

                // Apply damping to prevent sliding at higher speeds
                float3 relativeVel = pointB.velocity - pointA.velocity;
                float velocityAlongSpring = math.dot(relativeVel, direction);
                float3 dampingForce = connection.damperConstant * velocityAlongSpring * direction;

                // Combine forces
                float3 netForce = springForce + dampingForce;

                // Add forces to the map
                pointA.force += netForce;
                pointB.force -= netForce;

                springPoints[connection.a.Index] = pointA;
                springPoints[connection.b.Index] = pointB;
            }
        }
    }
}
