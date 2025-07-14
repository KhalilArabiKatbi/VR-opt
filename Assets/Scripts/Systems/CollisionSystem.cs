using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SpringSystem))]
public partial struct CollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Ground>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ground = SystemAPI.GetSingleton<Ground>();

        var groundJob = new GroundCollisionJob
        {
            groundLevel = ground.Level,
            groundBounce = ground.Bounce,
            groundFriction = ground.Friction
        };

        state.Dependency = groundJob.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public partial struct GroundCollisionJob : IJobEntity
    {
        public float groundLevel;
        public float groundBounce;
        public float groundFriction;

        public void Execute(ref SpringPoint springPoint)
        {
            float3 pointPosition = springPoint.position;

            float combinedBounce = (springPoint.bounciness + groundBounce) * 0.5f;
            float combinedFriction = math.sqrt(springPoint.friction * groundFriction);

            if (pointPosition.y < groundLevel)
            {
                springPoint.position = new float3(
                    pointPosition.x,
                    groundLevel,
                    pointPosition.z
                );

                springPoint.velocity = new float3(
                    springPoint.velocity.x * combinedFriction,
                    -springPoint.velocity.y * combinedBounce,
                    springPoint.velocity.z * combinedFriction
                );
            }
        }
    }
}
