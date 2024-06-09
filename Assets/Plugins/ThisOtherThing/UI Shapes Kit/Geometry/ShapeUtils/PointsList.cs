using System;
using System.Collections.Generic;
using UnityEngine;
using RoundingProperties = ThisOtherThing.UI.GeoUtils.RoundingProperties;

namespace ThisOtherThing.UI.ShapeUtils
{
    public class PointsList
    {
        private static Vector2 tmpPos;

        private static Vector2 tmpBackV;

        private static Vector2 tmpBackNormV;

        private static Vector2 tmpForwV;

        private static Vector2 tmpForwNormV;

        private static Vector2 tmpBackPos;

        private static Vector2 tmpForwPos;

        private static List<Vector2> tmpCachedPositions = new();

        public static void SetPositions(PointListProperties pointListProperties, ref PointsData lineData)
        {
            if (lineData.Positions == null)
            {
                lineData.Positions = new List<Vector2>(pointListProperties.Positions.Length);
            }

            CheckMinPointDistances(ref pointListProperties.Positions, ref tmpCachedPositions, lineData.LineWeight * 0.5f, lineData.IsClosed);

            lineData.Positions.Clear();

            var inputNumPositions = tmpCachedPositions.Count;

            if (lineData.Positions.Capacity < inputNumPositions)
            {
                lineData.Positions.Capacity = lineData.Positions.Capacity + inputNumPositions + 1;
            }

            // add first position
            if (lineData.IsClosed)
            {
                InterpolatePoints(
                    ref lineData,
                    tmpCachedPositions[inputNumPositions - 1],
                    tmpCachedPositions[0],
                    tmpCachedPositions[1],
                    pointListProperties,
                    0);
            }
            else
            {
                lineData.Positions.Add(tmpCachedPositions[0]);
            }

            for (var i = 1; i < inputNumPositions - 1; i++)
            {
                InterpolatePoints(ref lineData, tmpCachedPositions[i - 1], tmpCachedPositions[i], tmpCachedPositions[i + 1], pointListProperties, i);
            }

            // add end point
            if (lineData.IsClosed)
            {
                InterpolatePoints(
                    ref lineData,
                    tmpCachedPositions[inputNumPositions - 2],
                    tmpCachedPositions[inputNumPositions - 1],
                    tmpCachedPositions[0],
                    pointListProperties,
                    inputNumPositions - 1);
            }
            else
            {
                lineData.Positions.Add(tmpCachedPositions[inputNumPositions - 1]);
            }

            lineData.NumPositions = lineData.Positions.Count;
        }

