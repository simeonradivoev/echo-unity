using System;
using Unity.Collections;
using UnityEngine;

namespace UnityEcho.Utils
{
    public class GizmoHelper : MonoBehaviour
    {
        private NativeParallelHashSet<Gizmo> _debugs;

        public NativeParallelHashSet<Gizmo> Debugs => _debugs;

        private void Awake()
        {
            _debugs = new NativeParallelHashSet<Gizmo>(128, Allocator.Persistent);
        }

        private void OnDestroy()
        {
            _debugs.Dispose();
        }

        private void OnDrawGizmos()
        {
            if (_debugs.IsCreated)
            {
                foreach (var gizmo in _debugs)
                {
                    Gizmos.color = gizmo.color;
                    if (gizmo.IsSphere)
                    {
                        Gizmos.DrawWireSphere(gizmo.from, gizmo.radius);
                    }
                    else if (gizmo.IsLine)
                    {
                        Gizmos.DrawLine(gizmo.from, gizmo.to);
                    }
                }
            }
        }

        public void Queue(Gizmo gizmo)
        {
            _debugs.Add(gizmo);
        }

        public void Clear()
        {
            _debugs.Clear();
        }

        public struct Gizmo : IEquatable<Gizmo>
        {
            public bool IsSphere;

            public bool IsLine;

            public Vector3 from;

            public Vector3 to;

            public Color color;

            public float radius;

            #region Overrides of ValueType

            public override bool Equals(object obj)
            {
                return obj is Gizmo other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(IsSphere, IsLine, from, to, color, radius);
            }

            #endregion

            #region Implementation of IEquatable<Gizmo>

            public bool Equals(Gizmo other)
            {
                return IsSphere == other.IsSphere &&
                       IsLine == other.IsLine &&
                       from.Equals(other.from) &&
                       to.Equals(other.to) &&
                       color.Equals(other.color) &&
                       radius.Equals(other.radius);
            }

            #endregion
        }
    }
}