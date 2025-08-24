using GLTF.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Plugins;

namespace UnityEcho.Utils
{
    public class LevelGLTFImportPluginContext : GLTFImportPluginContext
    {
        private readonly Dictionary<int, GameObject> _gameObjects = new();

        private bool CheckMeshAndNode<T>(Node node, string key, Predicate<T> predicate, out T value)
        {
            if (node.Extras != null)
            {
                var objValue = node.Extras[key];
                if (objValue != null && predicate.Invoke(objValue.Value<T>()))
                {
                    value = objValue.Value<T>();
                    return true;
                }
            }

            if (node.Mesh != null && node.Mesh.Value.Extras != null)
            {
                var meshValue = node.Mesh.Value.Extras[key];
                if (meshValue != null && predicate.Invoke(meshValue.Value<T>()))
                {
                    value = meshValue.Value<T>();
                    return true;
                }
            }

            value = default;
            return false;
        }

        #region Overrides of GLTFImportPluginContext

        public override void OnAfterImport()
        {
            foreach (var o in _gameObjects)
            {
                if (o.Value.TryGetComponent(out MeshFilter filter) && !o.Value.TryGetComponent(out MeshCollider meshCollider))
                {
                    o.Value.AddComponent<MeshCollider>();
                }
            }

            _gameObjects.Clear();
        }

        public override void OnAfterImportNode(Node node, int nodeIndex, GameObject nodeObject)
        {
            if (CheckMeshAndNode<string>(node, "tag", t => !string.IsNullOrEmpty(t), out var tag))
            {
                nodeObject.tag = tag;
            }

            if (nodeObject.TryGetComponent(out MeshCollider meshCollider) && CheckMeshAndNode<bool>(node, "convex", t => true, out var convex))
            {
                meshCollider.convex = convex;
            }

            if (node.Extras?["static"]?.Value<bool>() ?? node.Name.StartsWith("static_"))
            {
                nodeObject.isStatic = true;
            }

            _gameObjects.Add(nodeIndex, nodeObject);
        }

        #endregion
    }

    public class LevelGLTFImportPlugin : GLTFImportPlugin
    {
        #region Overrides of GLTFPlugin

        public override string DisplayName => "Level Info";

        public override GLTFImportPluginContext CreateInstance(GLTFImportContext context)
        {
            return new LevelGLTFImportPluginContext();
        }

        #endregion
    }
}