        public static bool SetLineData(PointListProperties pointListProperties, ref PointsData lineData)
        {
            if (pointListProperties.Positions == null || pointListProperties.Positions.Length <= 1)
            {
                return false;
            }

            var needsUpdate = lineData.NeedsUpdate || lineData.Positions == null;

            if (needsUpdate)
            {
                SetPositions(pointListProperties, ref lineData);
            }

            var numPositions = lineData.NumPositions;

            if (lineData.PositionNormals == null || lineData.PositionNormals.Length != numPositions)
            {
                lineData.PositionTangents = new Vector2[numPositions];
                lineData.PositionNormals = new Vector2[numPositions];
                lineData.PositionDistances = new float[numPositions];
                lineData.NormalizedPositionDistances = new float[numPositions];

                for (var i = 0; i < numPositions; i++)
                {
                    lineData.PositionNormals[i] = GeoUtils.ZeroV2;
                    lineData.PositionTangents[i] = GeoUtils.ZeroV2;
                }

                needsUpdate = true;
            }

            if (needsUpdate)
            {
                var numPositionsMinusOne = numPositions - 1;

                lineData.TotalLength = 0.0f;

                float distance;
                var lastUnitTangent = GeoUtils.ZeroV2;
                var currentUnitTangent = GeoUtils.ZeroV2;

                // set data for first point
                if (!lineData.IsClosed)
                {
                    lineData.PositionTangents[0].x = lineData.Positions[0].x - lineData.Positions[1].x;
                    lineData.PositionTangents[0].y = lineData.Positions[0].y - lineData.Positions[1].y;

                    distance = Mathf.Sqrt(
                        lineData.PositionTangents[0].x * lineData.PositionTangents[0].x +
                        lineData.PositionTangents[0].y * lineData.PositionTangents[0].y);

                    lineData.PositionDistances[0] = distance;
                    lineData.TotalLength += distance;

                    lineData.PositionNormals[0].x = lineData.PositionTangents[0].y / distance;
                    lineData.PositionNormals[0].y = -lineData.PositionTangents[0].x / distance;

                    lastUnitTangent.x = -lineData.PositionTangents[0].x / distance;
                    lastUnitTangent.y = -lineData.PositionTangents[0].y / distance;

                    lineData.StartCapOffset.x = -lastUnitTangent.x;
                    lineData.StartCapOffset.y = -lastUnitTangent.y;
                }
                else
                {
                    lastUnitTangent.x = lineData.Positions[0].x - lineData.Positions[numPositionsMinusOne].x;
                    lastUnitTangent.y = lineData.Positions[0].y - lineData.Positions[numPositionsMinusOne].y;

                    distance = Mathf.Sqrt(lastUnitTangent.x * lastUnitTangent.x + lastUnitTangent.y * lastUnitTangent.y);

                    lastUnitTangent.x /= distance;
                    lastUnitTangent.y /= distance;

                    SetPointData(
                        lineData.Positions[0],
                        lineData.Positions[1],
                        ref currentUnitTangent,
                        ref lineData.PositionTangents[0],
                        ref lineData.PositionNormals[0],
                        ref lastUnitTangent,
                        ref lineData.PositionDistances[0]);

                    lineData.TotalLength += lineData.PositionDistances[0];
                }

                for (var i = 1; i < numPositionsMinusOne; i++)
                {
                    SetPointData(
                        lineData.Positions[i],
                        lineData.Positions[i + 1],
                        ref currentUnitTangent,
                        ref lineData.PositionTangents[i],
                        ref lineData.PositionNormals[i],
                        ref lastUnitTangent,
                        ref lineData.PositionDistances[i]);

                    lineData.TotalLength += lineData.PositionDistances[i];
                }

                // set data for last point
                if (!lineData.IsClosed)
                {
                    lineData.PositionTangents[numPositionsMinusOne].x =
                        lineData.Positions[numPositionsMinusOne].x - lineData.Positions[numPositionsMinusOne - 1].x;
                    lineData.PositionTangents[numPositionsMinusOne].y =
                        lineData.Positions[numPositionsMinusOne].y - lineData.Positions[numPositionsMinusOne - 1].y;

                    distance = Mathf.Sqrt(
                        lineData.PositionTangents[numPositionsMinusOne].x * lineData.PositionTangents[numPositionsMinusOne].x +
                        lineData.PositionTangents[numPositionsMinusOne].y * lineData.PositionTangents[numPositionsMinusOne].y);

                    lineData.EndCapOffset.x = lineData.PositionTangents[numPositionsMinusOne].x / distance;
                    lineData.EndCapOffset.y = lineData.PositionTangents[numPositionsMinusOne].y / distance;

                    lineData.PositionNormals[numPositionsMinusOne].x = -lineData.PositionTangents[numPositionsMinusOne].y / distance;
                    lineData.PositionNormals[numPositionsMinusOne].y = lineData.PositionTangents[numPositionsMinusOne].x / distance;
                }
                else
                {
                    SetPointData(
                        lineData.Positions[numPositionsMinusOne],
                        lineData.Positions[0],
                        ref currentUnitTangent,
                        ref lineData.PositionTangents[numPositionsMinusOne],
                        ref lineData.PositionNormals[numPositionsMinusOne],
                        ref lastUnitTangent,
                        ref lineData.PositionDistances[numPositionsMinusOne]);

                    lineData.TotalLength += lineData.PositionDistances[numPositionsMinusOne];
                }

                if (lineData.GenerateRoundedCaps)
                {
                    SetRoundedCapPointData(
                        Mathf.Atan2(-lineData.PositionNormals[0].x, -lineData.PositionNormals[0].y),
                        ref lineData.StartCapOffsets,
                        ref lineData.StartCapUVs,
                        lineData.RoundedCapResolution,
                        true);

                    SetRoundedCapPointData(
                        Mathf.Atan2(lineData.PositionNormals[numPositionsMinusOne].x, lineData.PositionNormals[numPositionsMinusOne].y),
                        ref lineData.EndCapOffsets,
                        ref lineData.EndCapUVs,
                        lineData.RoundedCapResolution,
                        false);
                }

                var accumulatedLength = 0.0f;
                for (var i = 0; i < lineData.PositionDistances.Length; i++)
                {
                    lineData.NormalizedPositionDistances[i] = accumulatedLength / lineData.TotalLength;
                    accumulatedLength += lineData.PositionDistances[i];
                }
            }

            lineData.NeedsUpdate = false;

            return true;
        }

