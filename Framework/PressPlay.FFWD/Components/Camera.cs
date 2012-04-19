﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PressPlay.FFWD.Extensions;

namespace PressPlay.FFWD.Components
{
    public class Camera : Behaviour, IComparer<Camera>, IComparer<Renderer>
    {
        public Camera()
        {
            _fieldOfView = MathHelper.ToRadians(60);
            _nearClipPlane = 0.3f;
            _farClipPlane = 1000;
        }

        public enum ClearFlags
        {
            Skybox,
            Color,
            Depth,
            Nothing
        }
        private float _fieldOfView;
        public float fieldOfView { 
            get
            {
                return _fieldOfView;
            }
            set
            {
                _fieldOfView = value;
                _projectionMatrix = Matrix.Identity;
            }
        }
        private float _nearClipPlane;
        public float nearClipPlane
        {
            get
            {
                return _nearClipPlane;
            }
            set
            {
                _nearClipPlane = value;
                _projectionMatrix = Matrix.Identity;
            }
        }
        private float _farClipPlane;
        public float farClipPlane 
        { 
            get
            {
                return _farClipPlane;
            }
            set
            {
                _farClipPlane = value;
                _projectionMatrix = Matrix.Identity;
            }
        }

        private float _orthographicSize;
        public float orthographicSize 
        { 
            get
            {
                return _orthographicSize;
            }
            set
            {
                _orthographicSize = value;
                _projectionMatrix = Matrix.Identity;
            }
        }
        public bool orthographic { get; set; }
        public int depth { get; set; }
        public float aspect { get; set; }
        public int cullingMask { get; set; }
        private readonly List<Renderer> oldRenderQueue = new List<Renderer>(50);
        internal static readonly RenderQueue RenderQueue = new RenderQueue(ApplicationSettings.DefaultCapacities.RenderQueues);
        internal readonly RenderQueue CulledRenderQueue = new RenderQueue(ApplicationSettings.DefaultCapacities.RenderCullingQueue);
        private bool doFullCullingScan;

        public float pixelWidth 
        { 
            get
            {
                return viewPort.Width * rect.width;
            }
        }
        public float pixelHeight
        { 
            get
            {
                return viewPort.Height * rect.height;
            }
        }

        [ContentSerializerIgnore]
        public BoundingFrustum frustum { get; private set; }

        public static bool wireframeRender = false;

        private static DynamicBatchRenderer dynamicBatchRenderer;

        internal static BasicEffect basicEffect;
        [ContentSerializerIgnore]
        public BasicEffect BasicEffect
        {
            get
            {
                return basicEffect;
            }
        }

        private Color _backgroundColor = Color.black;
        public Color backgroundColor
        { 
            get 
            {
                return _backgroundColor;
            }
            set
            {
                _backgroundColor = new Color(value.r, value.g, value.b, 1);
            }
        }

        private Rect _rect;
        public Rect rect 
        { 
            get
            {
                return _rect;
            }
            set
            {
                if (value == _rect)
                {
                    return;
                }
                _rect = value;
                _pixelRect = new Rect(
                    rect.x * FullScreen.Width,
                    rect.y * FullScreen.Height,
                    rect.width * FullScreen.Width,
                    rect.height * FullScreen.Height
                );
                viewPort = new Viewport((int)_pixelRect.x, (int)_pixelRect.y, Mathf.RoundToInt(_pixelRect.width), Mathf.RoundToInt(_pixelRect.height));
                targetChanged = true;
            }
        }
        private Rect _pixelRect;
        [ContentSerializerIgnore]
        public Rect pixelRect 
        {   
            get
            {
                return _pixelRect;
            }
        }
        public ClearFlags clearFlags { get; set; }

        public static Camera main { get; private set; }
        public static Camera mainCamera { get { return main; } }

        internal static Viewport FullScreen;
        internal static GraphicsDevice Device;
        internal static SpriteBatch RenderBatch;

        public Matrix view { get; private set; }

        private bool targetChanged = true;
        private RenderTarget2D target = null;

