﻿using System;
using System.Linq;
using NUnit.Framework;
using Plainion.GraphViz.Modules.CodeInspection.Core;

namespace Plainion.GraphViz.Modules.CodeInspection.Tests
{
    [TestFixture]
    class Inspect_CalledMethods_Tests
    {
        [Test]
        public void CallOfInstanceMethod()
        {
            Verify(typeof(Caller), nameof(Caller.Main), typeof(Callee), nameof(Callee.InstanceMethod));
        }

        [Test]
        public void CallOfStaticMethod()
        {
            Verify(typeof(Caller), nameof(Caller.Main), typeof(Callee), nameof(Callee.StaticMethod));
        }

        [Test]
        public void CallOfBaseClassMethod()
        {
            Verify(typeof(Caller), nameof(Caller.Main), typeof(CalleeBase), nameof(Callee.BaseMethod));
        }

        [Test]
        public void CallOfInterfaceMethod()
        {
            Verify(typeof(Caller), nameof(Caller.Action), typeof(ICallee), nameof(ICallee.InterfaceMethod));
        }

        [Test]
        public void CallOfVirtualMethod()
        {
            Verify(typeof(Caller), nameof(Caller.Main), typeof(Callee), nameof(Callee.VirtualMethod));
        }

        [Test]
        public void CallOfExtensionMethod()
        {
            Verify(typeof(Caller), nameof(Caller.Main), typeof(CalleeExtensions), nameof(CalleeExtensions.ExtensionMethod));
        }

        [Test]
        public void CallOfProperty()
        {
            Verify(typeof(Caller), nameof(Caller.SpecialMethods), typeof(Callee), "get_Property");
            Verify(typeof(Caller), nameof(Caller.SpecialMethods), typeof(Callee), "set_Property");
        }

        [Test]
        public void CallOfIndexer()
        {
            Verify(typeof(Caller), nameof(Caller.SpecialMethods), typeof(Callee), "get_Item");
            Verify(typeof(Caller), nameof(Caller.SpecialMethods), typeof(Callee), "set_Item");
        }

        [Test]
        public void CallOfEvent()
        {
            Verify(typeof(Caller), nameof(Caller.Main), typeof(CalleeExtensions), nameof(CalleeExtensions.ExtensionMethod));
        }

        private void Verify(Type callerType, string callerMethod, Type callingType, string callingMethod)
        {
            var reflector = new Inspector(new MonoLoader(), callerType);

            var calls = reflector.GetCalledMethods();

            var types = calls
                .Where(call => call.From.Name == callerMethod)
                .Select(call => call.To)
                .Distinct();

            var callee = new Method(callingType, callingMethod);
            Assert.That(types, Contains.Item(callee));
        }
    }

    class Caller
    {
        public void Main()
        {
            new Callee().InstanceMethod();
            Callee.StaticMethod();
            new Callee().BaseMethod();
            new Callee().VirtualMethod();
            new Callee().ExtensionMethod();
        }

        public void Action(ICallee callee)
        {
            callee.InterfaceMethod();
        }

        public void SpecialMethods()
        {
            var callee = new Callee();
            if (callee.Property == 42)
            {
                callee.Property = 42 * 2;
            }

            callee["a"] = callee["b"];
            callee.Event += (s, e) => { };
        }
    }

    interface ICallee
    {
        void InterfaceMethod();
    }

    class CalleeBase
    {
        public void BaseMethod()
        {
        }
    }

    class Callee : CalleeBase, ICallee
    {
        public void InstanceMethod()
        {
        }

        public static void StaticMethod()
        {
        }

        public void InterfaceMethod()
        {
        }

        public virtual void VirtualMethod()
        {
        }

        public int Property { get; set; }

        public string this[string key]
        {
            get { return null; }
            set { Event(this, EventArgs.Empty); }
        }

        public event EventHandler Event;
    }

    static class CalleeExtensions
    {
        public static void ExtensionMethod(this Callee self)
        {
        }
    }
}