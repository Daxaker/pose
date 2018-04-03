using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

using Pose.Exceptions;
using Pose.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static System.Console;

namespace Pose.Tests
{
    [TestClass]
    public class ShimTests
    {
        interface IInterface
        {
            int Foo();
        }

        class MyClass : IInterface
        {
            int IInterface.Foo() => 0;
        }

        abstract class AbstractClass : IInterface
        {
            public abstract int Foo();
            public virtual int VrtFoo() => 0;
        }

        class AbstractImplementation : AbstractClass
        {
            public override int Foo()
            {
                return 0;
            }
        }
        
        class AbstractVirtualImplementation : AbstractClass
        {
            public override int Foo()
            {
                return 0;
            }

            public override int VrtFoo()
            {
                return 1;
            }
        }

        [TestMethod]
        public void TestAreEqual()
        {
            Shim shim = Shim.Replace(() => Is.A<IInterface>().Foo()).With((IInterface t) => 42);
            var r =  new MyClass();
            PoseContext.Isolate(() => { Assert.AreEqual(42, ((IInterface) r).Foo()); }, shim);
            
        }        

        [TestMethod]
        public void TestExplicit()
        {
            Shim shim = Shim.Replace(() => Is.A<IInterface>().Foo()).With((IInterface t) => 42);
            var r =  new MyClass();
            var res = 1;
            PoseContext.Isolate(() => { res = ((IInterface)r).Foo(); }, shim);
            Assert.AreEqual(42, res);
        }

        [TestMethod]
        public void TestExplicitViaAbstract()
        {
            Shim shim = Shim.Replace(() => Is.A<AbstractClass>().Foo()).With((AbstractClass t) => 42);
            var r =  new AbstractImplementation();
            var res = 1;
            PoseContext.Isolate(() => { res = r.Foo(); }, shim);
            Assert.AreEqual(42, res);
        }
        
        [TestMethod]
        public void TestVirtualInstanceReplace()
        {
            var inst = new AbstractVirtualImplementation();
            Shim shim = Shim.Replace(() => inst.VrtFoo()).With((AbstractVirtualImplementation t) => 42);
            var res = 1;
            PoseContext.Isolate(() => { res = inst.VrtFoo(); }, shim);
            Assert.AreEqual(42, res);
        }
        
        [TestMethod]
        public void TestVirtual()
        {
            var inst = new AbstractVirtualImplementation();
            Shim shim = Shim.Replace(() => Is.A<AbstractClass>().VrtFoo()).With((AbstractClass t) => 42);
            var res = 1;
            PoseContext.Isolate(() => { res = inst.VrtFoo(); }, shim);
            Assert.AreEqual(42, res);
        }

        [TestMethod]
        public void TestReplace()
        {
            Shim shim = Shim.Replace(() => Console.WriteLine(""));

            Assert.AreEqual(typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }), shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestReplaceWithInstanceVariable()
        {
            ShimTests shimTests = new ShimTests();
            Shim shim = Shim.Replace(() => shimTests.TestReplace());

            Assert.AreEqual(typeof(ShimTests).GetMethod("TestReplace"), shim.Original);
            Assert.AreSame(shimTests, shim.Instance);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestShimReplaceWithInvalidSignature()
        {
            ShimTests shimTests = new ShimTests();
            Shim shim = Shim.Replace(() => shimTests.TestReplace());
            Assert.ThrowsException<InvalidShimSignatureException>(
                () => Shim.Replace(() => shimTests.TestReplace()).With(() => { }));
            Assert.ThrowsException<InvalidShimSignatureException>(
                () => Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(() => { }));
        }

        [TestMethod]
        public void TestShimReplaceWith()
        {
            ShimTests shimTests = new ShimTests();
            Action action = new Action(() => { });
            Action<ShimTests> actionInstance = new Action<ShimTests>((s) => { });

            Shim shim = Shim.Replace(() => Console.WriteLine()).With(action);
            Shim shim1 = Shim.Replace(() => shimTests.TestReplace()).With(actionInstance);

            Assert.AreEqual(typeof(Console).GetMethod("WriteLine", Type.EmptyTypes), shim.Original);
            Assert.AreEqual(action, shim.Replacement);

            Assert.AreEqual(typeof(ShimTests).GetMethod("TestReplace"), shim1.Original);
            Assert.AreSame(shimTests, shim1.Instance);
            Assert.AreEqual(actionInstance, shim1.Replacement);
        }

        [TestMethod]
        public void TestReplacePropertyGetter()
        {
            Shim shim = Shim.Replace(() => Thread.CurrentThread.CurrentCulture);

            Assert.AreEqual(typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).GetMethod, shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestReplacePropertySetter()
        {
            Shim shim = Shim.Replace(() => Is.A<Thread>().CurrentCulture, true);

            Assert.AreEqual(typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).SetMethod, shim.Original);
            Assert.IsNull(shim.Replacement);
        }
        
        
        [TestMethod]
        public void TestReplacePropertySetterAction()
        {
            var getterExecuted = false;
            var getterShim = Shim.Replace(() => Is.A<Thread>().CurrentCulture)
                .With((Thread t) =>
                {
                    getterExecuted = true;
                    return t.CurrentCulture;
                });
            var setterExecuted = false;
            var setterShim = Shim.Replace(() => Is.A<Thread>().CurrentCulture, true)
                .With((Thread t, CultureInfo value) =>
                {
                    setterExecuted = true;
                    t.CurrentCulture = value;
                });

            var currentCultureProperty = typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo));
            Assert.AreEqual(currentCultureProperty.GetMethod, getterShim.Original);
            Assert.AreEqual(currentCultureProperty.SetMethod, setterShim.Original);

            PoseContext.Isolate(() =>
            {
                var oldCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }, getterShim, setterShim);

            Assert.IsTrue(getterExecuted, "Getter not executed");
            Assert.IsTrue(setterExecuted, "Setter not executed");
        }
    }
}
