﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using PressPlay.FFWD.Extensions;
using Microsoft.Xna.Framework;
using System.Collections;

namespace PressPlay.FFWD.Components
{
    internal abstract class RenderItem
    {
        private static int nextId = 1;

        protected RenderItem()
        {
            Id = nextId++;
        }

        public int Id { get; private set; }
        public Material Material;
        public int Priority;

        protected int batches = -1;
        protected bool UseVertexColor = false;
        protected BoundingSphere Bounds;
        protected VertexBuffer VertexBuffer;
        protected IndexBuffer IndexBuffer;
        protected short[] indexData;

        private List<int> Transforms = new List<int>(1);
        private Dictionary<int, PooledPriorityQueue> CameraCullingInfo = new Dictionary<int, PooledPriorityQueue>(1);

        protected const int MAX_INDEX_BUFFER_SIZE = Int16.MaxValue;

        public abstract bool AddMesh(Mesh mesh, Matrix matrix, int subMeshIndex);
        public abstract void Initialize(GraphicsDevice device);

        private static Dictionary<string, RenderItem> RenderItemPool = new Dictionary<string, RenderItem>();

        private static int hitPool = 0;
        private static int missPool = 0;

        private Func<Camera, Transform, float> GetTransformPriority;

        internal static RenderItem Create(Material material, Mesh mesh, int subMeshIndex, Transform t)
        {
            RenderItem item;

            string id = material.GetInstanceID() + ":" + mesh.GetInstanceID();
            if (RenderItemPool.ContainsKey(id))
            {
                item = RenderItemPool[id];
                hitPool++;
            }
            else
            {
                missPool++;
                // TODO: The selection needs to be configurable from outside
                if (material.shader.supportsLights && mesh._normals.HasElements())
                {
                    item = new RenderItem<VertexPositionNormalTexture>(material, AddVertexPositionNormalTexture);
                }
                else
                {
                    if (material.shader.supportsVertexColor && mesh.colors.HasElements())
                    {
                        item = new RenderItem<VertexPositionColorTexture>(material, AddVertexPositionColorTexture);
                        item.UseVertexColor = true;
                    }
                    else
                    {
                        item = new RenderItem<VertexPositionTexture>(material, AddVertexPositionTexture);
                    }
                }
                item.Priority = material.renderQueue;
                item.AddMesh(mesh, t.world, subMeshIndex);
                if (material.IsTransparent())
                {
                    item.GetTransformPriority = GetTransparentPriority;
                }
                else
	            {
                    item.GetTransformPriority = GetOpaquePriority;
	            }
                RenderItemPool[id] = item;
            }
            item.AddTransform(t);
            return item;
        }

        private void AddTransform(Transform t)
        {
            int id = t.GetInstanceID();
            if (!Transforms.Contains(id))
            {
                Transforms.Add(t.GetInstanceID());
            }
        }

        internal void RemoveReference(Transform t)
        {
            Transforms.Remove(t.GetInstanceID());
        }

        private bool Alive()
        {
            return Transforms.Count > 0;
        }

        public void Render(GraphicsDevice device, Camera cam)
        {
            bool devicePrepared = false;
            int id = cam.GetInstanceID();  
            if (!CameraCullingInfo.ContainsKey(id))
	        {
                return;
	        }
            PooledPriorityQueue cullingInfo = CameraCullingInfo[id];
            for (int i = 0; i < cullingInfo.Count; i++)
            {
                Transform t = Application.Find<Transform>(cullingInfo[i]);
                if (t == null)
                {
                    continue;
                }
                if (!t.renderer.enabled || !t.gameObject.active)
                {
                    continue;
                }
#if DEBUG
                if (Camera.logRenderCalls)
                {
                    Debug.LogFormat("Render: {0} for {1} on {2}", this, t.gameObject, cam.gameObject);
                }
#endif
                Effect e = Material.shader.effect;
                if (!devicePrepared)
                {
                    devicePrepared = true;
                    device.SetVertexBuffer(VertexBuffer);
                    device.Indices = IndexBuffer;

                    Material.shader.ApplyPreRenderSettings(Material, UseVertexColor);
                    Material.SetBlendState(device);

                    IEffectMatrices ems = e as IEffectMatrices;
                    if (ems != null)
                    {
                        ems.World = t.world;
                        ems.View = cam.view;
                        ems.Projection = cam.projectionMatrix;
                    }
                }
                else
	            {
                    IEffectMatrices ems = e as IEffectMatrices;
                    if (ems != null)
                    {
                        ems.World = t.world;
                    }
	            }
                foreach (EffectPass pass in e.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        VertexBuffer.VertexCount,
                        0,
                        IndexBuffer.IndexCount / 3
                    );
                }
                RenderStats.AddDrawCall(batches, VertexBuffer.VertexCount, IndexBuffer.IndexCount / 3);
            }
        }

