﻿using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace PressPlay.FFWD.Components
{
    public class MeshFilter : Component
    {
        [ContentSerializer(ElementName = "mesh", Optional = true)]
        public Mesh sharedMesh;

        private Mesh _mesh;
        [ContentSerializerIgnore]
        public Mesh mesh 
        { 
            get
            {
                if ((_mesh == null) && (sharedMesh != null))
                {
                    _mesh = (Mesh)sharedMesh.Clone();
                }
                return _mesh;
            }
            set
            {
                _mesh = value;
            }
        }

        internal Mesh meshToRender
        {
            get
            {
                return _mesh ?? sharedMesh;
            }
        }

        public BoundingSphere boundingSphere
        {
            get
            {
                if (meshToRender != null)
                {
                    return meshToRender.boundingSphere;
                }
                return new BoundingSphere();
            }
        }

        public override void Awake()
        {
            base.Awake();
            // NOTE: I am not sure why we did this. But I have removed the functionality for now as I want to share meshes if possible.
            // Do this to force the mesh to get cloned on awake if it is already set.
            // If sharedMesh is changed later the clone will happen there.
            //Mesh mesh = this.mesh;
        }

        internal bool CanBatch()
        {
            return (meshToRender != null && meshToRender.vertices != null);
        }

        public ModelMesh GetModelMesh()
        {
            if (sharedMesh != null)
            {
                return sharedMesh.GetModelMesh();
            }
            return null;
        }
    }
}
