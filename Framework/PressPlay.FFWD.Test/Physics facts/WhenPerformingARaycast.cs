﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Microsoft.Xna.Framework;
using FarseerPhysics.Dynamics;

namespace PressPlay.FFWD.Test.Physics_facts
{
    [TestFixture]
    public class WhenPerformingARaycast
    {
        [SetUp]
        public void Setup()
        {
            Physics.Initialize();
        }
	
        [Test]
        public void WeCanDetermineAColliderHit()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 50), 1);
            bool hit = Physics.Raycast(Vector2.zero, Vector2.up, 100, 0);
            Assert.That(hit, Is.True);
        }

        [Test]
        public void WeCanDetermineAColliderHitUsing3d()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 50), 1);
            bool hit = Physics.Raycast(Vector3.zero, -Vector3.forward, 100, 0);
            Assert.That(hit, Is.True);
        }

        [Test]
        public void WeCanDetermineAColliderHitUsingRay()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 50), 1);
            Ray ray = new Ray(Vector3.zero, -Vector3.forward);
            bool hit = Physics.Raycast(ray, 100, 0);
            Assert.That(hit, Is.True);
        }

        [Test]
        public void WeCanMissAllColliders()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 50), 1);
            bool hit = Physics.Raycast(Vector2.zero, Vector2.right, 100, 0);
            Assert.That(hit, Is.False);
        }

        [Test]
        public void WeCanMissAllCollidersUsing3d()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 50), 1);
            bool hit = Physics.Raycast(Vector2.zero, Vector2.right, 100, 0);
            Assert.That(hit, Is.False);
        }

        [Test]
        public void WeCanMissAllCollidersUsingRay()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 50), 1);
            Ray ray = new Ray(Vector3.zero, Vector3.right);
            bool hit = Physics.Raycast(ray, 100, 0);
            Assert.That(hit, Is.False);
        }

        [Test]
        public void WeWillNotHitAColliderIfTheRayStartsInsideIt()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 0), 1);
            bool hit = Physics.Raycast(Vector2.zero, Vector2.up, 100, 0);
            Assert.That(hit, Is.False);
        }

        [Test]
        public void WeCanGetHitInfoOnTheObjectThatWasHit()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 50), 1);
            RaycastHit hit = new RaycastHit();
            bool hasHit = Physics.Raycast(Vector2.zero, Vector2.up, out hit, 100, 0);
            Assert.That(hasHit, Is.True);
            Assert.That(hit.body, Is.SameAs(body));
            Assert.That(hit.point, Is.EqualTo(new Vector3(0, 0, 45)));
            Assert.That(hit.normal, Is.EqualTo(new Vector3(0, 0, -1)));
            Assert.That(hit.distance, Is.EqualTo(45));
        }

        [Test]
        public void WeWillGetInfOnTheClosestObjectHit()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Body body1 = Physics.AddBody();
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 20), 1);
            Physics.AddBox(body1, false, 10, 10, new Vector2(0, 50), 1);
            RaycastHit hit = new RaycastHit();
            bool hasHit = Physics.Raycast(Vector2.zero, Vector2.up, out hit, 100, 0);
            Assert.That(hasHit, Is.True);
            Assert.That(hit.body, Is.SameAs(body));
        }

        [Test]
        public void WeWillGetHitsOnAllObjectsOnTheRay()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Body body1 = Physics.AddBody();
            Physics.AddBox(body1, false, 10, 10, new Vector2(0, 50), 1);
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 20), 1);
            RaycastHit[] hits = Physics.RaycastAll(Vector2.zero, Vector2.up, 100, 0);
            Assert.That(hits.Length, Is.EqualTo(2));
        }

        [Test]
        public void DoingARaycastWithzeroDistanceWillReturnFalse()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Assert.That(Physics.Raycast(Vector2.zero, Vector2.zero, 0, 0), Is.False);
            Assert.That(Physics.RaycastAll(Vector2.zero, Vector2.zero, 0, 0), Is.Empty);

            RaycastHit hit;
            Physics.Raycast(Vector2.zero, Vector2.zero, out hit, 0, 0);
            Assert.That(hit.body, Is.Null);
        }

        [Test]
        public void WeWillOnlyGetObjectsInDistance()
        {
            Assert.Inconclusive("This test is suspended as we need an active collider on the body to test on");
            Body body = Physics.AddBody();
            Body body1 = Physics.AddBody();
            Physics.AddBox(body1, false, 10, 10, new Vector2(0, 50), 1);
            Physics.AddBox(body, false, 10, 10, new Vector2(0, 20), 1);
            RaycastHit[] hits = Physics.RaycastAll(Vector2.zero, Vector2.up, 30, 0);
            Assert.That(hits.Length, Is.EqualTo(1));
        }
	
    }
}
