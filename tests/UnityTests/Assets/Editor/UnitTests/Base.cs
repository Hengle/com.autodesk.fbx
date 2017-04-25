// ***********************************************************************
// Copyright (c) 2017 Unity Technologies. All rights reserved.
//
// Licensed under the ##LICENSENAME##.
// See LICENSE.md file in the project root for full license information.
// ***********************************************************************
using NUnit.Framework;
using FbxSdk;

using System.Collections.Generic;

namespace UnitTests
{
    public abstract class Base<T> where T: FbxSdk.FbxObject
    {
        static Base() {
            s_createFromMgrAndName = typeof(T).GetMethod("Create", new System.Type[] {typeof(FbxManager), typeof(string)});
            s_createFromObjAndName = typeof(T).GetMethod("Create", new System.Type[] {typeof(FbxObject), typeof(string)});

#if ENABLE_COVERAGE_TEST
            // Register the calls we make through reflection.
            // We use reflection in CreateObject(FbxManager, string) and CreateObject(FbxObject, string).
            if (s_createFromMgrAndName != null) {
                var createFromMgrAndName = typeof(Base<T>).GetMethod("CreateObject", new System.Type[] {typeof(FbxManager), typeof(string)});
                CoverageTester.RegisterReflectionCall(createFromMgrAndName, s_createFromMgrAndName);
            }
            if (s_createFromObjAndName != null) {
                var createFromObjAndName = typeof(Base<T>).GetMethod("CreateObject", new System.Type[] {typeof(FbxObject), typeof(string)});
                CoverageTester.RegisterReflectionCall(createFromObjAndName, s_createFromObjAndName);
            }
#endif
        }

        protected FbxManager Manager {
            get;
            private set;
        }

        /* Create an object with the default manager. */
        public T CreateObject (string name = "") {
            return CreateObject(Manager, name);
        }

#if ENABLE_COVERAGE_TEST
        [Test]
        public void TestCoverage() { CoverageTester.TestCoverage(typeof(T), this.GetType()); }
#endif

        /* Create an object with another manager. Default implementation uses
         * reflection to call T.Create(...); override if reflection is wrong. */
        static System.Reflection.MethodInfo s_createFromMgrAndName;
        public virtual T CreateObject (FbxManager mgr, string name = "") {
            try {
                return (T)(s_createFromMgrAndName.Invoke(null, new object[] {mgr, name}));
            } catch(System.Reflection.TargetInvocationException xcp) {
                throw xcp.GetBaseException();
            }
        }

        /* Create an object with an object as container. Default implementation uses
         * reflection to call T.Create(...); override if reflection is wrong. */
        static System.Reflection.MethodInfo s_createFromObjAndName;
        public virtual T CreateObject (FbxObject container, string name = "") {
            try {
                return (T)(s_createFromObjAndName.Invoke(null, new object[] {container, name}));
            } catch(System.Reflection.TargetInvocationException xcp) {
                throw xcp.GetBaseException();
            }
        }

        [SetUp]
        public virtual void Init ()
        {
            Manager = FbxManager.Create ();
        }

        [TearDown]
        public virtual void Term ()
        {
            try {
                Manager.Destroy ();
            }
            catch (System.ArgumentNullException) {
            }
        }

        [Test]
        public void TestCreate()
        {
            var obj = CreateObject("MyObject");
            Assert.IsInstanceOf<T> (obj);
            Assert.AreEqual(Manager, obj.GetFbxManager());

            using(var manager2 = FbxManager.Create()) {
                var obj2 = CreateObject(manager2, "MyOtherObject");
                Assert.AreEqual(manager2, obj2.GetFbxManager());
                Assert.AreNotEqual(Manager, obj2.GetFbxManager());
            }

            var obj3 = CreateObject(obj, "MySubObject");
            Assert.AreEqual(Manager, obj3.GetFbxManager());

            // Test with a null manager or container. Should throw.
            Assert.That (() => { CreateObject((FbxManager)null, "MyObject"); }, Throws.Exception.TypeOf<System.NullReferenceException>());
            Assert.That (() => { CreateObject((FbxObject)null, "MyObject"); }, Throws.Exception.TypeOf<System.NullReferenceException>());

            // Test with a null string. Should work.
            Assert.IsNotNull(CreateObject((string)null));

            // Test with a destroyed manager. Should throw.
            var mgr = FbxManager.Create();
            mgr.Destroy();
            Assert.That (() => { CreateObject(mgr, "MyObject"); }, Throws.Exception.TypeOf<System.ArgumentNullException>());

            // Test with a disposed manager. Should throw.
            mgr = FbxManager.Create();
            mgr.Dispose();
            Assert.That (() => { CreateObject(mgr, "MyObject"); }, Throws.Exception.TypeOf<System.NullReferenceException>());
        }

