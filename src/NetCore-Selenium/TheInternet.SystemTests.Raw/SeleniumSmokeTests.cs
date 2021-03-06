﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yasf.Common;
using Yasf.Common.ExecutionContext.Runtime.BrowserSettings.Contracts;
using Yasf.Common.ExecutionContext.Runtime.ControlSettings;
using Yasf.Common.ExecutionContext.Runtime.InstrumentationSettings;
using Yasf.Common.Reporting.Contracts;

namespace TheInternet.SystemTests.Raw
{
    [TestClass]
    public class SeleniumSmokeTests : SeleniumTestBase
    {
        protected override string BaseUrl => "https://www.google.com";

        [TestMethod]
        public void ItIsChrome()
        {
            Resolve<IBrowserProperties>().Name.Should().BeOneOf("CHROME", "EDGE", "FIREFOX");
        }

        [TestMethod]
        public void DriverSessionExists()
        {
            Logger.Information($"Here I am");
            DriverSession.Should().NotBeNull(because: "the DriverSession is instantiated in the Base Class. ");
            Logger.Information($"Here I am again");

            DriverSession.TestCaseReporter.Name.Should().Be(TestContext.TestName, because: "the test case should have been initialized. ");
        }

        [TestMethod]
        public void InstrumentationSettingsExists()
        {
            Logger.Information($"Here I am - again");
            var instrumentationSettingsExists = Resolve<IInstrumentationSettings>();
            var controlSettings = Resolve<IControlSettings>();
            Logger.Information($"Here I am - yet again");
        }

        [TestMethod]
        public void ResolutionOfSomethings()
        {
            var folder = System.IO.Path.GetDirectoryName(typeof(SeleniumSmokeTests).Assembly.Location);

            Resolve<ITestRunReporter>().RootOutputFolder.Should().StartWith(folder, because: "the test deployment folder is where the tests are deployed. ");
        }
    }
}
