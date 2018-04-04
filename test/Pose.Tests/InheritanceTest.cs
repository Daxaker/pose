using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pose.Tests
{
    interface IInterface
    {
        int IntMethod(int intParam);
    }

    interface IGenericInterface<T>
    {
        T MethodT(T param);
    }
    
    abstract class Abstract
    {
        public abstract int MethodA();
        public virtual int MethodV() => 0;
    }

    class SimpleClass
    {
        public int Method() => 0;
    }

    class InheritedClass : Abstract
    {
        public override int MethodA() => 0;
        public override int MethodV() => 1;
    }
    
    class PartialInheritedClass : Abstract
    {
        public override int MethodA() => 0;
    }

    class InterfaceImplementationClass : IInterface
    {
        public int IntMethod(int intParam) => intParam - intParam;
    }
    
    class ExplicitInterfaceImplementationClass : IInterface
    {
        int IInterface.IntMethod(int intParam) => intParam - intParam;
    }

    class ExplicitInterfaceImplemetationInBase : ExplicitInterfaceImplementationClass
    {
    }

    class GenericInterfaceImplementationClass<T> : IGenericInterface<T>
    {
        public T MethodT(T param) => param;
    }
    
    [TestClass]
    public class InheritanceTest
    {
        [TestMethod]
        public void ExplicitImplimentationInBaseClass()
        {
            var irrelevant = new SimpleClass();
            var instance = new ExplicitInterfaceImplemetationInBase();
            var shim = Shim.Replace(() => irrelevant.Method()).With((SimpleClass i) => 42);
            PoseContext.Isolate(() => ((IInterface)instance).IntMethod(0), shim);

        }
        [TestMethod]
        public void ShimInstance()
        {
            var instance = new SimpleClass();
            var shim = Shim.Replace(() => instance.Method()).With((SimpleClass i) => 42);
            PoseContext.Isolate(() => Assert.AreEqual(instance.Method(), 42), shim);
        }
        
        [TestMethod]
        public void ShimInterface()
        {
            var instance = new InterfaceImplementationClass();
            var shim = Shim.Replace(() => Is.A<IInterface>().IntMethod(Is.A<int>())).With((IInterface i, int p) => 42);
            PoseContext.Isolate(() => Assert.AreEqual(instance.IntMethod(0), 42), shim);
        }
        
        [TestMethod]
        public void ShimInstanceOfInterface()
        {
            var instance = new InterfaceImplementationClass();
            var shim = Shim.Replace(() => instance.IntMethod(Is.A<int>())).With((InterfaceImplementationClass i, int p) => 42);
            PoseContext.Isolate(() => Assert.AreEqual(instance.IntMethod(0), 42), shim);
        }
        
        [TestMethod]
        public void ShimVirtual()
        {
            var nonShimedInstance = new InheritedClass();
            var instance = new PartialInheritedClass();
            var shim = Shim.Replace(() => Is.A<PartialInheritedClass>().MethodV()).With((PartialInheritedClass i) => 42);
            PoseContext.Isolate(() =>
            {
                Assert.AreEqual(instance.MethodV(), 42);
                Assert.AreEqual(nonShimedInstance.MethodV(), 1);
            }, shim);
        }
        
        [TestMethod]
        public void ShimExplicitInterface()
        {
            IInterface instance = new ExplicitInterfaceImplementationClass();
            var shim = Shim.Replace(() => Is.A<IInterface>().IntMethod(Is.A<int>())).With((IInterface i, int p) => 42);
            PoseContext.Isolate(() => Assert.AreEqual(instance.IntMethod(0), 42), shim);
        }
        
        [TestMethod]
        public void ShimGenericInterface()
        {
            var instance = new GenericInterfaceImplementationClass<int>();
            var shim = Shim.Replace(() => Is.A<IGenericInterface<int>>().MethodT(Is.A<int>())).With((IGenericInterface<int> i, int p) => 42);
            PoseContext.Isolate(() => Assert.AreEqual(instance.MethodT(0), 42), shim);
        }
    }
}