        private static VertexPositionNormalTexture AddVertexPositionNormalTexture(Microsoft.Xna.Framework.Vector3 position, Microsoft.Xna.Framework.Vector3 normal, Microsoft.Xna.Framework.Vector2 tex0, Microsoft.Xna.Framework.Vector2 tex1, Microsoft.Xna.Framework.Color c)
        {
            return new VertexPositionNormalTexture(position, normal, tex0);
        }

        private static VertexPositionTexture AddVertexPositionTexture(Microsoft.Xna.Framework.Vector3 position, Microsoft.Xna.Framework.Vector3 normal, Microsoft.Xna.Framework.Vector2 tex0, Microsoft.Xna.Framework.Vector2 tex1, Microsoft.Xna.Framework.Color c)
        {
            return new VertexPositionTexture(position, tex0);
        }

        private static VertexPositionColorTexture AddVertexPositionColorTexture(Microsoft.Xna.Framework.Vector3 position, Microsoft.Xna.Framework.Vector3 normal, Microsoft.Xna.Framework.Vector2 tex0, Microsoft.Xna.Framework.Vector2 tex1, Microsoft.Xna.Framework.Color c)
        {
            return new VertexPositionColorTexture(position, c, tex0);
        }

        private static VertexPositionColor AddVertexPositionColor(Microsoft.Xna.Framework.Vector3 position, Microsoft.Xna.Framework.Vector3 normal, Microsoft.Xna.Framework.Vector2 tex0, Microsoft.Xna.Framework.Vector2 tex1, Microsoft.Xna.Framework.Color c)
        {
            return new VertexPositionColor(position, c);
        }

        public override string ToString()
        {
            return String.Format("#{0} ({1}) {2}", Id, Priority, Material.name);
        }

        internal bool UpdateCullingInfo(Camera cam)
        {
            PooledPriorityQueue cullingInfo = null;
            int id = cam.GetInstanceID();
            if (CameraCullingInfo.ContainsKey(id))
            {
                cullingInfo = CameraCullingInfo[id];
                cullingInfo.Clear();
            }
#if DEBUG
            if (ApplicationSettings.LogSettings.LogCulling)
            {
                Debug.LogFormat("Update culling for {0}", cam.gameObject);
            }
#endif
            for (int i = 0; i < Transforms.Count; i++)
            {
                Transform t = Application.Find<Transform>(Transforms[i]);
                // Check the layer and cull accordingly
                if (t != null && (cam.cullingMask & (1 << t.gameObject.layer)) > 0)
                {
                    // Check frustum culling
                    // TODO: Here we should use something like an octtree to make full scanning faster
                    BoundingSphere sphere = new BoundingSphere(t.TransformPoint(Bounds.Center), Bounds.Radius * Math.Max(Math.Abs(t.lossyScale.x), Math.Max(Math.Abs(t.lossyScale.y), Math.Abs(t.lossyScale.z))));
                    ContainmentType contain;
                    cam.frustum.Contains(ref sphere, out contain);
                    if (contain != ContainmentType.Disjoint)
                    {
                        float priority = GetTransformPriority(cam, t);
#if DEBUG
                        if (ApplicationSettings.LogSettings.LogCulling)
                        {
                            Debug.LogFormat("Put {0} in renderqueue with priority {1}", t.gameObject, priority);
                        }
#endif
                        if (cullingInfo == null)
                        {
                            cullingInfo = new PooledPriorityQueue(Transforms.Count);
                            CameraCullingInfo[id] = cullingInfo;
                        }
                        cullingInfo.Add(t.GetInstanceID(), priority);
                    }
                }
            }
            return cullingInfo.Count > 0;
        }

