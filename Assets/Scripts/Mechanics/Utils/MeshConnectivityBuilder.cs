// Copy from Unity Physics Package

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics
{
    public struct MeshConnectivityBuilder : IDisposable
    {
        private const float k_MergeCoplanarTrianglesTolerance = 1e-4f;

        internal NativeArray<Triangle> Triangles;

        internal NativeArray<int3> TrianglesRaw;

        internal NativeArray<float3> VerticesRaw;

        public MeshConnectivityBuilder(NativeArray<int3> triangles, NativeArray<float3> vertices, Allocator allocator)
        {
            var numTriangles = triangles.Length;
            var numVertices = vertices.Length;

            VerticesRaw = vertices;
            TrianglesRaw = triangles;
            Triangles = new NativeArray<Triangle>(numTriangles, allocator);
        }

        // Copyright 2009-2021 Intel Corporation
        // SPDX-License-Identifier: Apache-2.0
        public static float3 closestPointOnTriangle(float3 p, float3 a, float3 b, float3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = p - a;

            var d1 = math.dot(ab, ap);
            var d2 = math.dot(ac, ap);
            if (d1 <= 0 && d2 <= 0)
            {
                return a; //#1
            }

            var bp = p - b;
            var d3 = math.dot(ab, bp);
            var d4 = math.dot(ac, bp);
            if (d3 >= 0 && d4 <= d3)
            {
                return b; //#2
            }

            var cp = p - c;
            var d5 = math.dot(ab, cp);
            var d6 = math.dot(ac, cp);
            if (d6 >= 0 && d5 <= d6)
            {
                return c; //#3
            }

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                var v0 = d1 / (d1 - d3);
                return a + v0 * ab; //#4
            }

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                var v1 = d2 / (d2 - d6);
                return a + v1 * ac; //#5
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
            {
                var v2 = (d4 - d3) / (d4 - d3 + (d5 - d6));
                return b + v2 * (c - b); //#6
            }

            var denom = 1 / (va + vb + vc);
            var v = vb * denom;
            var w = vc * denom;
            return a + v * ab + w * ac; //#0
        }

        public static NativeList<float3> WeldVertices(NativeArray<int> indices, NativeArray<float3> vertices)
        {
            var numVertices = vertices.Length;
            var verticesAndHashes = new NativeArray<VertexWithHash>(numVertices, Allocator.Temp);
            for (var i = 0; i < numVertices; i++)
            {
                verticesAndHashes[i] = new VertexWithHash { Index = i, Vertex = vertices[i], Hash = SpatialHash(vertices[i]) };
            }

            var uniqueVertices = new NativeList<float3>(numVertices, Allocator.Temp);
            var remap = new NativeArray<int>(numVertices, Allocator.Temp);
            verticesAndHashes.Sort(new SortVertexWithHashByHash());

            for (var i = 0; i < numVertices; i++)
            {
                if (verticesAndHashes[i].Index == int.MaxValue)
                {
                    continue;
                }

                uniqueVertices.Add(vertices[verticesAndHashes[i].Index]);
                remap[verticesAndHashes[i].Index] = uniqueVertices.Length - 1;

                for (var j = i + 1; j < numVertices; j++)
                {
                    if (verticesAndHashes[j].Index == int.MaxValue)
                    {
                        continue;
                    }

                    if (verticesAndHashes[i].Hash == verticesAndHashes[j].Hash)
                    {
                        if (verticesAndHashes[i].Vertex.x == verticesAndHashes[j].Vertex.x &&
                            verticesAndHashes[i].Vertex.y == verticesAndHashes[j].Vertex.y &&
                            verticesAndHashes[i].Vertex.z == verticesAndHashes[j].Vertex.z)
                        {
                            remap[verticesAndHashes[j].Index] = remap[verticesAndHashes[i].Index];

                            verticesAndHashes[j] = new VertexWithHash
                            {
                                Index = int.MaxValue, Vertex = verticesAndHashes[j].Vertex, Hash = verticesAndHashes[j].Hash
                            };
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            for (var i = 0; i < indices.Length; i++)
            {
                indices[i] = remap[indices[i]];
            }

            return uniqueVertices;
        }

        public static bool IsTriangleDegenerate(float3 a, float3 b, float3 c)
        {
            const float defaultTriangleDegeneracyTolerance = 1e-7f;

            // Small area check
            {
                var edge1 = a - b;
                var edge2 = a - c;
                var cross = math.cross(edge1, edge2);

                var edge1B = b - a;
                var edge2B = b - c;
                var crossB = math.cross(edge1B, edge2B);

                var cmp0 = defaultTriangleDegeneracyTolerance > math.lengthsq(cross);
                var cmp1 = defaultTriangleDegeneracyTolerance > math.lengthsq(crossB);
                if (cmp0 || cmp1)
                {
                    return true;
                }
            }

            // Point triangle distance check
            {
                var q = a - b;
                var r = c - b;

                var qq = math.dot(q, q);
                var rr = math.dot(r, r);
                var qr = math.dot(q, r);

                var qqrr = qq * rr;
                var qrqr = qr * qr;
                var det = qqrr - qrqr;

                return det == 0.0f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Dotxyz1(float4 lhs, float3 rhs)
        {
            return math.dot(lhs, new float4(rhs, 1));
        }

        // Utility function
        private static void Swap<T>(ref T a, ref T b) where T : struct
        {
            var t = a;
            a = b;
            b = t;
        }

        private static ulong SpatialHash(float3 vertex)
        {
            uint x, y, z;
            unsafe
            {
                var tmp = &vertex.x;
                x = *(uint*)tmp;

                tmp = &vertex.y;
                y = *(uint*)tmp;

                tmp = &vertex.z;
                z = *(uint*)tmp;
            }

            const ulong p1 = 73856093;
            const ulong p2 = 19349663;
            const ulong p3 = 83492791;

            return (x * p1) ^ (y * p2) ^ (z * p3);
        }

        private static int4 GetVertexIndices(NativeArray<int3> triangles, Edge edge)
        {
            int4 vertexIndices;
            var triangle = edge.Triangle;
            vertexIndices.x = triangles[triangle][edge.Start];
            vertexIndices.y = triangles[triangle][(edge.Start + 1) % 3];
            vertexIndices.z = triangles[triangle][(edge.Start + 2) % 3];
            vertexIndices.w = 0;
            return vertexIndices;
        }

        private static int GetApexVertexIndex(NativeArray<int3> triangles, Edge edge)
        {
            var triangleIndex = edge.Triangle;
            return triangles[triangleIndex][(edge.Start + 2) % 3];
        }

        private static float CalcTwiceSurfaceArea(float3 a, float3 b, float3 c)
        {
            var d0 = b - a;
            var d1 = c - a;
            return math.length(math.cross(d0, d1));
        }

        public unsafe void Build(int numVertices)
        {
            var numTriangles = TrianglesRaw.Length;

            var Vertices = new NativeArray<Vertex>(numVertices, Allocator.Temp);

            var triangles = TrianglesRaw;

            for (var i = 0; i < numTriangles; i++)
            {
                var triangle = new Triangle();
                triangle.Clear();
                Triangles[i] = triangle;
            }

            var numEdges = 0;

            // Compute cardinality and triangle flags.
            for (var triangleIndex = 0; triangleIndex < numTriangles; triangleIndex++)
            {
                ((Triangle*)Triangles.GetUnsafePtr())[triangleIndex].IsValid = triangles[triangleIndex][0] != triangles[triangleIndex][1] &&
                                                                               triangles[triangleIndex][1] != triangles[triangleIndex][2] &&
                                                                               triangles[triangleIndex][0] != triangles[triangleIndex][2];
                if (Triangles[triangleIndex].IsValid)
                {
                    ((Vertex*)Vertices.GetUnsafePtr())[triangles[triangleIndex][0]].Cardinality++;
                    ((Vertex*)Vertices.GetUnsafePtr())[triangles[triangleIndex][1]].Cardinality++;
                    ((Vertex*)Vertices.GetUnsafePtr())[triangles[triangleIndex][2]].Cardinality++;
                }
            }

            // Compute vertex first edge index.
            for (var vertexIndex = 0; vertexIndex < numVertices; ++vertexIndex)
            {
                var cardinality = Vertices[vertexIndex].Cardinality;
                ((Vertex*)Vertices.GetUnsafePtr())[vertexIndex].FirstEdge = cardinality > 0 ? numEdges : 0;
                numEdges += cardinality;
            }

            // Compute edges and triangles links.
            var counters = new NativeArray<int>(numVertices, Allocator.Temp);
            var Edges = new NativeArray<Edge>(numEdges, Allocator.Temp);

            for (var triangleIndex = 0; triangleIndex < numTriangles; triangleIndex++)
            {
                if (!Triangles[triangleIndex].IsValid)
                {
                    continue;
                }

                for (int i = 2, j = 0; j < 3; i = j++)
                {
                    var vertexI = triangles[triangleIndex][i];
                    var thisEdgeIndex = Vertices[vertexI].FirstEdge + counters[vertexI]++;
                    Edges[thisEdgeIndex] = new Edge { Triangle = triangleIndex, Start = i, IsValid = true };

                    var vertexJ = triangles[triangleIndex][j];
                    var other = Vertices[vertexJ];
                    var count = counters[vertexJ];

                    for (var k = 0; k < count; k++)
                    {
                        var edge = Edges[other.FirstEdge + k];

                        var endVertexOffset = (edge.Start + 1) % 3;
                        var endVertex = triangles[edge.Triangle][endVertexOffset];
                        if (endVertex == vertexI)
                        {
                            ((Triangle*)Triangles.GetUnsafePtr())[triangleIndex].SetLinks(i, edge);
                            ((Triangle*)Triangles.GetUnsafePtr())[edge.Triangle].SetLinks(edge.Start, Edges[thisEdgeIndex]);
                            break;
                        }
                    }
                }
            }

            // Compute vertices attributes.
            for (var vertexIndex = 0; vertexIndex < numVertices; vertexIndex++)
            {
                var nakedEdgeIndex = -1;
                var numNakedEdge = 0;
                {
                    var firstEdgeIndex = Vertices[vertexIndex].FirstEdge;
                    var numVertexEdges = Vertices[vertexIndex].Cardinality;
                    for (var i = 0; i < numVertexEdges; i++)
                    {
                        var edgeIndex = firstEdgeIndex + i;
                        if (IsNaked(Edges[edgeIndex]))
                        {
                            nakedEdgeIndex = i;
                            numNakedEdge++;
                        }
                    }
                }

                ref var vertex = ref ((Vertex*)Vertices.GetUnsafePtr())[vertexIndex];
                vertex.Manifold = numNakedEdge < 2 && vertex.Cardinality > 0;
                vertex.Boundary = numNakedEdge > 0;
                vertex.Border = numNakedEdge == 1 && vertex.Manifold;

                // Make sure that naked edge appears first.
                if (nakedEdgeIndex > 0)
                {
                    Swap(ref ((Edge*)Edges.GetUnsafePtr())[vertex.FirstEdge], ref ((Edge*)Edges.GetUnsafePtr())[vertex.FirstEdge + nakedEdgeIndex]);
                }

                // Order ring as fan.
                if (vertex.Manifold)
                {
                    var firstEdge = vertex.FirstEdge;
                    var count = vertex.Cardinality;
                    for (var i = 0; i < count - 1; i++)
                    {
                        var prevEdge = GetPrev(Edges[firstEdge + i]);
                        if (IsBound(prevEdge))
                        {
                            var triangle = GetLink(prevEdge).Triangle;
                            if (Edges[firstEdge + i + 1].Triangle != triangle)
                            {
                                var found = false;
                                for (var j = i + 2; j < count; ++j)
                                {
                                    if (Edges[firstEdge + j].Triangle == triangle)
                                    {
                                        Swap(ref ((Edge*)Edges.GetUnsafePtr())[firstEdge + i + 1], ref ((Edge*)Edges.GetUnsafePtr())[firstEdge + j]);
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    vertex.Manifold = false;
                                    vertex.Border = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (vertex.Manifold)
                    {
                        var lastEdge = GetPrev(Edges[firstEdge + count - 1]);
                        if (vertex.Border)
                        {
                            if (IsBound(lastEdge))
                            {
                                vertex.Manifold = false;
                                vertex.Border = false;
                            }
                        }
                        else
                        {
                            if (IsNaked(lastEdge) || GetLink(lastEdge).Triangle != Edges[firstEdge].Triangle)
                            {
                                vertex.Manifold = false;
                            }
                        }
                    }
                }
            }
        }

        public float3 closestPointOnTriangle(float3 p, int triangleIndex)
        {
            var triangle = TrianglesRaw[triangleIndex];
            return closestPointOnTriangle(p, VerticesRaw[triangle[0]], VerticesRaw[triangle[1]], VerticesRaw[triangle[2]]);
        }

        /// <summary>
        /// Used mainly for convex meshes where we don't have the convex mesh info accessible
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int FindClosestStartingTriangle(float3 point, out float3 closestPoint, out float3 closestNormal)
        {
            closestNormal = Vector3.zero;
            var minDistance = float.PositiveInfinity;
            var closestTriangle = -1;
            for (var i = 0; i < TrianglesRaw.Length; i++)
            {
                var closestTrianglePoint = closestPointOnTriangle(point, i);
                var distanceSq = math.lengthsq(closestTrianglePoint - point);
                if (distanceSq < minDistance)
                {
                    minDistance = distanceSq;
                    closestTriangle = i;
                    closestPoint = closestTrianglePoint;
                    var triangle = TrianglesRaw[i];
                    closestNormal = new Plane(VerticesRaw[triangle.x], VerticesRaw[triangle.y], VerticesRaw[triangle.z]).normal;
                }
            }

            closestPoint = point;
            return closestTriangle;
        }

        public bool FindClosestTriangle(
            Vector3 point,
            Vector3 normal,
            int triangleStart,
            float gripRadius,
            out int closestTriangle,
            out float3 closestPoint)
        {
            if (triangleStart < 0)
            {
                closestPoint = point;
                closestTriangle = -1;
                return false;
            }

            closestPoint = default;
            closestTriangle = -1;

            var open = new NativeQueue<int>(Allocator.Temp);
            var closed = new NativeHashSet<int>(128, Allocator.Temp);

            var closestSync = float.PositiveInfinity;

            open.Enqueue(triangleStart);

            while (open.Count > 0)
            {
                var head = open.Dequeue();
                closed.Add(head);

                var closestTrianglePoint = closestPointOnTriangle(point, head);
                var sync = math.distance(point, closestTrianglePoint);

                var triangle = TrianglesRaw[head];
                var a = VerticesRaw[triangle.x];
                var b = VerticesRaw[triangle.y];
                var c = VerticesRaw[triangle.z];
                var triangleNormal = math.normalize(math.cross(b - a, c - a));
                var angle = 1 - math.dot(-normal, triangleNormal);
                var angleSync = angle * angle;

                if (sync + angleSync < closestSync && sync < gripRadius)
                {
                    closestSync = sync + angleSync;
                    closestPoint = closestTrianglePoint;
                    closestTriangle = head;
                }

                var triangleData = Triangles[head];
                if (triangleData.Edge0.IsValid && !closed.Contains(triangleData.Edge0.Triangle))
                {
                    open.Enqueue(triangleData.Edge0.Triangle);
                }
                if (triangleData.Edge1.IsValid && !closed.Contains(triangleData.Edge1.Triangle))
                {
                    open.Enqueue(triangleData.Edge1.Triangle);
                }
                if (triangleData.Edge2.IsValid && !closed.Contains(triangleData.Edge2.Triangle))
                {
                    open.Enqueue(triangleData.Edge2.Triangle);
                }

                if (closed.Count >= 64)
                {
                    // We've looked far enough no need to search further
                    break;
                }
            }

            return float.IsFinite(closestSync);

            /*var start = 0;

            var open = new NativeHeap<NodeCost, NodeCost>(Allocator.Temp, TrianglesRaw.Length);
            var closed = new NativeArray<bool>(TrianglesRaw.Length, Allocator.Temp);
            var walkedCount = 0;
            var fallbackIndex = 0;

            TryAddNode(point, normal, ref closed, ref open, default, start);

            while (open.Count > 0)
            {
                var currentNode = open.Pop();
                walkedCount++;

                if (currentNode.distance <= gabDistance)
                {
                    closestPoint = currentNode.closestPoint;
                    closestTriangle = currentNode.idx;
                    Debug.Log($"Found Closest Triangle while walking {walkedCount} nodes");
                    return true;
                }

                var triangle = Triangles[currentNode.idx];

                TryAddNode(point, normal, ref closed, ref open, currentNode, triangle.Edge0.Triangle);
                TryAddNode(point, normal, ref closed, ref open, currentNode, triangle.Edge1.Triangle);
                TryAddNode(point, normal, ref closed, ref open, currentNode, triangle.Edge1.Triangle);

                if (open.Count <= 0 && fallbackIndex < TrianglesRaw.Length)
                {
                    for (; fallbackIndex < TrianglesRaw.Length; fallbackIndex++)
                    {
                        if (TryAddNode(point, normal, ref closed, ref open, default, fallbackIndex))
                        {
                            break;
                        }
                    }
                }
            }

            closestPoint = default;
            closestTriangle = -1;
            return false;*/
        }

        /// Get the opposite edge.
        internal Edge GetLink(Edge e)
        {
            return e.IsValid ? Triangles[e.Triangle].Links(e.Start) : e;
        }

        internal bool IsBound(Edge e)
        {
            return GetLink(e).IsValid;
        }

        internal bool IsNaked(Edge e)
        {
            return !IsBound(e);
        }

        internal Edge GetNext(Edge e)
        {
            return e.IsValid ? new Edge { Triangle = e.Triangle, Start = (e.Start + 1) % 3, IsValid = true } : e;
        }

        internal Edge GetPrev(Edge e)
        {
            return e.IsValid ? new Edge { Triangle = e.Triangle, Start = (e.Start + 2) % 3, IsValid = true } : e;
        }

        internal int GetStartVertexIndex(Edge e)
        {
            return Triangles[e.Triangle].Links(e.Start).Start;
        }

        internal int GetEndVertexIndex(Edge e)
        {
            return Triangles[e.Triangle].Links((e.Start + 1) % 3).Start;
        }

        internal bool IsEdgeConcaveOrFlat(Edge edge, NativeArray<int3> triangles, NativeArray<float3> vertices, NativeArray<float4> planes)
        {
            if (IsNaked(edge))
            {
                return false;
            }

            var apex = vertices[GetApexVertexIndex(triangles, edge)];
            if (Dotxyz1(planes[edge.Triangle], apex) < -k_MergeCoplanarTrianglesTolerance)
            {
                return false;
            }

            return true;
        }

        internal bool IsTriangleConcaveOrFlat(Edge edge, NativeArray<int3> triangles, NativeArray<float3> vertices, NativeArray<float4> planes)
        {
            for (var i = 0; i < 3; i++)
            {
                var e = GetNext(edge);
                if (!IsEdgeConcaveOrFlat(e, triangles, vertices, planes))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool IsFlat(Edge edge, NativeArray<int3> triangles, NativeArray<float3> vertices, NativeArray<float4> planes)
        {
            var link = GetLink(edge);
            if (!link.IsValid)
            {
                return false;
            }

            var apex = vertices[GetApexVertexIndex(triangles, link)];
            var flat = math.abs(Dotxyz1(planes[edge.Triangle], apex)) < k_MergeCoplanarTrianglesTolerance;

            apex = vertices[GetApexVertexIndex(triangles, edge)];
            flat |= math.abs(Dotxyz1(planes[link.Triangle], apex)) < k_MergeCoplanarTrianglesTolerance;

            return flat;
        }

        internal bool IsConvexQuad(Primitive quad, Edge edge, NativeArray<float4> planes)
        {
            float4x2 quadPlanes;
            quadPlanes.c0 = planes[edge.Triangle];
            quadPlanes.c1 = planes[GetLink(edge).Triangle];
            if (Dotxyz1(quadPlanes[0], quad.Vertices[3]) < k_MergeCoplanarTrianglesTolerance)
            {
                if (Dotxyz1(quadPlanes[1], quad.Vertices[1]) < k_MergeCoplanarTrianglesTolerance)
                {
                    var convex = true;
                    for (var i = 0; convex && i < 4; i++)
                    {
                        var delta = quad.Vertices[(i + 1) % 4] - quad.Vertices[i];
                        var normal = math.normalize(math.cross(delta, quadPlanes[i >> 1].xyz));
                        var edgePlane = new float4(normal, math.dot(-normal, quad.Vertices[i]));
                        for (var j = 0; j < 2; j++)
                        {
                            if (Dotxyz1(edgePlane, quad.Vertices[(i + j + 1) % 4]) > k_MergeCoplanarTrianglesTolerance)
                            {
                                convex = false;
                                break;
                            }
                        }
                    }
                    return convex;
                }
            }

            return false;
        }

        internal bool CanEdgeBeDisabled(
            Edge e,
            NativeArray<PrimitiveFlags> flags,
            NativeArray<int3> triangles,
            NativeArray<float3> vertices,
            NativeArray<float4> planes)
        {
            if (!e.IsValid || IsEdgeConcaveOrFlat(e, triangles, vertices, planes) || (flags[e.Triangle] & PrimitiveFlags.DisableAllEdges) != 0)
            {
                return false;
            }

            return true;
        }

        internal bool CanAllEdgesBeDisabled(
            NativeArray<Edge> edges,
            NativeArray<PrimitiveFlags> flags,
            NativeArray<int3> triangles,
            NativeArray<float3> vertices,
            NativeArray<float4> planes)
        {
            var allDisabled = true;
            for (var i = 0; i < edges.Length; ++i)
            {
                allDisabled &= CanEdgeBeDisabled(edges[i], flags, triangles, vertices, planes);
            }

            return allDisabled;
        }

        private bool TryAddNode(
            float3 point,
            float3 normal,
            ref NativeArray<bool> closed,
            ref NativeHeap<NodeCost, NodeCost> open,
            NodeCost currentNode,
            int triangleIndex)
        {
            if (!closed[triangleIndex])
            {
                var triangle = TrianglesRaw[triangleIndex];
                var plane = new Plane(VerticesRaw[triangle.x], VerticesRaw[triangle.y], VerticesRaw[triangle.z]);
                var closestPoint = closestPointOnTriangle(point, triangleIndex);
                var newCost = new NodeCost(triangleIndex, closestPoint, math.distance(closestPoint, point));
                var newGCost = currentNode.gCost + 1;

                newCost.gCost = newGCost;
                var hCost = newCost.distance + Vector3.Dot(normal, plane.normal);
                newCost.fCost = hCost + newCost.gCost;

                open.Insert(newCost);

                closed[triangleIndex] = true;

                return true;
            }

            return false;
        }

        private void DisableEdgesOfAdjacentPrimitives(
            NativeList<Primitive> primitives,
            NativeArray<int3> triangles,
            NativeArray<float3> vertices,
            NativeArray<float4> planes,
            NativeArray<PrimitiveFlags> flags,
            NativeList<Edge> quadRoots,
            NativeList<Edge> triangleRoots)
        {
            var outerBoundary = new NativeArray<Edge>(4, Allocator.Temp);

            for (var quadIndex = 0; quadIndex < quadRoots.Length; quadIndex++)
            {
                var root = quadRoots[quadIndex];
                var link = GetLink(root);
                var quadFlags = flags[root.Triangle];
                if ((quadFlags & PrimitiveFlags.IsFlatConvexQuad) == PrimitiveFlags.IsFlatConvexQuad &&
                    (quadFlags & PrimitiveFlags.DisableAllEdges) != PrimitiveFlags.DisableAllEdges)
                {
                    outerBoundary[0] = GetLink(GetNext(root));
                    outerBoundary[1] = GetLink(GetPrev(root));
                    outerBoundary[2] = GetLink(GetNext(link));
                    outerBoundary[3] = GetLink(GetPrev(link));

                    if (CanAllEdgesBeDisabled(outerBoundary, flags, triangles, vertices, planes))
                    {
                        quadFlags |= PrimitiveFlags.DisableAllEdges;
                    }
                }

                // Sync triangle flags.
                flags[root.Triangle] = quadFlags;
                flags[link.Triangle] = quadFlags;

                // Write primitive flags.
                primitives[quadIndex] = new Primitive { Vertices = primitives[quadIndex].Vertices, Flags = quadFlags };
            }

            outerBoundary = new NativeArray<Edge>(3, Allocator.Temp);

            for (var triangleIndex = 0; triangleIndex < triangleRoots.Length; triangleIndex++)
            {
                var root = triangleRoots[triangleIndex];
                var triangleFlags = flags[root.Triangle];
                if ((triangleFlags & PrimitiveFlags.DisableAllEdges) == 0)
                {
                    outerBoundary[0] = GetLink(root);
                    outerBoundary[1] = GetLink(GetNext(root));
                    outerBoundary[2] = GetLink(GetPrev(root));

                    if (CanAllEdgesBeDisabled(outerBoundary, flags, triangles, vertices, planes))
                    {
                        triangleFlags |= PrimitiveFlags.DisableAllEdges;
                    }
                }

                // Sync triangle flags.
                flags[root.Triangle] = triangleFlags;

                // Write primitive flags.
                var primitiveIndex = quadRoots.Length + triangleIndex;
                primitives[primitiveIndex] = new Primitive { Vertices = primitives[primitiveIndex].Vertices, Flags = triangleFlags };
            }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            VerticesRaw.Dispose();
            TrianglesRaw.Dispose();
            Triangles.Dispose();
        }

        #endregion

        [Flags]
        internal enum PrimitiveFlags
        {
            IsTrianglePair = 1 << 0,

            IsFlat = 1 << 1,

            IsConvex = 1 << 2,

            DisableInternalEdge = 1 << 3,

            DisableAllEdges = 1 << 4,

            IsFlatConvexQuad = IsTrianglePair | IsFlat | IsConvex,

            DefaultTriangleFlags = IsFlat | IsConvex,

            DefaultTrianglePairFlags = IsTrianglePair
        }

        public struct NodeCost : IComparer<NodeCost>
        {
            public int idx;

            public float3 closestPoint;

            public float distance;

            public float gCost;

            public float fCost;

            public NodeCost(int idx, float3 closestPoint, float distance)
            {
                this.idx = idx;
                this.closestPoint = closestPoint;
                this.distance = distance;
                gCost = 0;
                fCost = 0;
            }

            #region Implementation of IComparer<NodeCost>

            public int Compare(NodeCost x, NodeCost y)
            {
                return x.fCost.CompareTo(y.fCost);
            }

            #endregion
        }

        /// (Half) Edge.
        internal struct Edge
        {
            internal bool IsValid;

            // Starting vertex index
            internal int Start;

            // Triangle index
            internal int Triangle;

            internal static Edge Invalid()
            {
                return new Edge { IsValid = false };
            }
        }

        internal struct Primitive
        {
            internal PrimitiveFlags Flags;

            internal float3x4 Vertices;
        }

        internal struct Triangle
        {
            // Broken up rather than an array because we need native containers of triangles elsewhere in the code, and
            // nested native containers aren't supported.
            internal Edge Edge0;

            internal Edge Edge1;

            internal Edge Edge2;

            internal bool IsValid;

            public void Clear()
            {
                IsValid = false;
                Edge0.IsValid = false;
                Edge1.IsValid = false;
                Edge2.IsValid = false;
            }

            internal Edge Links(int edge)
            {
                switch (edge)
                {
                    case 0:
                        return Edge0;

                    case 1:
                        return Edge1;

                    case 2:
                        return Edge2;

                    default:
                        return default;
                }
            }

            internal void SetLinks(int edge, Edge newEdge)
            {
                switch (edge)
                {
                    case 0:
                        Edge0 = newEdge;
                        break;

                    case 1:
                        Edge1 = newEdge;
                        break;

                    case 2:
                        Edge2 = newEdge;
                        break;
                }
            }
        }

        /// Vertex.
        internal struct Vertex
        {
            /// true if the vertex is on the border, false otherwise.
            /// Note: if true the first edge of the ring is naked.
            /// Conditions: number of naked edges in the 1-ring is equal to 1.
            internal bool Border;

            /// true if the vertex is on the boundary, false otherwise.
            /// Note: if true the first edge of the ring is naked.
            /// Conditions: number of naked edges in the 1-ring is greater than 0.
            internal bool Boundary;

            /// Number of triangles referencing this vertex, or, equivalently, number of edge starting from this vertex.
            internal int Cardinality;

            /// Index of the first edge.
            internal int FirstEdge;

            /// true is the vertex 1-ring is manifold.
            /// Conditions: number of naked edges in the 1-ring is less than 2 and cardinality is greater than 0.
            internal bool Manifold;
        }

        private struct EdgeData : IComparable<EdgeData>
        {
            internal Edge Edge;

            internal float Value;

            #region Implementation of IComparable<EdgeData>

            public int CompareTo(EdgeData other)
            {
                return Value.CompareTo(other.Value);
            }

            #endregion
        }

        private struct SortVertexWithHashByHash : IComparer<VertexWithHash>
        {
            #region Implementation of IComparer<VertexWithHash>

            public int Compare(VertexWithHash x, VertexWithHash y)
            {
                return x.Hash.CompareTo(y.Hash);
            }

            #endregion
        }

        private struct VertexWithHash
        {
            internal ulong Hash;

            internal int Index;

            internal float3 Vertex;
        }
    }
}