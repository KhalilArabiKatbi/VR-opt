using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CollisionSystem))]
public partial struct IntegrationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new IntegrationJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public partial struct IntegrationJob : IJobEntity
    {
        public float deltaTime;

        public void Execute(ref SpringPoint springPoint, ref Translation translation)
        {
            if (springPoint.isFixed != 0) return;

            // --- NaN/Origin Checks ---
            float3 position = springPoint.predictedPosition;
            if (math.any(math.isnan(position)))
            {
                springPoint.force = float3.zero;
                springPoint.velocity = float3.zero;
                return;
            }

            // Prevent division by zero
            float mass = math.max(springPoint.mass, 1f);

            // --- Force/Velocity Validation ---
            float3 force = springPoint.force;
            if (!math.any(math.isnan(force)))
            {
                float3 acceleration = force / mass;
                float3 velocity = springPoint.velocity + (acceleration * deltaTime);

                // More conservative velocity clamping (50 units/s squared)
                if (math.lengthsq(velocity) > 2500f)
                {
                    velocity = math.normalize(velocity) * 50f;
                }

                springPoint.velocity = velocity;
            }

            float3 predictedPosition = position + (springPoint.velocity * deltaTime);
            if (!math.any(math.isnan(predictedPosition)) && math.length(predictedPosition) < 100000f)
            {
                springPoint.predictedPosition = predictedPosition;
            }
            else
            {
                springPoint.velocity = float3.zero;
            }

            // Reset force for next integration
            springPoint.force = float3.zero;

            // Update point velocity
            float3 new_velocity = (springPoint.predictedPosition - springPoint.position) / deltaTime;
            new_velocity *= 0.98f; // 0.98f ~ 2% damping

            // More conservative velocity clamping (50 units/s squared)
            if (math.lengthsq(new_velocity) > 2500f)
            {
                new_velocity = math.normalize(new_velocity) * 50f;
            }

            springPoint.velocity = new_velocity;

            // Update point position
            springPoint.position = springPoint.predictedPosition;
            translation.Value = springPoint.position;
        }
    }
}