        internal bool UpdateCullingInfo(Camera cam, Transform t)
        {
            PooledPriorityQueue cullingInfo = null;
            int id = cam.GetInstanceID();
            int transformId = t.GetInstanceID();
            if (CameraCullingInfo.ContainsKey(id))
            {
                cullingInfo = CameraCullingInfo[id];
                cullingInfo.Remove(transformId);
            }
#if DEBUG
            if (ApplicationSettings.LogSettings.LogCulling)
            {
                Debug.LogFormat("Update culling for {0} on {1}", t, cam.gameObject);
            }
#endif
            // Check the layer and cull accordingly
            if (t != null && (cam.cullingMask & (1 << t.gameObject.layer)) > 0)
            {
                // Check frustum culling
                // TODO: Here we should use something like an octtree to make full scanning faster
                BoundingSphere sphere = new BoundingSphere(t.TransformPoint(Bounds.Center), Bounds.Radius * Math.Max(Math.Abs(t.lossyScale.x), Math.Max(Math.Abs(t.lossyScale.y), Math.Abs(t.lossyScale.z))));
                ContainmentType contain;
                cam.frustum.Contains(ref sphere, out contain);
                if (contain != ContainmentType.Disjoint)
                {
                    float priority = GetTransformPriority(cam, t);
#if DEBUG
                    if (ApplicationSettings.LogSettings.LogCulling)
                    {
                        Debug.LogFormat("Put {0} in renderqueue with priority {1}", t.gameObject, priority);
                    }
#endif
                    if (cullingInfo == null)
                    {
                        cullingInfo = new PooledPriorityQueue(Transforms.Count);
                        CameraCullingInfo[id] = cullingInfo;
                    }
                    cullingInfo.Add(t.GetInstanceID(), priority);
                    return true;
                }
            }
            return false;
        }

        private static float GetTransparentPriority(Camera cam, Transform t)
        {
            return -Vector3.DistanceSquared(t.position, cam.transform.position);
        }

        private static float GetOpaquePriority(Camera cam, Transform t)
        {
            return 0;
        }
    }

    /// <summary>
    /// This contains an item that is to be rendered.
    /// </summary>
    internal class RenderItem<T> : RenderItem where T : struct
    {
        internal T[] vertexData;
        private AddVertex addVertex;

        public RenderItem(Material mat, AddVertex addV)
            : base()
        {
            Material = mat;
            addVertex = addV;
        }

        public delegate T AddVertex(Microsoft.Xna.Framework.Vector3 position, Microsoft.Xna.Framework.Vector3 normal, Microsoft.Xna.Framework.Vector2 tex0, Microsoft.Xna.Framework.Vector2 tex1, Microsoft.Xna.Framework.Color c);

        /// <summary>
        /// Adds a mesh to be rendered by this render item.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public override bool AddMesh(Mesh mesh, Matrix matrix, int subMeshIndex)
        {
            if (!mesh._vertices.HasElements())
	        {
                return false;
	        }

            int vertexOffset = 0;
            if (vertexData == null)
            {
                if (mesh.vertices.Length > RenderItem.MAX_INDEX_BUFFER_SIZE)
                {
                    return false;
                }
                vertexData = new T[mesh.vertexCount];
            }
            else
            {
                vertexOffset = vertexData.Length;
                if (mesh.vertices.Length + vertexOffset > RenderItem.MAX_INDEX_BUFFER_SIZE)
                {
                    return false;
                }
                T[] oldVerts = vertexData;
                vertexData = new T[vertexOffset + mesh.vertexCount];
                oldVerts.CopyTo(vertexData, 0);
            }
            batches++;

            for (int i = 0; i < mesh._vertices.Length; i++)
            {
                // Modify UV coordinates for tiling
                Microsoft.Xna.Framework.Vector2 uv1 = new Vector2(
                        mesh._uv[i].X * Material.mainTextureScale.x + Material.mainTextureOffset.x,
                        1 - ((1 - mesh._uv[i].Y) * Material.mainTextureScale.y + Material.mainTextureOffset.y));

                vertexData[i + vertexOffset] = addVertex(mesh._vertices[i], (mesh._normals.HasElements()) ? mesh._normals[i] : Microsoft.Xna.Framework.Vector3.Zero, uv1, (mesh._uv2.HasElements()) ? mesh._uv2[i] : Microsoft.Xna.Framework.Vector2.Zero, (mesh.colors.HasElements()) ? mesh.colors[i] : Color.white);

            }
            if (Bounds.Radius == 0)
	        {
                Bounds = mesh.bounds.boundingSphere;
	        }
            else
	        {
                Bounds = BoundingSphere.CreateMerged(Bounds, mesh.bounds.boundingSphere);
            }

            short[] tris = mesh.GetTriangles(subMeshIndex);
            if (indexData == null)
            {
                indexData = tris.ToArray();
            }
            else
            {
                short[] oldIndexData = indexData;
                indexData = new short[oldIndexData.Length + tris.Length];
                oldIndexData.CopyTo(indexData, 0);
                tris.CopyTo(indexData, oldIndexData.Length);
            }
            return true;
        }

        /// <summary>
        /// Sets up the VertexBuffers and IndexBuffers with the data.
        /// </summary>
        public override void Initialize(GraphicsDevice device)
        {
            if (VertexBuffer == null && vertexData.HasElements())
            {
                VertexBuffer = new VertexBuffer(device, typeof(T), vertexData.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData<T>(vertexData);
                vertexData = null;

                IndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, indexData.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(indexData);
                indexData = null;
            }
        }
    }
}
