using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace UnityEcho.Mechanics.Utils
{
    public class MeshConnectivityBuilderHelper : MonoBehaviour
    {
        private readonly Dictionary<Mesh, MeshConnectivityBuilder> _connectivityBuilders = new();

        private void OnDestroy()
        {
            foreach (var connectivityBuilder in _connectivityBuilders)
            {
                connectivityBuilder.Value.Dispose();
            }
        }

        public MeshConnectivityBuilder GetOrBuild(Mesh mesh)
        {
            if (_connectivityBuilders.TryGetValue(mesh, out var connectivityBuilder))
            {
                return connectivityBuilder;
            }

            var vertices = new NativeArray<Vector3>(mesh.vertices, Allocator.Persistent).Reinterpret<float3>();
            var triangles = new NativeArray<int>(mesh.triangles, Allocator.Persistent);

            connectivityBuilder = new MeshConnectivityBuilder(triangles.Reinterpret<int3>(sizeof(int)), vertices, Allocator.Persistent);
            _connectivityBuilders.Add(mesh, connectivityBuilder);
            var job = new MeshConnectivityBuilderJob { builder = connectivityBuilder };
            job.Run();

            return connectivityBuilder;
        }

        [BurstCompile]
        private struct MeshConnectivityBuilderJob : IJob
        {
            public MeshConnectivityBuilder builder;

            #region Implementation of IJob

            public void Execute()
            {
                var weldedVertices = MeshConnectivityBuilder.WeldVertices(
                    builder.TrianglesRaw.Reinterpret<int>(sizeof(int) * 3),
                    builder.VerticesRaw);
                NativeArray<float3>.Copy(weldedVertices.AsArray(), builder.VerticesRaw, weldedVertices.Length);

                builder.Build(weldedVertices.Length);
            }

            #endregion
        }
    }
}