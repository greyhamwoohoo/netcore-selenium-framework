﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System;
using Yasf.Common.Infrastructure;

namespace Yasf.Common
{
    [TestClass]
    public abstract class TestBase
    {
        protected IServiceScope Scope;
        protected ILogger Logger => Resolve<ILogger>();

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetupTest()
        {
            Scope = ContainerSingleton.Instance.CreateScope();
        }

        [TestCleanup]
        public void TeardownTest()
        {
            Scope?.Dispose();
        }

        protected T Resolve<T>()
        {
            var result = Scope.ServiceProvider.GetService<T>();
            if (result == null) throw new InvalidOperationException($"The type {typeof(T).FullName} is not registered in the Container. Update the ContainerSingleton.cs file. ");

            return result;
        }
    }
}