        [Test]
        public void TestDestroySelf ()
        {
            // We can call destroy with no args, or with a 'false' argument.
            // Test both.
            var obj = CreateObject ();

            Assert.IsNotNull (obj);
            Assert.IsInstanceOf<FbxObject> (obj);
            obj.Destroy ();

            var obj2 = CreateObject ();
            obj2.Destroy (false);
        }

        [Test]
        public void TestDestroyRecursive ()
        {
            var obj = CreateObject ();

            Assert.IsNotNull (obj);
            Assert.IsInstanceOf<FbxObject> (obj);
            obj.Destroy (true);
        }

        [Test]
        public void TestUsing ()
        {
            // Test that the using statement works.
            using (var obj = CreateObject ()) {
                obj.GetName ();
            }

            // Test also that an explicit Dispose works.
            var obj2 = CreateObject();
            obj2.Dispose();
        }

        [Test]
        public void TestDestroyedZombie ()
        {
            // Test that if we try to use an object after Destroy()ing it,
            // we get an exception (not a crash).
            var obj = CreateObject();
            Assert.IsNotNull (obj);
            obj.Destroy ();
            Assert.That (() => { obj.GetName (); }, Throws.Exception.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void TestDestroyedManagerZombie ()
        {
            // Test that if we try to use an object after Destroy()ing its
            // manager, the object was destroyed as well.
            var obj = CreateObject();
            Assert.IsNotNull (obj);
            Manager.Destroy();
            Assert.That (() => { obj.GetName (); }, Throws.Exception.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void TestDisposedZombie ()
        {
            // Test that if we try to use an object after Dispose()ing it,
            // we get an exception (not a crash). This is a regression test
            // based on some wrong code:
            T zombie;
            using(var obj = CreateObject()) {
                Assert.IsNotNull (obj);
                zombie = obj;
            }
            Assert.That (() => { zombie.GetName (); }, Throws.Exception.TypeOf<System.NullReferenceException>());
        }

        [Test]
        public void TestSelected ()
        {
            var obj = CreateObject ();
            Assert.IsNotNull (obj);

            Assert.IsFalse (obj.GetSelected ());
            obj.SetSelected (true);
            Assert.IsTrue (obj.GetSelected ());
        }

        [Test]
        public void TestNames ()
        {
            /*
             * We use this also for testing that string handling works.
             * Make sure we can pass const char*, FbxString, and const
             * FbxString&.
             * Make sure we can return those too (though I'm not actually
             * seeing a return of a const-ref anywhere).
             */

            // Test a function that takes const char*.
            FbxObject obj = FbxObject.Create(Manager, "MyObject");
            Assert.IsNotNull (obj);

            // Test a function that returns const char*.
            Assert.AreEqual ("MyObject", obj.GetName ());

            // Test a function that takes an FbxString with an accent in it.
            obj.SetNameSpace("Accentué");

            // Test a function that returns FbxString.
            Assert.AreEqual ("MyObject", obj.GetNameWithoutNameSpacePrefix ());

            // Test a function that returns FbxString with an accent in it.
            Assert.AreEqual ("Accentué", obj.GetNameSpaceOnly());

            // Test a function that takes a const char* and returns an FbxString.
            // We don't want to convert the other StripPrefix functions, which
            // modify their argument in-place.
            Assert.AreEqual("MyObject", FbxObject.StripPrefix("NameSpace::MyObject"));

            obj.Destroy();
        }
    }
}
