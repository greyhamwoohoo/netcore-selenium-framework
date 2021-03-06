using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Idioms;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System.IO;
using System.Linq;

namespace Yasf.Common.UnitTests.ExecutionContext.Runtime
{
    [TestClass]
    public class RuntimeSettings
    {
        [TestClass]
        public class GuardsTests
        {
            [TestMethod]
            public void All_Methods_Have_Guards()
            {
                var fixture = new Fixture();
                fixture.Customize(new AutoNSubstituteCustomization());
                var assertThatAllMembersHaveGuards = new GuardClauseAssertion(fixture);
                assertThatAllMembersHaveGuards.Verify(typeof(Common.ExecutionContext.Runtime.RuntimeSettings).GetMembers());
            }
        }

        [TestClass]
        public class GetPaths
        {
            Common.ExecutionContext.Runtime.RuntimeSettings _utilities = default;
            ILogger _logger = default;

            string SETTINGS_ENVIRONMENT_VARIABLE_NAME = "PREF_SOMETHINGSETTINGS_FILES";

            [TestInitialize]
            public void SetupRuntimeSettings()
            {
                _utilities = new Common.ExecutionContext.Runtime.RuntimeSettings(NSubstitute.Substitute.For<ILogger>());
                _logger = NSubstitute.Substitute.For<ILogger>();
            }

            [TestMethod]
            [DataRow("PREF_", "D:", "SomethingSettings", "the-file.json", @"D:\SomethingSettings\the-file.json", "because by convention the file be rooted and located under the category folder")]
            [DataRow("PREF_", "D:", "SomethingSettings", @"Y:\the-rooted-file.json", @"Y:\the-rooted-file.json", "because explicitly rooted paths are preserved. ")]
            public void Will_Use_Default_Settings_File_When_No_Environment_Variable_Is_Set(string prefix, string rootFolder, string category, string defaultFilename, string expectedResultTemplate, string because)
            {
                // Scenario
                System.Environment.SetEnvironmentVariable(SETTINGS_ENVIRONMENT_VARIABLE_NAME, null);

                // Arrange (as we cannot use calculated expressions in [DataRow])
                var expectedResult = SubstitutePlaceholdersForOsFilesystem(expectedResultTemplate);

                // Act
                var result = _utilities.CalculatePathsOfSettingsFiles(prefix, rootFolder, category, defaultFilename);

                // Assert
                result.Count().Should().Be(1, because: "the convention is to always revert to the single, default settings file. ");
                result.First().Should().Be(expectedResult, because);
            }

            [TestMethod]
            [DataRow("env-file.json", "PREF_", "D:", "SomethingSettings", "the-file.json", new[] { @"D:\SomethingSettings\env-file.json" }, "because relative paths are rooted under the category folder")]
            [DataRow("env-file.json;yeha-file.json", "PREF_", "D:", "SomethingSettings", "the-file.json", new[] { @"D:\SomethingSettings\env-file.json", @"D:\SomethingSettings\yeha-file.json" }, "because multiple relative paths are supported. ")]
            [DataRow(@"env-file.json;Z:\yeha-file.json", "PREF_", "D:", "SomethingSettings", "the-file.json", new[] { @"D:\SomethingSettings\env-file.json", @"Z:\yeha-file.json" }, "because a mixture of relative and rooted paths are supported. ")]
            [DataRow(@"Z:\yeha-file.json", "PREF_", "D:", "SomethingSettings", "the-file.json", new[] { @"Z:\yeha-file.json" }, "because explicitly rooted paths are supported. ")]
            [DataRow(@"env-file.json;Z:\yeha-file.json", "NOTPREFIX_", "E:", "SomethingSettings", "the-file.json", new[] { @"E:\SomethingSettings\the-file.json" }, "because the environment variable set does not match the convention, its values are not used. The default is used instead. ")]
            public void Will_Use_Paths_From_Environment_Variables_When_Set(string envValue, string prefix, string rootFolder, string category, string defaultFilename, string[] expectedResultTemplates, string because)
            {
                // Scenario
                System.Environment.SetEnvironmentVariable(SETTINGS_ENVIRONMENT_VARIABLE_NAME, envValue);

                // Arrange (as we cannot use calculated expressions in [DataRow])
                var expectedResults = expectedResultTemplates.Select(template => SubstitutePlaceholdersForOsFilesystem(template));

                // Act
                var result = _utilities.CalculatePathsOfSettingsFiles(prefix, rootFolder, category, defaultFilename);

                // Assert
                result.Count().Should().Be(expectedResultTemplates.Count(), because: "we expect a certain number of fully qualified settings paths to be returned. ");
                expectedResults.Should().BeEquivalentTo(result, because);
            }

            private string SubstitutePlaceholdersForOsFilesystem(string value)
            {
                return value
                    .Replace(@":", $"{Path.VolumeSeparatorChar}")
                    .Replace(@"\", $"{Path.DirectorySeparatorChar}");
            }
        }
    }
}
