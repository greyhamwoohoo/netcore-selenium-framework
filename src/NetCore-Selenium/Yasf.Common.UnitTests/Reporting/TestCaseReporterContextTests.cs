﻿using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Idioms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yasf.Common.Reporting;

namespace Yasf.Common.UnitTests.Reporting
{
    [TestClass]
    public class TestCaseReporterContextTests
    {
        [TestMethod]
        public void All_Methods_Have_Guards()
        {
            // Arrange
            var fixture = new Fixture();

            fixture.Customize(new AutoNSubstituteCustomization());

            var assertThatAllMembersHaveGuards = new GuardClauseAssertion(fixture);

            // Act, Assert
            assertThatAllMembersHaveGuards.Verify(typeof(TestCaseReporterContext).GetMembers());
        }
    }
}
