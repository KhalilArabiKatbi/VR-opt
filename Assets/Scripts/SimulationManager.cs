using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public MeshFilter meshFilter;
    public int maxSubdivisionLevel = 3;
    public float influenceRadius = 1.0f;
    public bool showWeights = false;
    public bool logSubdividedTriangles = true;

    private EntityManager entityManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Create a gravity entity
        var gravityEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(gravityEntity, new Gravity { Value = new float3(0, -9.81f, 0) });

        // Create a ground entity
        var groundEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(groundEntity, new Ground { Level = 0, Bounce = 0.5f, Friction = 0.5f });


        if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;

        // Create entities for each vertex in the mesh
        var vertices = mesh.vertices;
        var springPoints = new NativeArray<SpringPoint>(vertices.Length, Allocator.Temp);
        var entities = new NativeArray<Entity>(vertices.Length, Allocator.Temp);
        for (int i = 0; i < vertices.Length; i++)
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new LocalToWorld());
            entityManager.AddComponentData(entity, new Translation { Value = vertices[i] });
            var springPoint = new SpringPoint
            {
                position = vertices[i],
                initialPosition = vertices[i],
                predictedPosition = vertices[i],
                mass = 1.0f,
                isFixed = 0,
                bounciness = 0.5f,
                friction = 0.5f,
            };
            entityManager.AddComponentData(entity, springPoint);
            entities[i] = entity;
            springPoints[i] = springPoint;
        }

        // Precompute vertex mappings
        var vertexWeightBindings = new NativeArray<VertexWeightBinding>(vertices.Length, Allocator.Persistent);
        PrecomputeVertexMapping(vertices, springPoints, ref vertexWeightBindings);

        // Create a mesh deformer entity
        var meshDeformerEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(meshDeformerEntity, new MeshDeformerComponent
        {
            mesh = mesh,
            worldToLocal = transform.worldToLocalMatrix,
            vertexWeightBindings = vertexWeightBindings
        });

        // Create entities for each edge in the mesh
        var triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var entityA = entities[triangles[i]];
            var entityB = entities[triangles[i + 1]];
            var entityC = entities[triangles[i + 2]];

            CreateSpringConnection(entityA, entityB, 1.0f, 100.0f, 10.0f);
            CreateSpringConnection(entityB, entityC, 1.0f, 100.0f, 10.0f);
            CreateSpringConnection(entityC, entityA, 1.0f, 100.0f, 10.0f);
        }

        entities.Dispose();
    }

    private void CreateSpringConnection(Entity a, Entity b, float restLength, float springConstant, float damperConstant)
    {
        var entity = entityManager.CreateEntity();
        entityManager.AddComponentData(entity, new SpringConnection
        {
            a = a,
            b = b,
            restLength = restLength,
            springConstant = springConstant,
            damperConstant = damperConstant,
        });
    }

    public void PrecomputeVertexMapping(Vector3[] vertices, NativeArray<SpringPoint> springPoints, ref NativeArray<VertexWeightBinding> vertexWeightBindings)
    {
        int vertexCount = vertices.Length;

        for (int i = 0; i < vertexCount; i++)
        {
            // Transform vertex to world space
            Vector3 worldPos = transform.TransformPoint(vertices[i]);

            // Find closest spring points and their squared distances
            var closest = new NativeList<(int, float)>(Allocator.Temp);
            for (int j = 0; j < springPoints.Length; j++)
            {
                float distSq = math.lengthsq((float3)worldPos - springPoints[j].position);
                closest.Add((j, distSq));
            }

            // Sort by distance ascending
            closest.Sort(new DistanceComparer());

            // We want exactly 3 entries per vertex. If less, fill with dummy entries.
            // Compute total inverse distance weight for normalization
            float totalWeight = 0f;
            int limit = math.min(3, closest.Length);

            var invDistances = new NativeArray<float>(3, Allocator.Temp);
            var indices = new NativeArray<int>(3, Allocator.Temp);

            for (int k = 0; k < limit; k++)
            {
                invDistances[k] = 1f / (closest[k].Item2 + 1e-4f);
                indices[k] = closest[k].Item1;
                totalWeight += invDistances[k];
            }

            // Fill missing entries with index -1 and zero weight
            for (int k = limit; k < 3; k++)
            {
                invDistances[k] = 0f;
                indices[k] = -1; // invalid index
            }

            // Normalize weights and assign to the struct
            vertexWeightBindings[i] = new VertexWeightBinding
            {
                wp0 = new WeightedPoint
                {
                    index = indices[0],
                    weight = (totalWeight > 0f) ? invDistances[0] / totalWeight : 0f
                },
                wp1 = new WeightedPoint
                {
                    index = indices[1],
                    weight = (totalWeight > 0f) ? invDistances[1] / totalWeight : 0f
                },
                wp2 = new WeightedPoint
                {
                    index = indices[2],
                    weight = (totalWeight > 0f) ? invDistances[2] / totalWeight : 0f
                }
            };
        }
    }

    private struct DistanceComparer : System.Collections.Generic.IComparer<(int, float)>
    {
        public int Compare((int, float) a, (int, float) b)
        {
            return a.Item2.CompareTo(b.Item2);
        }
    }
}
