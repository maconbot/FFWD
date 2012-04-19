﻿using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using PressPlay.FFWD.Components;
using PressPlay.FFWD.Interfaces;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Factories;
using FarseerPhysics;

namespace PressPlay.FFWD
{
    public static class Physics
    {
        private static World _world;
        internal static World world
        {
            get
            {
                if (_world == null)
                {
                    Initialize();
                }
                return _world;
            }
            set
            {
                // TODO: If we have an old world we must dispose it properly
                _world = value;
            }
        }

        private static Vector3 _gravity;
        public static Vector3 gravity
        {
            get
            {
                return _gravity;
            }
            set
            {
                _gravity = value;
                if (_world != null)
                {
                    _world.Gravity = new Microsoft.Xna.Framework.Vector2(_gravity.x, _gravity.y);
                }
            }
        }

        public const int kDefaultRaycastLayers = -5;

        private static bool isPaused = false;
        private static GameObjectContactProcessor contactProcessor;
        public static int velocityIterations = 2;
        public static int positionIterations = 2;

        private static readonly List<Body> rigidBodies = new List<Body>(50);

        internal enum To2dMode { DropX, DropY, DropZ };

        #region FFWD specific methods
        public static void Initialize()
        {
            Initialize(new Microsoft.Xna.Framework.Vector2(gravity.x, gravity.y));
        }

        public static void Initialize(Vector2 gravity)
        {
#if DEBUG
            Settings.EnableDiagnostics = true;
#else
            Settings.EnableDiagnostics = false;
#endif
            Settings.VelocityIterations = velocityIterations;
            Settings.PositionIterations = positionIterations;
            Settings.ContinuousPhysics = false;

            world = new World(gravity);
            
            Physics.contactProcessor = new GameObjectContactProcessor();

            world.ContactManager.BeginContact = Physics.contactProcessor.BeginContact;
            world.ContactManager.EndContact = Physics.contactProcessor.EndContact;
        }

        public static void TogglePause()
        {
            isPaused = !isPaused;
        }

        public static void Update(float elapsedTime)
        {
#if DEBUG
            if (ApplicationSettings.ShowBodyCounter)
	        {
                Debug.Display("Body count", world.BodyList.Count);
	        }
#endif

            world.Step(elapsedTime);

            for (int i = rigidBodies.Count - 1; i >= 0; i--)
            {
                Body body = rigidBodies[i];
                Collider comp = (Collider)body.UserData;
                FarseerPhysics.Common.Transform t;
                body.GetTransform(out t);
                comp.transform.SetPositionFromPhysics(VectorConverter.Convert(t.Position, comp.to2dMode), t.Angle, VectorConverter.GetUp(comp.to2dMode));
            }

            contactProcessor.Update();
        }
        #endregion

        #region Helper methods to create physics objects

        internal static Body AddBody()
        {
            return new Body(world);
        }

        internal static void AddBox(Body body, bool isTrigger, float width, float height, Vector2 position, float density)
        {
            if (world == null)
            {
                throw new InvalidOperationException("You have to Initialize the Physics system before adding bodies");
            }
            if (width < 0)
            {
                Debug.LogWarning(String.Format("Width of the Physics rectangle at {0} is negative. Inverting.", body.UserData.ToString()));
                width = -width;
            }
            if (height < 0)
            {
                Debug.LogWarning(String.Format("Height of the Physics rectangle at {0} is negative. Inverting.", body.UserData.ToString()));
                height = -height;
            }
            Fixture fix = FixtureFactory.AttachRectangle(width, height, density, position, body);
            fix.IsSensor = isTrigger;            
        }

        internal static void AddCircle(Body body, bool isTrigger, float radius, Vector2 position, float density)
        {
            if (world == null)
            {
                throw new InvalidOperationException("You have to Initialize the Physics system before adding bodies");
            }
            CircleShape shp = new CircleShape(radius, density);
            shp.Position = position;
            Fixture fix = body.CreateFixture(shp);
            fix.IsSensor = isTrigger;
        }

        internal static void AddPolygon(Body body, bool isTrigger, Vertices vertices, float density)
        {
            if (world == null)
            {
                throw new InvalidOperationException("You have to Initialize the Physics system before adding bodies");
            }
            PolygonShape shp = new PolygonShape(vertices, density);
            Fixture fix = body.CreateFixture(shp);
            fix.IsSensor = isTrigger;
        }

