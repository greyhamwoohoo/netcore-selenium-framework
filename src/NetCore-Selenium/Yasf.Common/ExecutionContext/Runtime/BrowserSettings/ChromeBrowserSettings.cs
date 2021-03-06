﻿using Newtonsoft.Json;
using OpenQA.Selenium;
using System.Collections.Generic;
using System.Linq;

namespace Yasf.Common.ExecutionContext.Runtime.BrowserSettings
{
    public class ChromeBrowserSettings : BrowserSettingsBase
    {
        [JsonProperty("options")]
        public ChromeBrowserParametersOptions Options { get; set; }

        public ChromeBrowserSettings()
        {
            Cleanse();
        }

        internal void Cleanse()
        {
            if (Options == null) Options = new ChromeBrowserParametersOptions();

            Options.Cleanse();
        }

        public class ChromeBrowserParametersOptions
        {
            [JsonProperty("arguments")]
            public List<string> Arguments { get; set; }
            [JsonProperty("loggingPreferences")]
            public Dictionary<string, LogLevel> LoggingPreferences { get; set; }
            [JsonProperty("userProfilePreferences")]
            public Dictionary<string, object> UserProfilePreferences { get; set; }
            [JsonProperty("performanceLoggingPreferences")]
            public LoggingPreferencesPerformance PerformanceLoggingPreferences { get; set; }
            [JsonProperty("captureChromeLogFile")]
            public bool CaptureChromeLogFile { get; set; }
            [JsonProperty("chromeLogPath")]
            public string ChromeLogPath { get; set; }
            public ChromeBrowserParametersOptions()
            {
                Cleanse();
            }

            internal void Cleanse()
            {
                if (Arguments == null) Arguments = new List<string>();
                if (LoggingPreferences == null) LoggingPreferences = new Dictionary<string, LogLevel>();
                if (PerformanceLoggingPreferences == null) PerformanceLoggingPreferences = new LoggingPreferencesPerformance();
                if (UserProfilePreferences == null) UserProfilePreferences = new Dictionary<string, object>();

                Arguments = Arguments.Where(argument => !argument.StartsWith("!")).ToList();
                LoggingPreferences = new Dictionary<string, LogLevel>(LoggingPreferences.Where(pair => !pair.Key.StartsWith("!")).ToDictionary(pair => pair.Key, pair => pair.Value));
                UserProfilePreferences = new Dictionary<string, object>(UserProfilePreferences.Where(pair => !pair.Key.StartsWith("!")).ToDictionary(pair => pair.Key, pair => pair.Value));
                ChromeLogPath = ChromeLogPath ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Chrome_debug.log");
            }

            public class LoggingPreferencesPerformance
            {
                public bool IsEmpty => TracingCategories == null || TracingCategories.Count == 0;

                [JsonProperty("tracingCategories")]
                public List<string> TracingCategories { get; set; }
                public LoggingPreferencesPerformance()
                {
                    Cleanse();
                }

                internal void Cleanse()
                {
                    if (TracingCategories == null) TracingCategories = new List<string>();

                    TracingCategories = TracingCategories.Where(tracingCategory => !tracingCategory.StartsWith("!")).ToList();
                }
            }
        }
    }
}