        public override void Awake()
        {
            CreateRenderTarget();

            frustum = new BoundingFrustum(view * projectionMatrix);
            RecalculateView();

            // This code will be replaced with the new render queue
            for (int i = nonAssignedRenderers.Count - 1; i >= 0; i--)
            {
                if (nonAssignedRenderers[i] == null || nonAssignedRenderers[i].gameObject == null || addRenderer(nonAssignedRenderers[i]))
                {
                    nonAssignedRenderers.RemoveAt(i);
                }
            }
            for (int i = 0; i < _allCameras.Count; i++)
            {
                for (int j = 0; j < _allCameras[i].oldRenderQueue.Count; j++)
                {
                    addRenderer(_allCameras[i].oldRenderQueue[j]);
                }
            }
            // This code will be replaced with the new render queue END

            _allCameras.Add(this);
            _allCameras.Sort(this);

            if (gameObject.CompareTag("MainCamera") && (main == null))
            {
                main = this;
            }
        }

        private void CreateRenderTarget()
        {
            if (target != null)
            {
                target.Dispose();
            }
            if (rect != Rect.unit)
            {
                target = new RenderTarget2D(Device, Mathf.RoundToInt(Device.PresentationParameters.BackBufferWidth * rect.width), Mathf.RoundToInt(Device.PresentationParameters.BackBufferHeight * rect.height), false, SurfaceFormat.Bgra5551, Device.PresentationParameters.DepthStencilFormat, Device.PresentationParameters.MultiSampleCount, RenderTargetUsage.DiscardContents);
            }
            _projectionMatrix = Matrix.Identity;
            targetChanged = false;
        }

        public static void RemoveCamera(Camera cam)
        {
            _allCameras.Remove(cam);
            cam.CulledRenderQueue.Clear();
            if (cam == main)
            {
                main = null;
                for (int i = 0; i < _allCameras.Count; i++)
                {
                    if (_allCameras[i].CompareTag("MainCamera"))
                    {
                        main = _allCameras[i];
                        return;
                    }
                }
            }
        }

        private static List<Camera> _allCameras = new List<Camera>();
        public static IEnumerable<Camera> allCameras
        {
            get
            {
                return _allCameras;
            }
        }

        [ContentSerializerIgnore]
        internal Viewport viewPort; 