        internal static void AddMesh(Body body, bool isTrigger, List<Microsoft.Xna.Framework.Vector2[]> tris, float density)
        {
            if (world == null)
            {
                throw new InvalidOperationException("You have to Initialize the Physics system before adding bodies");
            }
            for (int i = 0; i < tris.Count(); i++)
            {
                Vertices verts = new Vertices(tris.ElementAt(i));
                try
                {
                    PolygonShape shp = new PolygonShape(verts, density);
                    Fixture fix = body.CreateFixture(shp);
                    fix.IsSensor = isTrigger;
                }
                catch (Exception ex)
                {
                    Debug.Log(body.UserData + ". Collider triangle is broken: " + verts[0] + "; " + verts[1] + "; " + verts[2] + ": " + ex.Message);
                }
            }
        }

        internal static void AddMesh(Body body, bool isTrigger, List<Vertices> tris, float density)
        {
            if (world == null)
            {
                throw new InvalidOperationException("You have to Initialize the Physics system before adding bodies");
            }
            List<Fixture> fixes = FixtureFactory.AttachCompoundPolygon(tris, density, body);
            for (int i = 0; i < fixes.Count; i++)
            {
                fixes[i].IsSensor = isTrigger;
            }
        }
        #endregion

        #region Raycast methods
        public static bool Raycast(Vector2 origin, Vector2 direction)
        {
            return Raycast(origin, direction, Mathf.Infinity, kDefaultRaycastLayers);
        }

        public static bool Raycast(Vector2 origin, Vector2 direction, float distance)
        {
            return Raycast(origin, direction, distance, kDefaultRaycastLayers);
        }

