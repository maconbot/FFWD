﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PressPlay.FFWD.Components;

namespace PressPlay.FFWD.Test.Core_framework
{
    [TestFixture]
    public class WhenUsingTheGameObjectShortcutProperties
    {
        [Test]
        public void WeWillGetTheRigidBodyIfItIsThere()
        {
            GameObject go = new GameObject();

            Assert.That(go.rigidbody, Is.Null);

            Rigidbody body = new Rigidbody();
            go.AddComponent(body);

            Assert.That(go.rigidbody, Is.Not.Null);
            Assert.That(go.rigidbody, Is.SameAs(body));
        }

        [Test]
        public void WeWillGetTheCorrectRigidbodyAfterAnInstantiation()
        {
            GameObject go = new GameObject();
            Rigidbody body = new Rigidbody();
            go.AddComponent(body);
            Assert.That(go.rigidbody, Is.Not.Null);

            GameObject inst = (GameObject)GameObject.Instantiate(go);
            Assert.That(inst.rigidbody, Is.Not.Null);
            Assert.That(inst.rigidbody, Is.Not.SameAs(body));
        }

        [Test]
        public void WeWillGetTheColliderIfItIsThere()
        {
            GameObject go = new GameObject();

            Assert.That(go.collider, Is.Null);

            Collider body = new BoxCollider();
            go.AddComponent(body);

            Assert.That(go.collider, Is.Not.Null);
            Assert.That(go.collider, Is.SameAs(body));
        }

        [Test]
        public void WeWillGetTheCorrectColliderAfterAnInstantiation()
        {
            GameObject go = new GameObject();
            Collider body = new BoxCollider();
            go.AddComponent(body);
            Assert.That(go.collider, Is.Not.Null);

            GameObject inst = (GameObject)GameObject.Instantiate(go);
            Assert.That(inst.collider, Is.Not.Null);
            Assert.That(inst.collider, Is.Not.SameAs(body));
        }

        [Test]
        public void WeWillGetTheRendererIfItIsThere()
        {
            GameObject go = new GameObject();

            Assert.That(go.renderer, Is.Null);

            Renderer body = new MeshRenderer();
            go.AddComponent(body);

            Assert.That(go.renderer, Is.Not.Null);
            Assert.That(go.renderer, Is.SameAs(body));
        }

        [Test]
        public void WeWillGetTheCorrectRendererAfterAnInstantiation()
        {
            GameObject go = new GameObject();
            Renderer body = new MeshRenderer();
            go.AddComponent(body);
            Assert.That(go.renderer, Is.Not.Null);

            GameObject inst = (GameObject)GameObject.Instantiate(go);
            Assert.That(inst.renderer, Is.Not.Null);
            Assert.That(inst.renderer, Is.Not.SameAs(body));
        }
	
    }
}