        private static void CheckMinPointDistances(ref Vector2[] inPositions, ref List<Vector2> outPositions, float minDistance, bool isClosed)
        {
            outPositions.Clear();

            if (outPositions.Capacity < inPositions.Length)
            {
                outPositions.Capacity = inPositions.Length;
            }

            var minSqrDistance = minDistance * minDistance;
            float sqrDistance;

            outPositions.Add(inPositions[0]);

            for (var i = 2; i < inPositions.Length; i++)
            {
                tmpPos.x = inPositions[i].x - inPositions[i - 1].x;
                tmpPos.y = inPositions[i].y - inPositions[i - 1].y;

                sqrDistance = tmpPos.x * tmpPos.x + tmpPos.y * tmpPos.y;

                if (sqrDistance < minSqrDistance)
                {
                    tmpPos.x *= 0.5f;
                    tmpPos.x += inPositions[i - 1].x;

                    tmpPos.y *= 0.5f;
                    tmpPos.y += inPositions[i - 1].y;

                    outPositions.Add(tmpPos);

                    i++;
                }
                else
                {
                    outPositions.Add(inPositions[i - 1]);
                }
            }

            if (!isClosed)
            {
                outPositions.Add(inPositions[inPositions.Length - 1]);
            }
            else
            {
                tmpPos.x = inPositions[inPositions.Length - 1].x - inPositions[0].x;
                tmpPos.y = inPositions[inPositions.Length - 1].y - inPositions[0].y;

                sqrDistance = tmpPos.x * tmpPos.x + tmpPos.y * tmpPos.y;

                if (sqrDistance < minSqrDistance)
                {
                    tmpPos.x *= 0.5f;
                    tmpPos.x += inPositions[0].x;

                    tmpPos.y *= 0.5f;
                    tmpPos.y += inPositions[0].y;

                    outPositions[0] = tmpPos;
                }
                else
                {
                    outPositions.Add(inPositions[inPositions.Length - 1]);
                }
            }
        }

        private static void InterpolatePoints(
            ref PointsData lineData,
            Vector2 prevPosition,
            Vector2 position,
            Vector2 nextPosition,
            PointListProperties pointListProperties,
            int index)
        {
            tmpBackV.x = prevPosition.x - position.x;
            tmpBackV.y = prevPosition.y - position.y;
            var backLength = Mathf.Sqrt(tmpBackV.x * tmpBackV.x + tmpBackV.y * tmpBackV.y);
            tmpBackNormV.x = tmpBackV.x / backLength;
            tmpBackNormV.y = tmpBackV.y / backLength;

            tmpForwV.x = nextPosition.x - position.x;
            tmpForwV.y = nextPosition.y - position.y;
            var forwLength = Mathf.Sqrt(tmpForwV.x * tmpForwV.x + tmpForwV.y * tmpForwV.y);
            tmpForwNormV.x = tmpForwV.x / forwLength;
            tmpForwNormV.y = tmpForwV.y / forwLength;

            var cos = tmpBackNormV.x * tmpForwNormV.x + tmpBackNormV.y * tmpForwNormV.y;
            var angle = Mathf.Acos(cos);

            // ignore points along straight line
            if (cos <= -0.9999f)
            {
                return;
            }

            if (pointListProperties.RoundingDistance > 0.0f)
            {
                AddRoundedPoints(
                    ref lineData,
                    tmpBackNormV,
                    position,
                    tmpForwNormV,
                    pointListProperties,
                    angle,
                    Mathf.Min(backLength, forwLength) * 0.49f);
            }
            else
            {
                if (angle < pointListProperties.MaxAngle)
                {
                    lineData.Positions.Add(position + tmpBackNormV * 0.5f);
                    lineData.Positions.Add(position + tmpForwNormV * 0.5f);
                }
                else
                {
                    lineData.Positions.Add(position);
                }
            }
        }