        public static bool Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask)
        {
#if DEBUG
            Application.raycastTimer.Start();
#endif
            RaycastHelper.SetValues(distance, true, layerMask);

            Vector2 pt2 = origin + (direction * distance);
            if (pt2 == origin)
            {
                return false;
            }
            try
            {
                world.RayCast(null, origin, pt2);
            }
            catch (InvalidOperationException)
            {
                Debug.Log("RAYCAST THREW InvalidOperationException");
                return false;
            }
            finally
            {
#if DEBUG
                Application.raycastTimer.Stop();
#endif
            }
            return (RaycastHelper.HitCount > 0);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction)
        {
            return Raycast(origin, direction, Mathf.Infinity, kDefaultRaycastLayers);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, float distance)
        {
            return Raycast(origin, direction, distance, kDefaultRaycastLayers);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
        {
            return Raycast((Vector2)origin, (Vector2)direction, distance, layerMask);
        }

        public static bool Raycast(Vector2 origin, Vector2 direction, out RaycastHit hitInfo, float distance, int layerMask)
        {
#if DEBUG
            Application.raycastTimer.Start();
#endif
            RaycastHelper.SetValues(distance, true, layerMask);

            Vector2 pt2 = origin + (direction * distance);
            if (pt2 == origin)
            {
                hitInfo = new RaycastHit();
                return false;
            }
            try
            {
                world.RayCast(null, origin, pt2);
                hitInfo = RaycastHelper.ClosestHit();
            }
            catch (InvalidOperationException)
            {
                hitInfo = new RaycastHit();
                Debug.Log("RAYCAST THREW InvalidOperationException");
                return false;
            }
            finally
            {
#if DEBUG
                Application.raycastTimer.Stop();
#endif
            }
            return (RaycastHelper.HitCount > 0);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo)
        {
            return Raycast(origin, direction, out hitInfo, Mathf.Infinity, kDefaultRaycastLayers);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float distance)
        {
            return Raycast(origin, direction, out hitInfo, distance, kDefaultRaycastLayers);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float distance, int layerMask)
        {
            return Raycast((Vector2)origin, (Vector2)direction, out hitInfo, distance, layerMask);
        }

        public static bool Raycast(Ray ray)
        {
            return Raycast(ray.origin, ray.direction, Mathf.Infinity, kDefaultRaycastLayers);
        }

        public static bool Raycast(Ray ray, float distance)
        {
            return Raycast(ray.origin, ray.direction, distance, kDefaultRaycastLayers);
        }

        public static bool Raycast(Ray ray, float distance, int layerMask)
        {
            return Raycast(ray.origin, ray.direction, distance, layerMask);
        }

        public static bool Raycast(Ray ray, out RaycastHit hitInfo)
        {
            return Raycast(ray.origin, ray.direction, out hitInfo, Mathf.Infinity, kDefaultRaycastLayers);
        }

        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float distance)
        {
            return Raycast(ray.origin, ray.direction, out hitInfo, distance, kDefaultRaycastLayers);
        }

        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float distance, int layerMask)
        {
            return Raycast(ray.origin, ray.direction, out hitInfo, distance, layerMask);
        }

        public static RaycastHit[] RaycastAll(Vector2 origin, Vector2 direction)
        {
            return RaycastAll(origin, direction, Mathf.Infinity, kDefaultRaycastLayers);
        }

        public static RaycastHit[] RaycastAll(Vector2 origin, Vector2 direction, float distance)
        {
            return RaycastAll(origin, direction, distance, kDefaultRaycastLayers);
        }

        public static RaycastHit[] RaycastAll(Vector2 origin, Vector2 direction, float distance, int layerMask)
        {
#if DEBUG
            Application.raycastTimer.Start();
#endif

            RaycastHelper.SetValues(distance, false, layerMask);

            Vector2 pt2 = origin + (direction * distance);
            if (pt2 == origin)
            {
                return new RaycastHit[0];
            }
            world.RayCast(null, origin, pt2);
#if DEBUG
            Application.raycastTimer.Stop();
#endif
            return RaycastHelper.Hits;
        }

        public static RaycastHit[] RaycastFromTo(Vector2 from, Vector2 to, int layerMask)
        {
#if DEBUG
            Application.raycastTimer.Start();
#endif
            if (from == to)
            {
                #if DEBUG
                Application.raycastTimer.Stop();
                #endif
                return new RaycastHit[0];
            }
            RaycastHelper.SetValues(100f, false, layerMask);

            world.RayCast(null, from, to);
#if DEBUG
            Application.raycastTimer.Stop();
#endif
            return RaycastHelper.HitsByDistance;
        }

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction)
        {
            return RaycastAll(origin, direction, Mathf.Infinity, kDefaultRaycastLayers);
        }

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float distance)
        {
            return RaycastAll(origin, direction, distance, kDefaultRaycastLayers);
        }

        public static RaycastHit[] RaycastAll(Ray ray, float distance, int layerMask)
        {
            return RaycastAll(ray.origin, ray.direction, distance, layerMask);
        }

        internal static bool Raycast(Body body, Ray ray, out RaycastHit hitInfo, float distance)
        {
            RayCastOutput output;
            RayCastInput input = new RayCastInput() { Point1 = ray.origin, Point2 = ray.origin + ray.direction, MaxFraction = distance };
            hitInfo = new RaycastHit() { body = body };
            for (int i = 0; i < body.FixtureList.Count; i++)
            {
                if (body.FixtureList[i].RayCast(out output, ref input, 0))
                {
                    hitInfo.collider = body.UserData;
                    hitInfo.transform = body.UserData.transform;
                    hitInfo.normal = VectorConverter.Convert(output.Normal, body.UserData.to2dMode);
                    hitInfo.distance = output.Fraction;
                    hitInfo.point = ray.GetPoint(output.Fraction);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Pointcast methods
        public static bool Pointcast(Vector2 point)
        {
            return Pointcast(point, kDefaultRaycastLayers);
        }

        public static bool Pointcast(Vector2 point, int layerMask)
        {
            RaycastHit hit;
            return Pointcast(point, out hit, layerMask);
        }

        public static bool Pointcast(Vector2 point, out RaycastHit hitInfo)
        {
            return Pointcast(point, out hitInfo, kDefaultRaycastLayers);
        }

        public static bool Pointcast(Vector2 point, out RaycastHit hitInfo, int layerMask)
        {
#if DEBUG
            Application.raycastTimer.Start();
#endif
            Fixture f = world.TestPointActive(point, layerMask);
            hitInfo = new RaycastHit();            
            if (f != null)
            {
                hitInfo.body = f.Body;
                hitInfo.collider = f.Body.UserData;
                hitInfo.transform = f.Body.UserData.transform;
                hitInfo.point = VectorConverter.Convert(point, f.Body.UserData.to2dMode);
            }
#if DEBUG
            Application.raycastTimer.Stop();
#endif
            return (f != null);
        }

        private static List<Fixture> fList = new List<Fixture>(10);
        public static RaycastHit[] PointcastAll(Vector2 point, int layerMask)
        {
#if DEBUG
            Application.raycastTimer.Start();
#endif
            fList.Clear();
            world.TestPointAllActive(point, fList, layerMask);
            RaycastHit[] hits = new RaycastHit[fList.Count];
            for (int i = 0; i < fList.Count; i++)
            {
                Fixture f = fList[i];
                RaycastHit hitInfo = new RaycastHit();
                hitInfo.body = f.Body;
                hitInfo.collider = f.Body.UserData;
                hitInfo.transform = f.Body.UserData.transform;
                hitInfo.point = VectorConverter.Convert(point, f.Body.UserData.to2dMode);
                hits[i] = hitInfo;
            }
#if DEBUG
            Application.raycastTimer.Stop();
#endif
            return hits;
        }
        #endregion

        #region Linecast methods
        public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask)
        {

#if DEBUG
            Application.raycastTimer.Start();
#endif
            Vector2 pt2 = end;
            Vector2 origin = start;
            RaycastHelper.SetValues((origin - pt2).magnitude, true, layerMask);
            if (pt2 == origin)
            {
                hitInfo = new RaycastHit();
                return false;
            }
            world.RayCast(null, origin, pt2);
            hitInfo = RaycastHelper.ClosestHit();
#if DEBUG
            Application.raycastTimer.Stop();
#endif
            return (RaycastHelper.HitCount > 0);
        }
        public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo)
        {
            return Linecast(start, end, out hitInfo, kDefaultRaycastLayers);
        }
        #endregion

        #region CheckCapsule methods
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius)
        {
            return CheckCapsule(start, end, radius, kDefaultRaycastLayers);
        }

        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, LayerMask layermask)
        {
            //TODO an actual capsule check.. not just a raycasts            
            Vector3 forward = (end - start).normalized;
            Vector3 right = new Vector3(forward.z,0,-forward.x);

            Ray middleRay = new Ray(start, forward);
            Ray rightRay = new Ray(start + right * radius + forward * radius, forward);
            Ray leftRay = new Ray(start - right * radius + forward * radius, forward);

            float middleLength = (end - start).magnitude;
            float sidesLength = middleLength - radius * 2;

            return (Raycast(middleRay, middleLength, layermask) || Raycast(rightRay, sidesLength, layermask) || Raycast(leftRay, sidesLength, layermask));
        }
        #endregion

        #region OverlapSpehere methods
        public static Collider[] OverlapSphere(Vector2 position, float radius)
        {
            return OverlapSphere(position, radius, kDefaultRaycastLayers);
        }

        public static Collider[] OverlapSphere(Vector2 position, float radius, LayerMask layermask)
        {
            AABB aabb = new AABB(position, radius, radius);
            QueryHelper.layermask = layermask;
            world.QueryAABB(null, ref aabb);
            return QueryHelper.GetQueryResult();
        }
        #endregion

        public static void IgnoreCollision(Collider collider1, Collider collider2)
        {
            IgnoreCollision(collider1, collider2, true);
        }

        public static void IgnoreCollision(Collider collider1, Collider collider2, bool ignore)
        {
            if (ignore)
            {
                collider1.connectedBody.IgnoreCollisionWith(collider2.connectedBody);
                RemoveStays(collider1, collider2);
            }
            else
            {
                collider1.connectedBody.RestoreCollisionWith(collider2.connectedBody);
            }
        }

        private static void RemoveStays(Collider collider1, Collider collider2)
        {
            contactProcessor.RemoveStay(collider1, collider2);
        }

        internal static void RemoveStays(Collider collider)
        {
            contactProcessor.ResetStays(collider);
        }

        internal static void AddRigidBody(Body body)
        {
            if (!rigidBodies.Contains(body) && body.UserData != null)
            {
                rigidBodies.Add(body);
            }
        }

        internal static void RemoveRigidBody(Body body)
        {
            if (rigidBodies.Contains(body))
            {
                rigidBodies.Remove(body);
            }
        }

        internal static void RemoveBody(Body body)
        {
            if (body.UserData == null)
            {
                return;
            }
            world.RemoveBody(body);
            body.UserData = null;
            if (rigidBodies.Contains(body))
            {
                rigidBodies.Remove(body);
            }
        }

        public static bool SphereCast(Ray ray, float radius, float distance, LayerMask layerMask)
        {
            AABB aabb = new AABB(ray.GetPoint(distance / 2), distance, radius);
            QueryHelper.layermask = layerMask;
            QueryHelper.breakOnFirst = true;
            world.QueryAABB(null, ref aabb);
            return QueryHelper.GetQueryResult().Length > 0;
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo)
        {
            return SphereCast(origin, radius, direction, out hitInfo, Mathf.Infinity, kDefaultRaycastLayers);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float distance)
        {
            return SphereCast(origin, radius, direction, out hitInfo, distance, kDefaultRaycastLayers);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float distance, int layerMask)
        {
            Ray ray = new Ray(origin, direction);
            AABB aabb = new AABB(ray.GetPoint(distance / 2), distance, radius);
            QueryHelper.layermask = layerMask;
            QueryHelper.breakOnFirst = true;
            world.QueryAABB(null, ref aabb);

            if (QueryHelper.GetQueryResult().Length > 0)
            {
                hitInfo = QueryHelper.ClosestHit();    
                return true;
            }
            hitInfo = new RaycastHit();
            return false;
        }

        internal static void Reset()
        {
            Initialize();
        }

        public static void DrawDebug()
        {
          List<Body> Bodies = world.BodyList;

          for(int i = 0; i < Bodies.Count; ++i)
          {
            FarseerPhysics.Common.Transform Transf;
            Bodies[i].GetTransform(out Transf);
            List<Fixture> Fixtures = Bodies[i].FixtureList;
            
            for(int j = 0; j < Fixtures.Count; ++j)
            {
              FixtureProxy[] Proxies = Fixtures[j].Proxies;

              for(int k = 0; k < Proxies.Length; ++k)
              {
                Microsoft.Xna.Framework.Vector2[] Vertices = Proxies[k].AABB.GetVertices();

                for(int l = 0; l < Vertices.Length; ++l)
                {
                  Vector3 vOriginBot = new Vector3(Vertices[l].X,  0.0f, Vertices[l].Y);
                  Vector3 vOriginTop = new Vector3(Vertices[l].X,  9.0f, Vertices[l].Y);
                  int     iNext   = (l + 1) % Vertices.Length;
                  Vector3 vEndBot = new Vector3(Vertices[iNext].X, 0.0f, Vertices[iNext].Y);
                  Vector3 vEndTop = new Vector3(Vertices[iNext].X, 9.0f, Vertices[iNext].Y);
                  
                  Debug.DrawLine(vOriginBot, vEndBot,    Color.red);
                  Debug.DrawLine(vOriginTop, vEndTop,    Color.red);
                  Debug.DrawLine(vOriginBot, vOriginTop, Color.red);
                }
              }
            }
          }
        }

        internal static void MoveCollider(Collider coll)
        {
            Body body = coll.connectedBody;
            BodyType bodyType = body.BodyType;
            Transform t = coll.transform;
            if ((bodyType == BodyType.Kinematic) || (ApplicationSettings.Physics_MoveStaticColliders && bodyType == BodyType.Static))
            {
                if (((t.changes & TransformChanges.Position) == TransformChanges.Position) || ((t.changes & TransformChanges.Rotation) == TransformChanges.Rotation))
                {
                    float rad = MathHelper.ToRadians(VectorConverter.Angle(t.eulerAngles, coll.to2dMode));
                    Microsoft.Xna.Framework.Vector2 pos = VectorConverter.Convert(t.position, coll.to2dMode);
#if DEBUG
                    Debug.LogIf(DebugSettings.LogColliderChanges, String.Format("Move {0} to {1} and rotate to {2}", coll, pos, rad));
#endif
                    body.SetTransformIgnoreContacts(ref pos, rad);
                }
                if ((t.changes & TransformChanges.Scale) == TransformChanges.Scale)
                {
#if DEBUG
                    Debug.LogIf(DebugSettings.LogColliderChanges, String.Format("Resize collider {0}", coll));
#endif
                    coll.ResizeConnectedBody();
                }
            }
        }
    }
}