        private Matrix _projectionMatrix = Matrix.Identity;
        [ContentSerializerIgnore]
        public Matrix projectionMatrix
        {
            get
            {
                if (_projectionMatrix == Matrix.Identity)
                {
                    if (orthographic)
                    {
                        float aspect = (target != null) ? (rect.width * Device.PresentationParameters.BackBufferWidth) / (rect.height * Device.PresentationParameters.BackBufferHeight) : viewPort.AspectRatio;
                        Matrix.CreateOrthographic(orthographicSize * 2 * aspect, orthographicSize * 2, nearClipPlane, farClipPlane, out _projectionMatrix);
                    }
                    else
                    {
                        Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fieldOfView), FullScreen.AspectRatio, Mathf.Max(ApplicationSettings.DefaultValues.MinimumNearClipPlane, nearClipPlane), farClipPlane, out _projectionMatrix);
                    }
                }
                return _projectionMatrix;
            }
        }

        public Ray ScreenPointToRay(Vector2 screen)
        {
            screen.y = (pixelRect.yMin - screen.y) + pixelRect.height + pixelRect.yMin;
            Vector3 near = viewPort.Unproject(new Vector3(screen.x, screen.y, 0), projectionMatrix, view, Matrix.Identity);
            Vector3 far = viewPort.Unproject(new Vector3(screen.x, screen.y, 1), projectionMatrix, view, Matrix.Identity);
            return new Ray(near, (far - near).normalized);
        }

        public Vector3 ScreenToWorldPoint(Vector3 vector3)
        {
            float normZ = ((vector3.z + nearClipPlane) - nearClipPlane) / (farClipPlane - nearClipPlane);
            vector3.z = normZ;
            vector3.y = (pixelRect.yMin - vector3.y) + pixelRect.height + pixelRect.yMin;
            return viewPort.Unproject(vector3, projectionMatrix, view, Matrix.Identity);
        }

        public Vector3 WorldToViewportPoint(Vector3 position)
        {
            Vector3 v = viewPort.Project(position, projectionMatrix, view, Matrix.Identity);
            v.y = pixelHeight - v.y;
            return v;
        }

        public Vector3 WorldToScreenPoint(Vector3 position)
        {
            Vector3 v = viewPort.Project(position, projectionMatrix, view, Matrix.Identity);
            v.y = pixelHeight - v.y;
            v.z = MathHelper.Lerp(nearClipPlane, farClipPlane, v.z);
            return v;
        }

        public Vector3 ScreenToViewportPoint(Vector3 v)
        {
            Vector2 pt = v;
            pt.x /= FullScreen.Width;
            pt.y /= FullScreen.Height;
            return new Vector3((pt.x - rect.x) / rect.width, (pt.y - rect.y) / rect.height, (float)v);
        }

        public Vector3 ViewportToScreenPoint(Vector3 v)
        {
            return new Vector3(v.x * viewPort.Width, v.y * viewPort.Height, v.z);
        }

        #region Keeping track of renderers
        internal static List<Renderer> nonAssignedRenderers = new List<Renderer>();
        internal static void AddRenderer(Renderer renderer)
        {
            bool isAdded = false;

            if (renderer.isPartOfStaticBatch)
            {
                return;
            }
            if (renderer is MeshRenderer && !ApplicationSettings.UseFallbackRendering)
            {
                renderer.AddRenderItems(RenderQueue);
                if (renderer.renderItems.HasElements())
                {
                    return;
                }
            }

            for (int i = 0; i < _allCameras.Count; i++)
            {
                isAdded |= _allCameras[i].addRenderer(renderer);
            }
            if (!isAdded && !nonAssignedRenderers.Contains(renderer))
            {
                nonAssignedRenderers.Add(renderer);
            }
        }

        private bool addRenderer(Renderer renderer)
        {
            if (oldRenderQueue.Contains(renderer))
            {
                return true;
            }
            if ((cullingMask & (1 << renderer.gameObject.layer)) > 0)
            {
                int index = oldRenderQueue.BinarySearch(renderer, this);
                if (index < 0)
                {
                    oldRenderQueue.Insert(~index, renderer);
                }
                else
                {
                    oldRenderQueue.Insert(index, renderer);
                }
                return true;
            }
            return false;
        }

        internal static void RemoveRenderer(Renderer renderer)
        {
            for (int i = 0; i < _allCameras.Count; i++)
            {
                _allCameras[i].removeRenderer(renderer);
            }
            renderer.RemoveRenderItems(RenderQueue);
        }

        private void removeRenderer(Renderer renderer)
        {
            oldRenderQueue.Remove(renderer);
            renderer.RemoveRenderItems(CulledRenderQueue);
        }

        internal static void ChangeRenderQueue(Renderer renderer)
        {
            for (int i = 0; i < _allCameras.Count; i++)
            {
                Camera cam = _allCameras[i];
                // If the camera has the renderer readd it to the queue
                if (cam.oldRenderQueue.Contains(renderer))
                {
                    cam.removeRenderer(renderer);
                    cam.addRenderer(renderer);
                }
            }
        }
        #endregion

#if DEBUG
        internal static bool logRenderCalls = false;