        private static void AddRoundedPoints(
            ref PointsData lineData,
            Vector2 backNormV,
            Vector2 position,
            Vector2 forwNormV,
            PointListProperties pointListProperties,
            float angle,
            float maxDistance)
        {
            var roundingDistance = Mathf.Min(maxDistance, pointListProperties.RoundingDistance);

            tmpBackPos.x = position.x + backNormV.x * roundingDistance;
            tmpBackPos.y = position.y + backNormV.y * roundingDistance;

            tmpForwPos.x = position.x + forwNormV.x * roundingDistance;
            tmpForwPos.y = position.y + forwNormV.y * roundingDistance;

            pointListProperties.CornerRounding.UpdateAdjusted(roundingDistance / 4.0f, 0.0f, (GeoUtils.TwoPI - angle) / Mathf.PI);

            float interpolator;
            var resolution = pointListProperties.CornerRounding.AdjustedResolution;
            var resolutionF = pointListProperties.CornerRounding.AdjustedResolution - 1.0f;

            if (lineData.Positions.Capacity < lineData.Positions.Count + resolution)
            {
                lineData.Positions.Capacity = lineData.Positions.Count + resolution;
            }

            for (var i = 0; i < resolution; i++)
            {
                interpolator = i / resolutionF;

                tmpPos.x = Mathf.LerpUnclamped(
                    Mathf.LerpUnclamped(tmpBackPos.x, position.x, interpolator),
                    Mathf.LerpUnclamped(position.x, tmpForwPos.x, interpolator),
                    interpolator);

                tmpPos.y = Mathf.LerpUnclamped(
                    Mathf.LerpUnclamped(tmpBackPos.y, position.y, interpolator),
                    Mathf.LerpUnclamped(position.y, tmpForwPos.y, interpolator),
                    interpolator);

                lineData.Positions.Add(tmpPos);
            }
        }

        private static void SetRoundedCapPointData(float centerAngle, ref Vector2[] offsets, ref Vector2[] uvs, int resolution, bool isStart)
        {
            var angleIncrement = Mathf.PI / (resolution + 1);
            var baseAngle = centerAngle;
            float angle;

            if (offsets == null || offsets.Length != resolution)
            {
                offsets = new Vector2[resolution];
                uvs = new Vector2[resolution];
            }

            baseAngle += angleIncrement;

            for (var i = 0; i < resolution; i++)
            {
                angle = baseAngle + angleIncrement * i;

                offsets[i].x = Mathf.Sin(angle);
                offsets[i].y = Mathf.Cos(angle);

                // set angle for uvs
                angle = angleIncrement * i + Mathf.PI * 0.14f;

                if (isStart)
                {
                    angle += Mathf.PI;
                }
                uvs[i].x = Mathf.Abs(Mathf.Sin(angle));

                uvs[i].y = Mathf.Cos(angle) * 0.5f + 0.5f;
            }
        }

