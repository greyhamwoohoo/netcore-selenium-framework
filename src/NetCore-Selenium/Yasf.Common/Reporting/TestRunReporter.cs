﻿using Serilog;
using System;
using Yasf.Common.Reporting.Contracts;

namespace Yasf.Common.Reporting
{
    public class TestRunReporter : ITestRunReporter
    {
        public string RootOutputFolder { get; }
        public string TestRunIdentity { get; }
        public string ReportOutputFolder => System.IO.Path.Combine(RootOutputFolder, TestRunIdentity);
        private bool _initialized { get; set; }
        private ILogger _logger { get; }


        public TestRunReporter(ILogger logger, string testOutputFolder, string testRunIdentity)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            RootOutputFolder = testOutputFolder ?? throw new ArgumentNullException(nameof(testOutputFolder));
            TestRunIdentity = testRunIdentity ?? throw new ArgumentNullException(nameof(testRunIdentity));
        }

        public void Initialize()
        {
            _logger.Debug($"Setup: Existing state: {_initialized}");
            if (_initialized) return;

            _initialized = true;

            _logger.Debug($"Setup: New state: {_initialized}");
        }

        public void Uninitialize()
        {
            _logger.Debug($"Teardown: Existing state: {_initialized}");

            if (!_initialized) return;

            _initialized = false;

            _logger.Debug($"Teardown: New state: {_initialized}");
        }

        public ITestCaseReporter CreateTestCaseReporter(ILogger logger, ITestCaseReporterContext testCaseReportingContext)
        {
            if (null == testCaseReportingContext) throw new ArgumentNullException(nameof(testCaseReportingContext));
            if (null == logger) throw new ArgumentNullException(nameof(logger));

            return new TestCaseReporter(logger, testCaseReportingContext);
        }
    }
}
