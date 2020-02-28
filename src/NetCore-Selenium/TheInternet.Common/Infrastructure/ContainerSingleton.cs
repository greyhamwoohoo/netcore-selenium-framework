﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using TheInternet.Common.ExecutionContext.Runtime;
using TheInternet.Common.ExecutionContext.Runtime.BrowserSettings;
using TheInternet.Common.ExecutionContext.Runtime.BrowserSettings.Contracts;
using TheInternet.Common.ExecutionContext.Runtime.RemoteWebDriverSettings;
using TheInternet.Common.SessionManagement;
using TheInternet.Common.SessionManagement.Contracts;

namespace TheInternet.Common.Infrastructure
{
    public static class ContainerSingleton
    {
        private static IServiceProvider _instance;
        private static object locker = new object();

        public static IServiceProvider Instance
        {
            get
            {
                lock (locker)
                {
                    if (_instance == null) throw new InvalidOperationException($"You must call Container.Initialize exactly once to initialize the container. ");

                    return _instance;
                }
            }
        }

        public static void Initialize(string prefix)
        {
            if (_instance != null) throw new InvalidOperationException($"The Initialize method has already been called. ");

            var services = new ServiceCollection();

            ConfigureBrowserSettings(prefix, services);
            ConfigureRemoteWebDriverSettings(prefix, services);
            ConfigureEnvironmentSettings(prefix, services);

            services.AddSingleton<IBrowserSessionFactory, BrowserSessionFactory>();
            services.AddScoped(isp =>
            {
                var factory = isp.GetRequiredService<IBrowserSessionFactory>();
                var browserProperties = isp.GetRequiredService<IBrowserProperties>();
                var remoteWebDriverSettings = isp.GetRequiredService<RemoteWebDriverSettings>();
                var environmentSettings = isp.GetRequiredService<EnvironmentSettings>();

                var browserSession = factory.Create(browserProperties, remoteWebDriverSettings, environmentSettings);
                return browserSession;
            });

            _instance = services.BuildServiceProvider();
        }

        private static void ConfigureBrowserSettings(string prefix, IServiceCollection services)
        {
            System.Environment.SetEnvironmentVariable("THEINTERNET_BROWSERSETTINGS_FILES", "default-firefox.json");
            System.Environment.SetEnvironmentVariable("THEINTERNET_REMOTEWEBDRIVERSETTINGS_FILES", "zalenium-docker-localhost.json");

            var runtimeSettingsUtilities = new RuntimeSettingsUtilities();
            var paths = runtimeSettingsUtilities.GetSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "BrowserSettings", "default-chrome.json");
            var configurationRoot = runtimeSettingsUtilities.Buildconfiguration(prefix, paths);

            var browserName = configurationRoot.GetSection("browserName")?.Value?.ToUpper();
            var browserSettings = configurationRoot.GetSection("browserSettings");

            switch(browserName)
            {
                case "CHROME":
                    var instance = new ChromeBrowserSettings();
                    
                    browserSettings.Bind(instance);

                    instance = SubstituteEnvironmentVariables<ChromeBrowserSettings>(instance);

                    instance.BrowserName = browserName;
                    instance.Cleanse();

                    services.AddSingleton(instance);
                    services.AddSingleton<IBrowserProperties>(instance);
                    break;
                case "EDGE":
                    var edgeInstance = new EdgeBrowserSettings();

                    browserSettings.Bind(edgeInstance);

                    edgeInstance = SubstituteEnvironmentVariables<EdgeBrowserSettings>(edgeInstance);

                    edgeInstance.BrowserName = browserName;
                    edgeInstance.Cleanse();

                    services.AddSingleton(edgeInstance);
                    services.AddSingleton<IBrowserProperties>(edgeInstance);
                    break;
                case "FIREFOX":
                    var ffInstance = new FireFoxBrowserSettings();

                    browserSettings.Bind(ffInstance);

                    ffInstance = SubstituteEnvironmentVariables<FireFoxBrowserSettings>(ffInstance);

                    ffInstance.BrowserName = browserName;
                    ffInstance.Cleanse();

                    services.AddSingleton(ffInstance);
                    services.AddSingleton<IBrowserProperties>(ffInstance);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException($"The browser called {browserName} is currently not supported. ");
            }
        }

        private static void ConfigureRemoteWebDriverSettings(string prefix, IServiceCollection services)
        {
            var runtimeSettingsUtilities = new RuntimeSettingsUtilities();
            var paths = runtimeSettingsUtilities.GetSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "RemoteWebDriverSettings", "localhost.json");
            var configurationRoot = runtimeSettingsUtilities.Buildconfiguration(prefix, paths);

            var remoteWebDriverSettings = configurationRoot.GetSection("remoteWebDriverSettings");

            var instance = new RemoteWebDriverSettings();

            remoteWebDriverSettings.Bind(instance);

            instance = SubstituteEnvironmentVariables<RemoteWebDriverSettings>(instance);

            instance.Cleanse();

            services.AddSingleton(instance);
        }

        private static void ConfigureEnvironmentSettings(string prefix, IServiceCollection services)
        {
            var runtimeSettingsUtilities = new RuntimeSettingsUtilities();
            var paths = runtimeSettingsUtilities.GetSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "EnvironmentSettings", "internet.json");
            var configurationRoot = runtimeSettingsUtilities.Buildconfiguration(prefix, paths);

            var environmentSettings = configurationRoot.GetSection("EnvironmentSettings");

            var instance = new EnvironmentSettings();

            environmentSettings.Bind(instance);

            instance = SubstituteEnvironmentVariables<EnvironmentSettings>(instance);

            instance.Cleanse();

            services.AddSingleton(instance);
        }

        /// <summary>
        /// Brute force any environment variables embedded within the properties of the object. 
        /// TODO: use a ConfigurationProvider to do this
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        private static T SubstituteEnvironmentVariables<T>(T value) where T: class
        {
            var variables = System.Environment.GetEnvironmentVariables();
            var content = JsonConvert.SerializeObject(value);
            if (content.Contains("%"))
            {
                foreach (var variable in variables.Keys.Cast<string>())
                {
                    var candidateString = JsonConvert.ToString(variables[variable]).Trim('"');
                    content = content.Replace($"%{variable.ToUpper()}%", candidateString);
                }

                return JsonConvert.DeserializeObject<T>(content);
            } 
            else
            {
                return value;    
            }
        }
    }
}