#endif

        internal static void Culling()
        {
            int camCount = _allCameras.Count;

            // Go through the moved renderers and update culling
            Renderer updated = RenderQueue.GetUpdatedRenderer();
            while (updated != null)
            {
                for (int i = 0; i < camCount; i++)
                {
                    if (!_allCameras[i].doFullCullingScan)
                    {
                        // TODO: We should have a method of updating only the cull info of the moved item and not the entrire camera queue
                        updated.UpdateCullingInfo(_allCameras[i]);
                    }
                }
                updated = RenderQueue.GetUpdatedRenderer();
            }

            for (int i = 0; i < camCount; i++)
            {
                Camera cam = _allCameras[i];
                if (cam.doFullCullingScan)
                {
                    cam.doFullCullingScan = false;
                    cam.CulledRenderQueue.Clear();

                    // Initialize the culling queue for this camera
                    // TODO: Here we should use something like an octtree to make full scanning faster
                    int rqCount = RenderQueue.Count;
                    for (int j = 0; j < rqCount; j++)
                    {
                        RenderItem item = RenderQueue[j];
                        if (item.UpdateCullingInfo(cam))
                        {
                            cam.CulledRenderQueue.Add(item);
                        }
                    }
                }
            }
        }

        internal static void DoRender(GraphicsDevice device)
        {
#if DEBUG && WINDOWS
            if (Input.GetKeyUp(Microsoft.Xna.Framework.Input.Keys.W))
            {
                wireframeRender = !wireframeRender;
            }
            if (Input.GetMouseButtonUp(1))
            {
                Debug.Log("----------- Render log begin ---------------", Time.realtimeSinceStartup);
                logRenderCalls = true;
            }
#endif
            if (dynamicBatchRenderer == null)
            {
                dynamicBatchRenderer = new DynamicBatchRenderer(device);
            }

            if (device == null)
            {
                return;
            }

            device.BlendState = BlendState.Opaque;
            if (wireframeRender)
            {
                RasterizerState state = new RasterizerState();
                state.FillMode = FillMode.WireFrame;
                device.RasterizerState = state;
            }
            else
            {
                device.RasterizerState = RasterizerState.CullNone;
            }

            // Render all cameras that use a render target
            int renderTargets = 0;
            for (int i = 0; i < _allCameras.Count; i++)
            {
                Camera cam = _allCameras[i];
                if (cam.gameObject.active && cam.target != null)
                {
                    renderTargets++;
                    cam.doRender(device);
                }
            }
            // Now render everything
            int directRender = 0;
            device.SetRenderTarget(null);
            for (int i = 0; i < _allCameras.Count; i++)
            {
                Camera cam = _allCameras[i];
                if (cam.gameObject.active)
                {
                    if (cam.target == null)
                    {
                        directRender++;
                        cam.doRender(device);
                    }
                    else
                    {
                        RenderBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                        RenderBatch.Draw(cam.target, new Vector2(cam.pixelRect.x, FullScreen.Height - (cam.pixelRect.y + cam.pixelRect.height)), Microsoft.Xna.Framework.Color.White);
                        RenderBatch.End();
                    }
                }
            }

            GUI.StartRendering();
            GUI.RenderComponents(Application.guiComponents);
            GUI.EndRendering();

#if DEBUG
            Debug.Display("Draw stats", System.String.Format("Draw {0}, Batch draw {1}, Tris {2}, Verts {3}, RTs {4}", RenderStats.DrawCalls, RenderStats.BatchedDrawCalls, RenderStats.TrianglesDrawn, RenderStats.VerticesDrawn, renderTargets));
            logRenderCalls = false;
#endif
        }

        private readonly Matrix inverter = new Matrix(-1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        internal void doRender(GraphicsDevice device)
        {
            if (targetChanged)
            {
                CreateRenderTarget();
            }
            if (target != null)
            {
                device.SetRenderTarget(target);
                device.Clear(Microsoft.Xna.Framework.Color.Transparent);
            }
            Clear(device);

#if DEBUG
            if (logRenderCalls)
            {
                if (target == null)
                {
                    Debug.Log("**** Camera begin : ", name, "****");
                }
                else
                {
                    Debug.Log("**** Camera begin to texture : ", name, "****");
                }
            }
#endif
            RenderStats.Clear();

            if (CulledRenderQueue.Count > 0)
            {
                int rqCount = CulledRenderQueue.Count;
                for (int i = 0; i < rqCount; i++)
                {
                    CulledRenderQueue[i].Render(device, this);
                }
            }
            frustum.Matrix = view * projectionMatrix;

            int q = 0;
            int count = oldRenderQueue.Count;

            if (count > 0)
            {
                BasicEffect.View = view;
                BasicEffect.Projection = projectionMatrix;

                if (Light.HasLights)
                {
                    Light.EnableLighting(BasicEffect, 0);
                }
            }

            for (int i = 0; i < count; i++)
            {
                Renderer r = oldRenderQueue[i];

                if (r.gameObject == null)
                {
                    // This will happen if the game object has been destroyed in update.
                    // It is acceptable behaviour.
                    continue;
                }

                if (r.material == null)
                {
                    // We have no material, so we will skip rendering
                    continue;
                }

                if (r.isPartOfStaticBatch)
                {
                    // The object is statically batched, so we will skip it
                    continue;
                }

                if (r.material.renderQueue != q)
                {
                    if (q > 0)
                    {
                        dynamicBatchRenderer.DoDraw(device, this);
                    }
                    q = r.material.renderQueue;
                }

                if (r.gameObject.active && r.enabled)
                {
                    r.Draw(device, this);
                }
            }

            dynamicBatchRenderer.DoDraw(device, this);
        }

        internal void RecalculateView()
        {
            Matrix m = Matrix.CreateLookAt(
                transform.position,
                transform.position + transform.forward,
                transform.up);
            view = m * inverter;
            doFullCullingScan = true;
        }

        private void Clear(GraphicsDevice device)
        {
            switch (clearFlags)
            {
                case ClearFlags.Skybox:
                    device.Clear(backgroundColor);
                    break;
                case ClearFlags.Color:
                    device.Clear(backgroundColor);
                    break;
                case ClearFlags.Depth:
                    device.Clear(ClearOptions.DepthBuffer, backgroundColor, 1.0f, 0);
                    break;
            }
        }
            
        #region IComparer<Camera> Members
        public int Compare(Camera x, Camera y)
        {
            return x.depth.CompareTo(y.depth);
        }
        #endregion

        #region IComparer<IRenderable> Members
        public int Compare(Renderer x, Renderer y)
        {
            int iRet = x.renderQueue.CompareTo(y.renderQueue);

            if((iRet == 0) && (x.material != null) && (x.material.IsTransparent()))
            {
              Vector3 vPosRendererX = WorldToViewportPoint(x.bounds.min);
              Vector3 vPosRendererY = WorldToViewportPoint(y.bounds.min);

              iRet = vPosRendererY.z.CompareTo(vPosRendererX.z);
            }

            return iRet;
        }
        #endregion

        internal static Camera FindByName(string name)
        {
            for (int i = 0; i < _allCameras.Count; i++)
            {
                if (_allCameras[i].name == name)
                {
                    return _allCameras[i];
                }
            }
            return null;
        }

        internal int BatchRender(Mesh data, Material[] materials, Transform transform)
        {
            int calls = 0;
            for (int i = 0; i < data.subMeshCount; i++)
            {
                Material mat = materials[0];
                if (i < materials.Length)
                {
                    mat = materials[i];
                }
                calls += dynamicBatchRenderer.Draw(this, mat, data, transform, i);
            }
            return calls;
        }

        internal bool DoFrustumCulling(ref BoundingSphere sphere)
        {
            if (sphere.Radius == 0)
            {
                return false;
            }

            ContainmentType contain;
            frustum.Contains(ref sphere, out contain);
            if (contain == ContainmentType.Disjoint)
            {
                return true;
            }
            return false;
        }

        internal bool DoFrustumCulling(ref BoundingBox bbox)
        {
            if (bbox.Min == Microsoft.Xna.Framework.Vector3.Zero &&
                bbox.Max == Microsoft.Xna.Framework.Vector3.Zero)
            {
                return false;
            }

            ContainmentType contain;
            frustum.Contains(ref bbox, out contain);
            if (contain == ContainmentType.Disjoint)
            {
                return true;
            }
            return false;
        }
    }
}
