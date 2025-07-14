using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class SurfaceBound : MonoBehaviour
{
    [Range(0, 1)] public float shrink = 0.1f;
    [Range(0, 1)] public float stretch = 0.1f;

    public void ApplyBounds(NativeArray<SpringPointData> points, NativeArray<SpringConnectionData> connections)
    {
        if (!enabled) return;

        for (int i = 0; i < connections.Length; i++)
        {
            var connection = connections[i];
            var p1 = points[connection.pointA];
            var p2 = points[connection.pointB];

            Vector3 p1Pos = p1.position;
            Vector3 p2Pos = p2.position;

            float restLength = connection.restLength;
            float dist = Vector3.Distance(p1Pos, p2Pos);

            if (dist > restLength)
            {
                float min = restLength - (restLength * shrink);
                float max = restLength + (restLength * stretch);

                float error = 0;

                if (dist > max) error = dist - max;
                else if (dist < min) error = dist - min;

                if (error != 0)
                {
                    Vector3 delta = p2Pos - p1Pos;
                    Vector3 correction = delta.normalized * error;
                    Vector3 pHalf = correction * 0.5f;

                    p1.position += (Unity.Mathematics.float3)pHalf;
                    p2.position -= (Unity.Mathematics.float3)pHalf;

                    points[connection.pointA] = p1;
                    points[connection.pointB] = p2;
                }
            }
        }
    }
}