        private static void SetPointData(
            Vector2 currentPoint,
            Vector2 nextPoint,
            ref Vector2 currentUnitTangent,
            ref Vector2 positionTangent,
            ref Vector2 positionNormal,
            ref Vector2 lastUnitTangent,
            ref float distance)
        {
            positionTangent.x = currentPoint.x - nextPoint.x;
            positionTangent.y = currentPoint.y - nextPoint.y;

            distance = Mathf.Sqrt(positionTangent.x * positionTangent.x + positionTangent.y * positionTangent.y);

            currentUnitTangent.x = positionTangent.x / distance;
            currentUnitTangent.y = positionTangent.y / distance;

            positionNormal.x = -(lastUnitTangent.x + currentUnitTangent.x);
            positionNormal.y = -(lastUnitTangent.y + currentUnitTangent.y);

            if (positionNormal.x == 0.0f && positionNormal.y == 0.0f)
            {
                positionNormal.x = -lastUnitTangent.y;
                positionNormal.y = lastUnitTangent.x;
            }

            // normalize line normal
            var normalMag = Mathf.Sqrt(positionNormal.x * positionNormal.x + positionNormal.y * positionNormal.y);
            positionNormal.x /= normalMag;
            positionNormal.y /= normalMag;

            var inBetweenAngle = Mathf.Acos(Vector2.Dot(lastUnitTangent, currentUnitTangent)) * 0.5f;

            var angleAdjustedLength = 1.0f / Mathf.Sin(inBetweenAngle);

            if (currentUnitTangent.x * positionNormal.y - currentUnitTangent.y * positionNormal.x > 0.0f)
            {
                angleAdjustedLength *= -1.0f;
            }

            positionNormal.x *= angleAdjustedLength;
            positionNormal.y *= angleAdjustedLength;

            lastUnitTangent.x = -currentUnitTangent.x;
            lastUnitTangent.y = -currentUnitTangent.y;
        }

        [Serializable]
        public class PointListGeneratorData
        {
            public enum Generators { Custom, Rect, Round, RadialGraph, LineGraph, AngleLine, Star, Gear }

            public Generators Generator = Generators.Custom;

            public bool NeedsUpdate = true;

            public Vector2 Center = Vector2.zero;

            [Min(1.0f)]
            public float Width = 10.0f;

            [Min(1.0f)]
            public float Height = 10.0f;

            [Min(1.0f)]
            public float Radius = 10.0f;

            [Range(-1.0f, 1.0f)]
            public float Direction = 1.0f;

            public float[] FloatValues;

            public float MinFloatValue;

            public float MaxFloatValue = 1.0f;

            public int IntStartOffset;

            public float FloatStartOffset;

            public float Length = 1.0f;

            public float EndRadius;

            [Min(2)]
            public int Resolution = 10;

            public bool CenterPoint;

            public bool SkipLastPosition;

            public float Angle;

            public float InnerScaler = 0.8f;

            public float OuterScaler = 0.5f;
        }

        [Serializable]
        public class PointListProperties
        {
            public PointListGeneratorData GeneratorData = new();

            public Vector2[] Positions = { new(-20.0f, 0.0f), new(20.0f, 0.0f), new(20.0f, -20.0f) };

            [Range(0.0f, Mathf.PI)]
            public float MaxAngle = 0.2f;

            [Utils.Min(0.0f)]
            public float RoundingDistance;

            public RoundingProperties CornerRounding = new();

            public bool ShowHandles = true;

            public void SetPoints()
            {
                if (GeneratorData.NeedsUpdate && GeneratorData.Generator != PointListGeneratorData.Generators.Custom)
                {
                    PointsGenerator.SetPoints(ref Positions, GeneratorData);
                }

                GeneratorData.NeedsUpdate = false;
            }
        }

        [Serializable]
        public class PointListsProperties
        {
            public PointListProperties[] PointListProperties;

            public PointListsProperties()
            {
                PointListProperties = new PointListProperties[] { new() };
            }
        }

        public struct PointsData
        {
            public bool NeedsUpdate;

            public bool IsClosed;

            public List<Vector2> Positions;

            public int NumPositions;

            public Vector2[] PositionTangents;

            public Vector2[] PositionNormals;

            public float TotalLength;

            public float[] PositionDistances;

            public float[] NormalizedPositionDistances;

            public Vector2 StartCapOffset;

            public Vector2 EndCapOffset;

            public bool GenerateRoundedCaps;

            public int RoundedCapResolution;

            public Vector2[] StartCapOffsets;

            public Vector2[] StartCapUVs;

            public Vector2[] EndCapOffsets;

            public Vector2[] EndCapUVs;

            public float LineWeight;
        }
    }
}